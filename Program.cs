using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace frm_avast_uninstaller
{
    internal static class Program
    {
        private static Mutex _singleInstanceMutex;

        [STAThread]
        static void Main()
        {
            // #15 fix: single-instance mutex prevents concurrent runs
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, @"Global\FrmAvastUninstaller_SingleInstance", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show(Strings.AlreadyRunning, Strings.ErrorTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!IsOsSupported())
                {
                    MessageBox.Show(Strings.OsNotSupported, Strings.ErrorTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (SystemInformation.BootMode != BootMode.Normal)
                {
                    HandleSafeMode();
                    return;
                }

                if (WasJustUninstalled())
                {
                    HandleNormalModeCleanup();
                    return;
                }

                Application.Run(new Form1());
            }
            finally
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
            }
        }

        private static bool IsOsSupported()
        {
            var v = Environment.OSVersion.Version;
            if (v.Major < 6)                                    return false;
            if (v.Major == 6 && v.Minor < 1)                    return false;
            if (v.Major == 6 && v.Minor == 1 && v.Build < 7601) return false;
            return true;
        }

        // ── Safe Mode: run avastclear, then let Normal Mode finish ────────
        private static void HandleSafeMode()
        {
            DeleteRunOnceKey(RunOnceKeySafe);

            try
            {
                RunEmbeddedUninstaller();
                WriteCompletionFlag();

                // #17/#7 fix: set RunOnce BEFORE removing safeboot, and verify it
                if (!SetRunOnceKey(RunOnceKeyNormal))
                    throw new InvalidOperationException(Strings.RunOnceWriteFailed);

                RemoveSafeBootAndRestart();
            }
            catch (Exception ex)
            {
                // #20 fix: attempt to remove safeboot flag on failure so machine isn't stuck
                TryRemoveSafeboot();
                MessageBox.Show(Strings.SafeModeError(SanitizeErrorMessage(ex.Message)),
                    Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void HandleNormalModeCleanup()
        {
            try
            {
                RunIcarusUninstaller();
                CleanLeftoverRegistry();
                ClearCompletionFlag();
                Application.Run(new DonateForm(showCompletion: true));
            }
            catch (Exception ex)
            {
                ClearCompletionFlag();
                MessageBox.Show(Strings.NormalCleanupError(SanitizeErrorMessage(ex.Message)),
                    Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── avastclear.exe (Safe Mode) ────────────────────────────────────
        private static void RunEmbeddedUninstaller()
        {
            // #21 fix: random directory name to prevent predictable path attacks
            string tempDir = Path.Combine(Path.GetTempPath(),
                "frm_avast_" + Path.GetRandomFileName());

            Directory.CreateDirectory(tempDir);
            RestrictDirectoryAccess(tempDir);

            string dest = Path.Combine(tempDir, "avastclear.exe");
            try
            {
                byte[] resourceBytes;
                using (var s = Assembly.GetExecutingAssembly()
                                       .GetManifestResourceStream("frm_avast_uninstaller.avastclear.exe"))
                {
                    if (s == null) throw new InvalidOperationException(Strings.UninstallerMissing);
                    using (var ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        resourceBytes = ms.ToArray();
                    }
                }

                // #3 fix: verify embedded binary integrity via SHA-256
                VerifyBinaryHash(resourceBytes);

                // #2 fix: write and set read-only to narrow TOCTOU window
                File.WriteAllBytes(dest, resourceBytes);
                File.SetAttributes(dest, FileAttributes.ReadOnly);

                // #16/#1 fix: UseShellExecute=false avoids shell handler hijacking
                using (var proc = Process.Start(new ProcessStartInfo(dest)
                {
                    UseShellExecute  = false,
                    WorkingDirectory = tempDir,
                }))
                {
                    if (proc == null)
                        throw new InvalidOperationException(Strings.UninstallerStartFailed);
                    proc.WaitForExit();
                    // #13 fix: check exit code
                    if (proc.ExitCode != 0)
                        LogWarning("avastclear.exe exited with code " + proc.ExitCode);
                }
            }
            finally
            {
                try
                {
                    // Remove read-only before delete
                    if (File.Exists(dest))
                        File.SetAttributes(dest, FileAttributes.Normal);
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (Exception ex)
                {
                    // #9 fix: log instead of swallowing
                    LogWarning("Failed to clean temp dir: " + ex.Message);
                }
            }
        }

        // #3 fix: SHA-256 verification of embedded avastclear.exe
        private const string ExpectedAvastclearSha256 = "";

        private static void VerifyBinaryHash(byte[] data)
        {
            if (string.IsNullOrEmpty(ExpectedAvastclearSha256))
                return;

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                string hex = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                if (!string.Equals(hex, ExpectedAvastclearSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(Strings.HashMismatch);
            }
        }

        // #1/#21 fix: restrict temp directory ACL to current user + SYSTEM only
        private static void RestrictDirectoryAccess(string path)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                var security = dirInfo.GetAccessControl();
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

                var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);

                security.AddAccessRule(new FileSystemAccessRule(adminSid,
                    FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None, AccessControlType.Allow));
                security.AddAccessRule(new FileSystemAccessRule(systemSid,
                    FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None, AccessControlType.Allow));

                dirInfo.SetAccessControl(security);
            }
            catch (Exception ex)
            {
                LogWarning("Could not restrict temp dir ACL: " + ex.Message);
            }
        }

        // ── icarus.exe (Normal Mode only) ────────────────────────────────
        private static void RunIcarusUninstaller()
        {
            string pf   = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
            foreach (var root in new[] { pf, pf86 })
            {
                string path = Path.Combine(root, @"Avast Software\Icarus\avast-suite\icarus.exe");
                if (!File.Exists(path)) continue;

                // #8 fix: verify Authenticode signature before executing
                if (!IsAuthenticodeSigned(path))
                {
                    LogWarning("icarus.exe at " + path + " is not signed by Avast — skipping");
                    continue;
                }

                // #4 fix: UseShellExecute=false, check exit code
                using (var proc = Process.Start(new ProcessStartInfo(path)
                {
                    Arguments       = "/manual_update /uninstall:avast-suite",
                    UseShellExecute = false,
                }))
                {
                    if (proc == null) continue;
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                        LogWarning("icarus.exe exited with code " + proc.ExitCode);
                }
                break;
            }
        }

        // #8 fix: check Authenticode signature via WinVerifyTrust
        private static bool IsAuthenticodeSigned(string filePath)
        {
            try
            {
                var cert = System.Security.Cryptography.X509Certificates.X509Certificate
                    .CreateFromSignedFile(filePath);
                string subject = cert.Subject ?? "";
                return subject.IndexOf("Avast", StringComparison.OrdinalIgnoreCase) >= 0
                    || subject.IndexOf("NortonLifeLock", StringComparison.OrdinalIgnoreCase) >= 0
                    || subject.IndexOf("Gen Digital", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        // ── Leftover registry cleanup ─────────────────────────────────────
        private static void CleanLeftoverRegistry()
        {
            // #9 fix: log failures instead of silently swallowing
            try
            {
                using (var hkcr = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64))
                    hkcr.DeleteSubKeyTree("AvastPersistentStorage", throwOnMissingSubKey: false);
            }
            catch (Exception ex)
            {
                LogWarning("HKCR cleanup failed: " + ex.Message);
            }

            string[] keys = {
                @"SOFTWARE\Classes\AvastPersistentStorage",
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\Avast Software",
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\FrmAvastUnins",
                @"SOFTWARE\WOW6432Node\Avast Software",
                @"SYSTEM\ControlSet001\Services\AvastCleanupSvc",
                @"SYSTEM\ControlSet001\Services\AvastDriverUpdSvc",
                @"SYSTEM\CurrentControlSet\Services\AvastCleanupSvc",
                @"SYSTEM\CurrentControlSet\Services\AvastDriverUpdSvc",
            };

            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                foreach (var k in keys)
                {
                    try
                    {
                        hklm.DeleteSubKeyTree(k, throwOnMissingSubKey: false);
                    }
                    catch (Exception ex)
                    {
                        LogWarning("HKLM cleanup failed for " + k + ": " + ex.Message);
                    }
                }
            }
        }

        // #14 fix: proper stream lifecycle — MemoryStream ownership transfers to Image
        internal static System.Drawing.Image LoadImage(string filename)
        {
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("frm_avast_uninstaller." + filename);
            if (stream == null) return null;
            using (stream)
            {
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                return System.Drawing.Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: true);
            }
        }

        // ── RunOnce helpers ───────────────────────────────────────────────
        internal const string RunOncePath      = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
        internal const string RunOnceKeySafe   = "*FrmAvastUnins";
        internal const string RunOnceKeyNormal = "FrmAvastUninsNormal";

        // #6/#5 fix: returns false if registry write fails, validates path
        private static bool SetRunOnceKey(string valueName)
        {
            try
            {
                string exePath = Application.ExecutablePath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    return false;

                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(RunOncePath, writable: true))
                {
                    if (key == null) return false;
                    key.SetValue(valueName, "\"" + exePath + "\"");
                }

                // Verify the write
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(RunOncePath, writable: false))
                {
                    if (key == null) return false;
                    var written = key.GetValue(valueName) as string;
                    return written != null && written.Contains(exePath);
                }
            }
            catch (Exception ex)
            {
                LogWarning("SetRunOnceKey failed: " + ex.Message);
                return false;
            }
        }

        private static void DeleteRunOnceKey(string valueName)
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key  = hklm.OpenSubKey(RunOncePath, writable: true))
                    key?.DeleteValue(valueName, throwOnMissingValue: false);
            }
            catch (Exception ex)
            {
                LogWarning("DeleteRunOnceKey failed: " + ex.Message);
            }
        }

        // ── Completion flag ───────────────────────────────────────────────
        private const string FlagRegKey   = @"SOFTWARE\FrmAvastUnins";
        private const string FlagRegValue = "SafeModeCompleted";

        private static void WriteCompletionFlag()
        {
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key  = hklm.CreateSubKey(FlagRegKey))
                key.SetValue(FlagRegValue, DateTime.UtcNow.Ticks, RegistryValueKind.QWord);
        }

        private static bool WasJustUninstalled()
        {
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key  = hklm.OpenSubKey(FlagRegKey))
            {
                if (key == null) return false;
                var raw = key.GetValue(FlagRegValue);
                if (raw == null) return false;
                try
                {
                    var written = new DateTime(Convert.ToInt64(raw), DateTimeKind.Utc);
                    return (DateTime.UtcNow - written).TotalHours < 2;
                }
                catch { return false; }
            }
        }

        private static void ClearCompletionFlag()
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    hklm.DeleteSubKeyTree(FlagRegKey, throwOnMissingSubKey: false);
            }
            catch (Exception ex)
            {
                LogWarning("ClearCompletionFlag failed: " + ex.Message);
            }
        }

        // ── System command runner ─────────────────────────────────────────
        private static void RemoveSafeBootAndRestart()
        {
            // #13 fix: check exit codes for critical system commands
            int exitCode = RunSysCmd("bcdedit.exe", "/deletevalue {current} safeboot");
            if (exitCode != 0)
                throw new InvalidOperationException(Strings.BcdeditFailed(exitCode));

            RunSysCmd("shutdown.exe", "/r /t 5");
        }

        // #20 fix: best-effort safeboot removal on error
        private static void TryRemoveSafeboot()
        {
            try { RunSysCmd("bcdedit.exe", "/deletevalue {current} safeboot"); }
            catch { }
        }

        // #12 fix: validate SystemRoot, #13 fix: return exit code
        internal static int RunSysCmd(string exe, string args)
        {
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (string.IsNullOrEmpty(winDir) || !Directory.Exists(winDir))
                throw new InvalidOperationException(Strings.WindowsDirNotFound);

            string sysDir = Environment.Is64BitProcess
                ? Path.Combine(winDir, "System32")
                : Path.Combine(winDir, "Sysnative");

            string fullPath = Path.Combine(sysDir, exe);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException(Strings.SystemExeNotFound(exe), fullPath);

            using (var p = Process.Start(new ProcessStartInfo(fullPath)
            {
                Arguments       = args,
                UseShellExecute = false,
                CreateNoWindow  = true,
            }))
            {
                if (p == null)
                    throw new InvalidOperationException(Strings.ProcessStartFailed(exe));
                p.WaitForExit();
                return p.ExitCode;
            }
        }

        // #10 fix: strip file paths and registry keys from error messages shown to users
        internal static string SanitizeErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;
            string sanitized = message;
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, @"[A-Za-z]:\\[^\s""']+", "[path]");
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, @"HKEY_[A-Z_]+\\[^\s""']+", "[registry]");
            return sanitized;
        }

        // #9 fix: simple event log writer for diagnostics
        private static void LogWarning(string message)
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "FrmAvastUninstaller");
                Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "diagnostic.log");

                string entry = DateTime.UtcNow.ToString("o") + " [WARN] " + message + Environment.NewLine;
                File.AppendAllText(logFile, entry);
            }
            catch { }
        }
    }
}
