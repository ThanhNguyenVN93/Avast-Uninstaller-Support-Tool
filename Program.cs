using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace frm_avast_uninstaller
{
    internal static class Program
    {
        [STAThread]
        static void Main()
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

        // ── OS guard ─────────────────────────────────────────────────────
        private static bool IsOsSupported()
        {
            var v = Environment.OSVersion.Version;
            if (v.Major < 6)                                    return false; // XP-
            if (v.Major == 6 && v.Minor < 1)                    return false; // Vista
            if (v.Major == 6 && v.Minor == 1 && v.Build < 7601) return false; // Win7 RTM
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
                SetRunOnceKey(RunOnceKeyNormal);
                RemoveSafeBootAndRestart();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.SafeModeError(ex.Message),
                    Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Normal Mode cleanup: icarus + registry ────────────────────────
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
                MessageBox.Show(Strings.NormalCleanupError(ex.Message),
                    Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── avastclear.exe (Safe Mode) ────────────────────────────────────
        private static void RunEmbeddedUninstaller()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "frm_avast_uninstaller_temp");
            Directory.CreateDirectory(tempDir);
            string dest = Path.Combine(tempDir, "avastclear.exe");
            try
            {
                using (var s = Assembly.GetExecutingAssembly()
                                       .GetManifestResourceStream("frm_avast_uninstaller.avastclear.exe"))
                {
                    if (s == null) throw new InvalidOperationException(Strings.UninstallerMissing);
                    using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                        s.CopyTo(fs);
                }
                using (var proc = Process.Start(new ProcessStartInfo(dest)
                {
                    UseShellExecute  = true,
                    WorkingDirectory = tempDir,
                }))
                    proc?.WaitForExit();
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
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
                using (var proc = Process.Start(new ProcessStartInfo(path)
                {
                    Arguments       = "/manual_update /uninstall:avast-suite",
                    UseShellExecute = true,
                }))
                    proc?.WaitForExit();
                break;
            }
        }

        // ── Leftover registry cleanup ─────────────────────────────────────
        private static void CleanLeftoverRegistry()
        {
            try
            {
                using (var hkcr = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64))
                    hkcr.DeleteSubKeyTree("AvastPersistentStorage", throwOnMissingSubKey: false);
            }
            catch { }

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
                foreach (var k in keys)
                    try { hklm.DeleteSubKeyTree(k, throwOnMissingSubKey: false); } catch { }
        }

        // ── Image loader (used by both forms) ────────────────────────────
        internal static System.Drawing.Image LoadImage(string filename)
        {
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("frm_avast_uninstaller." + filename);
            if (stream == null) return null;
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            stream.Dispose();
            ms.Position = 0;
            return System.Drawing.Image.FromStream(ms);
        }

        // ── RunOnce helpers ───────────────────────────────────────────────
        internal const string RunOncePath      = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
        internal const string RunOnceKeySafe   = "*FrmAvastUnins";     // * = fires in Safe Mode too
        internal const string RunOnceKeyNormal = "FrmAvastUninsNormal"; // Normal Mode only

        private static void SetRunOnceKey(string valueName)
        {
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key  = hklm.OpenSubKey(RunOncePath, writable: true))
                key?.SetValue(valueName, "\"" + Application.ExecutablePath + "\"");
        }

        private static void DeleteRunOnceKey(string valueName)
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key  = hklm.OpenSubKey(RunOncePath, writable: true))
                    key?.DeleteValue(valueName, throwOnMissingValue: false);
            }
            catch { }
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
            catch { }
        }

        // ── System command runner ─────────────────────────────────────────
        private static void RemoveSafeBootAndRestart()
        {
            RunSysCmd("bcdedit.exe", "/deletevalue {current} safeboot");
            RunSysCmd("shutdown.exe", "/r /t 5");
        }

        internal static void RunSysCmd(string exe, string args)
        {
            string winDir = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
            string sysDir = Environment.Is64BitProcess
                ? Path.Combine(winDir, "System32")
                : Path.Combine(winDir, "Sysnative");

            using (var p = Process.Start(new ProcessStartInfo(Path.Combine(sysDir, exe))
            {
                Arguments       = args,
                UseShellExecute = false,
                CreateNoWindow  = true,
            }))
                p?.WaitForExit();
        }
    }
}
