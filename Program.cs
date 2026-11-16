using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace frm_avast_uninstaller
{
    internal static class Program
    {
        internal const string AppVersion = "2.0.0";

        private static Mutex _singleInstanceMutex;

        private const string StateRegKey = @"SOFTWARE\FrmAvastUnins";
        private const string RunOncePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
        private const string RunOnceKeySafe = "*FrmAvastUnins";
        private const string RunOnceKeyNormal = "FrmAvastUninsNormal";
        private const string AlternateShellKey = @"SYSTEM\CurrentControlSet\Control\SafeBoot";
        private const string AlternateShellValue = "AlternateShell";

        private const int MaxSafeModeAttempts = 3;
        private const int StaleArmHours = 6;
        private const long MinDiskSpaceBytes = 100L * 1024 * 1024;
        private const long MaxLogBytes = 1024 * 1024;

        internal static string LogFilePath
        {
            get { return Path.Combine(LocalDir(), "diagnostic.log"); }
        }

        [STAThread]
        static void Main()
        {
            Application.ThreadException += (s, e) => HandleCrash(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                HandleCrash(e.ExceptionObject as Exception);

            string[] args = Environment.GetCommandLineArgs();

            if (HasArg(args, "--help"))
            {
                MessageBox.Show(Loc.T("help_text"), Loc.T("help_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool createdNew;
            _singleInstanceMutex = new Mutex(true, @"Global\FrmAvastUninstaller_SingleInstance", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show(Loc.T("already_running"), Loc.T("app_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Loc.LoadSaved();
                TryAddAvastExclusion();

                if (!IsOsSupported())
                {
                    MessageBox.Show(Loc.T("os_not_supported"), Loc.T("app_title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                StageSelfLocally();

                if (IsSafeMode())
                {
                    HandleSafeMode();
                    return;
                }

                if (HasArg(args, "--result"))
                {
                    Application.Run(new ResultForm());
                    return;
                }

                UndoAllBootTriggers();

                if (WasJustUninstalled())
                {
                    HandleNormalModeCleanup();
                    return;
                }

                if (HasArg(args, "--auto"))
                {
                    RunAutoSilent(args);
                    return;
                }

                if (HasArg(args, "--nosafemode"))
                {
                    RunEmbeddedUninstaller(interactive: true);
                    Environment.ExitCode = AvastInstalled() ? 1 : 0;
                    return;
                }

                if (HasArg(args, "--cleanleftovers"))
                {
                    CleanAvastLeftovers();
                    Environment.ExitCode = 0;
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

        private static void HandleCrash(Exception ex)
        {
            try
            {
                Log("CRASH: " + (ex != null ? ex.ToString() : "(null)"));
                MessageBox.Show(Loc.T("crash") + "\n" + LogFilePath,
                    Loc.T("crash_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }

        private static bool IsOsSupported()
        {
            var v = Environment.OSVersion.Version;
            if (v.Major < 6) return false;
            if (v.Major == 6 && v.Minor < 1) return false;
            if (v.Major == 6 && v.Minor == 1 && v.Build < 7601) return false;
            return true;
        }

        // ── Safe Mode handler ────────────────────────────────────────────
        private static void HandleSafeMode()
        {
            DeleteRunOnceKey(RunOnceKeySafe);
            Log("=== Safe Mode session start ===");

            int attempts = ParseInt(GetState("SafeModeAttempts"));
            if (attempts >= MaxSafeModeAttempts)
            {
                Log("Loop guard: " + attempts + " attempts reached, aborting.");
                SetState("LastResult", "Error");
                UndoAllBootTriggers();
                SetRunOnceKey(RunOnceKeyNormal, "--result");
                RemoveSafebootAndRestart();
                return;
            }

            string armTimeStr = GetState("ArmTime");
            if (!string.IsNullOrEmpty(armTimeStr))
            {
                long ticks;
                if (long.TryParse(armTimeStr, out ticks))
                {
                    var armed = new DateTime(ticks, DateTimeKind.Utc);
                    if ((DateTime.UtcNow - armed).TotalHours > StaleArmHours)
                    {
                        Log("Stale arm detected (" + StaleArmHours + "h), aborting.");
                        SetState("LastResult", "Error");
                        UndoAllBootTriggers();
                        SetRunOnceKey(RunOnceKeyNormal, "--result");
                        RemoveSafebootAndRestart();
                        return;
                    }
                }
            }

            SetState("SafeModeAttempts", (attempts + 1).ToString());

            try
            {
                bool cleanLeftovers = GetState("CleanLeftovers") == "1";
                int passes = 2;

                for (int pass = 1; pass <= passes; pass++)
                {
                    Log("Uninstall pass " + pass + "/" + passes);
                    RunEmbeddedUninstaller(interactive: false);
                    if (!AvastInstalled()) break;
                }

                if (cleanLeftovers)
                {
                    Log("Cleaning leftover Avast services/folders...");
                    CleanAvastLeftovers();
                }

                bool still = AvastInstalled();
                SetState("LastResult", still ? "Warning" : "Success");
                Log("Safe Mode done. Avast still present: " + still);

                DeleteRunOnceKey(RunOnceKeySafe);
                RestoreAlternateShell();
                try { AutoLogon.Disable(); } catch { }
                ClearState("Armed");
                ClearState("ArmTime");
                ClearState("SafeModeAttempts");

                bool runOnceOk = SetRunOnceKey(RunOnceKeyNormal, "--result");
                Log("RunOnce for normal set: " + runOnceOk);
                FlushRegistry();
                RemoveSafebootAndRestart();
            }
            catch (Exception ex)
            {
                Log("Safe Mode error: " + ex);
                SetState("LastResult", "Error");
                TryRemoveSafeboot();
                RestoreAlternateShell();
                try { AutoLogon.Disable(); } catch { }
                ClearState("Armed");
                ClearState("ArmTime");
                ClearState("SafeModeAttempts");
                SetRunOnceKey(RunOnceKeyNormal, "--result");
                FlushRegistry();
                try { RunSysCmd("shutdown.exe", "/r /t 10"); } catch { }
            }
        }

        private static void HandleNormalModeCleanup()
        {
            try
            {
                AutoLogon.Disable();
                RunIcarusUninstaller();
                if (GetState("CleanLeftovers") == "1")
                    CleanAvastLeftovers();
                else
                    CleanLeftoverRegistry();
                ClearState("SafeModeCompleted");

                string result = GetState("LastResult");
                if (result == "Success" || result == "Warning")
                {
                    Application.Run(new DonateForm(showCompletion: true));
                }
                else
                {
                    Application.Run(new ResultForm());
                }
            }
            catch (Exception ex)
            {
                Log("Normal cleanup error: " + ex.Message);
                ClearState("SafeModeCompleted");
                Application.Run(new ResultForm());
            }
        }

        // ── CLI silent mode ──────────────────────────────────────────────
        private static void RunAutoSilent(string[] args)
        {
            Log("=== Auto/silent mode ===");

            if (!AvastInstalled())
            {
                Log("No Avast detected, nothing to do.");
                Environment.ExitCode = 3;
                return;
            }

            bool cleanLeftovers = HasArg(args, "--cleanleftovers");
            SetState("CleanLeftovers", cleanLeftovers ? "1" : "0");

            string password = GetArgValue(args, "--password");

            try
            {
                TryStageLatestUninstaller();
                CreateRestorePoint();
                BackupNetworkSettings();
                ArmSafeMode(password);
                RunSysCmd("shutdown.exe", "/r /t 5");
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Log("Auto mode failed: " + ex.Message);
                UndoAllBootTriggers();
                Environment.ExitCode = 2;
            }
        }

        // ── Arm Safe Mode ────────────────────────────────────────────────
        internal static void ArmSafeMode(string password)
        {
            string exePath = StagedOrCurrentExe();
            Log("Arming Safe Mode. Exe: " + exePath);

            SetState("Armed", "1");
            SetState("ArmTime", DateTime.UtcNow.Ticks.ToString());
            SetState("SafeModeAttempts", "0");

            SetRunOnceKey(RunOnceKeySafe, null, exePath);

            SetAlternateShell(exePath);

            int rc = RunSysCmd("bcdedit.exe", "/set {current} safeboot minimal");
            if (rc != 0)
                throw new InvalidOperationException("bcdedit safeboot failed (exit " + rc + ")");

            rc = RunSysCmd("bcdedit.exe", "/set {current} safebootalternateshell yes");
            if (rc != 0)
                Log("WARNING: safebootalternateshell failed (exit " + rc + ") — RunOnce fallback will be used.");

            if (!string.IsNullOrEmpty(password))
            {
                try
                {
                    AutoLogon.Enable(password);
                    Log("Autologon enabled.");
                }
                catch (Exception ex)
                {
                    Log("Autologon failed: " + ex.Message);
                }
            }
        }

        internal static void UndoAllBootTriggers()
        {
            DeleteRunOnceKey(RunOnceKeySafe);
            RestoreAlternateShell();
            TryRemoveSafeboot();
            try { AutoLogon.Disable(); } catch { }
            ClearState("Armed");
            ClearState("ArmTime");
            ClearState("SafeModeAttempts");
            ClearState("CleanLeftovers");
        }

        private static void SetAlternateShell(string exePath)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(AlternateShellKey, writable: true))
                {
                    if (key == null) return;
                    string prior = key.GetValue(AlternateShellValue) as string;
                    if (!string.IsNullOrEmpty(prior))
                        SetState("PriorAlternateShell", prior);
                    key.SetValue(AlternateShellValue, exePath);
                    Log("AlternateShell set to " + exePath);
                }
            }
            catch (Exception ex)
            {
                Log("AlternateShell set failed: " + ex.Message);
            }
        }

        private static void RestoreAlternateShell()
        {
            try
            {
                string prior = GetState("PriorAlternateShell");
                using (var key = Registry.LocalMachine.OpenSubKey(AlternateShellKey, writable: true))
                {
                    if (key == null) return;
                    if (!string.IsNullOrEmpty(prior))
                        key.SetValue(AlternateShellValue, prior);
                    else
                        key.SetValue(AlternateShellValue, "cmd.exe");
                }
                ClearState("PriorAlternateShell");
            }
            catch { }
        }

        // ── Avast exclusion (prevent CyberCapture quarantine) ────────────
        internal static void TryAddAvastExclusion()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                if (string.IsNullOrEmpty(exePath)) return;

                string[] exclusionKeys = {
                    @"SOFTWARE\Avast Software\Avast\Exclusions\Path",
                    @"SOFTWARE\WOW6432Node\Avast Software\Avast\Exclusions\Path",
                };

                foreach (var regPath in exclusionKeys)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.CreateSubKey(regPath))
                        {
                            if (key == null) continue;
                            bool found = false;
                            foreach (var name in key.GetValueNames())
                            {
                                string val = key.GetValue(name) as string;
                                if (val != null && val.Equals(exePath, System.StringComparison.OrdinalIgnoreCase))
                                { found = true; break; }
                            }
                            if (!found)
                            {
                                string newName = key.ValueCount.ToString();
                                key.SetValue(newName, exePath);
                                Log("Added Avast exclusion: " + exePath + " in " + regPath);
                            }
                        }
                    }
                    catch { }
                }

                string localDir = LocalDir();
                foreach (var regPath in exclusionKeys)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.CreateSubKey(regPath))
                        {
                            if (key == null) continue;
                            bool found = false;
                            foreach (var name in key.GetValueNames())
                            {
                                string val = key.GetValue(name) as string;
                                if (val != null && val.Equals(localDir, System.StringComparison.OrdinalIgnoreCase))
                                { found = true; break; }
                            }
                            if (!found)
                            {
                                string newName = key.ValueCount.ToString();
                                key.SetValue(newName, localDir);
                                Log("Added Avast exclusion dir: " + localDir + " in " + regPath);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log("Avast exclusion failed: " + ex.Message);
            }
        }

        // ── Avast detection ──────────────────────────────────────────────
        internal static bool AvastInstalled()
        {
            string[] services = { "AvastSvc", "aswbIDSAgent", "avast! Antivirus", "AvastWscReporter" };
            foreach (var svc in services)
            {
                try
                {
                    using (var sc = new System.ServiceProcess.ServiceController(svc))
                    {
                        var _ = sc.Status;
                        return true;
                    }
                }
                catch { }
            }

            string[] uninstKeys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
            };
            foreach (var path in uninstKeys)
            {
                try
                {
                    using (var root = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (root == null) continue;
                        foreach (var sub in root.GetSubKeyNames())
                        {
                            using (var k = root.OpenSubKey(sub))
                            {
                                string dn = k?.GetValue("DisplayName") as string;
                                if (dn != null && dn.IndexOf("Avast", StringComparison.OrdinalIgnoreCase) >= 0)
                                    return true;
                            }
                        }
                    }
                }
                catch { }
            }

            string[] folders = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Avast Software"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Avast Software"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Avast Software"),
            };
            foreach (var f in folders)
                if (Directory.Exists(f)) return true;

            return false;
        }

        // ── Download latest avastclear from Avast CDN ─────────────────────
        private const string AvastClearUrl =
            "https://honzik.avcdn.net/setup/avast-av/release/avast_av_clear.exe";

        internal static bool TryStageLatestUninstaller()
        {
            string dir = LocalDir();
            string tmp = Path.Combine(dir, "avastclear_download.tmp");
            string dest = Path.Combine(dir, "avastclear_staged.exe");

            try
            {
                Directory.CreateDirectory(dir);
                Log("Downloading latest avastclear from " + AvastClearUrl);

                System.Net.ServicePointManager.SecurityProtocol |=
                    System.Net.SecurityProtocolType.Tls12;

                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(AvastClearUrl);
                req.Timeout = 30000;
                req.ReadWriteTimeout = 30000;
                req.UserAgent = "AvastUninstallerSupport/" + AppVersion;

                using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
                using (var rs = resp.GetResponseStream())
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
                    rs.CopyTo(fs);

                var fi = new FileInfo(tmp);
                if (fi.Length < 100 * 1024)
                {
                    Log("Downloaded file too small (" + fi.Length + " bytes), rejected.");
                    try { File.Delete(tmp); } catch { }
                    return false;
                }

                uint wvt = Verify.WinVerifyCode(tmp);
                if (wvt != 0)
                {
                    Log("Downloaded avastclear WinVerifyTrust failed: 0x" + wvt.ToString("X"));
                    try { File.Delete(tmp); } catch { }
                    return false;
                }

                string signer;
                if (Verify.TryGetSigner(tmp, out signer))
                {
                    if (!Verify.IsAvastSigner(signer))
                    {
                        Log("Downloaded avastclear not signed by Avast: " + signer);
                        try { File.Delete(tmp); } catch { }
                        return false;
                    }
                    Log("Downloaded avastclear signer: " + signer);
                }

                if (File.Exists(dest)) File.Delete(dest);
                File.Move(tmp, dest);
                Log("Staged latest avastclear: " + dest + " (" + fi.Length + " bytes)");
                return true;
            }
            catch (Exception ex)
            {
                Log("Download avastclear failed: " + ex.Message);
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                return false;
            }
        }

        // ── Embedded uninstaller ─────────────────────────────────────────
        internal static void RunEmbeddedUninstaller(bool interactive)
        {
            if (!CheckDiskSpace()) return;

            string staged = Path.Combine(LocalDir(), "avastclear_staged.exe");
            string tempDir = Path.Combine(Path.GetTempPath(), "frm_avast_" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            RestrictDirectoryAccess(tempDir);

            string dest = Path.Combine(tempDir, "avastclear.exe");
            try
            {
                bool usedStaged = false;
                if (File.Exists(staged))
                {
                    uint wvt = Verify.WinVerifyCode(staged);
                    if (wvt == 0)
                    {
                        File.Copy(staged, dest, overwrite: true);
                        usedStaged = true;
                        Log("Using staged avastclear: " + staged + " (WinVerifyTrust OK)");
                    }
                    else
                    {
                        Log("Staged avastclear failed WinVerifyTrust (0x" + wvt.ToString("X") + "), falling back to embedded.");
                    }
                }

                if (!usedStaged)
                {
                    byte[] resourceBytes;
                    using (var s = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("frm_avast_uninstaller.avastclear.exe"))
                    {
                        if (s == null)
                        {
                            Log("ERROR: avastclear.exe not found in embedded resources.");
                            return;
                        }
                        using (var ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            resourceBytes = ms.ToArray();
                        }
                    }

                    string hash = Verify.Sha256OfBytes(resourceBytes);
                    Log("Embedded avastclear hash: " + hash);

                    File.WriteAllBytes(dest, resourceBytes);
                }

                File.SetAttributes(dest, FileAttributes.ReadOnly);

                string signer;
                if (Verify.TryGetSigner(dest, out signer))
                    Log("avastclear signer: " + signer);

                int timeoutMs = interactive ? 30 * 60 * 1000 : 5 * 60 * 1000;

                string args = interactive ? "" : "/silent /uninstallonly";
                Log("Starting avastclear: " + dest + (string.IsNullOrEmpty(args) ? "" : " " + args));

                using (var proc = Process.Start(new ProcessStartInfo(dest)
                {
                    Arguments = args,
                    UseShellExecute = false,
                    WorkingDirectory = tempDir,
                }))
                {
                    if (proc == null)
                    {
                        Log("ERROR: Failed to start avastclear.exe");
                        return;
                    }

                    bool exited = proc.WaitForExit(timeoutMs);
                    if (!exited)
                    {
                        Log("avastclear.exe timed out after " + (timeoutMs / 1000) + "s, killing.");
                        try { proc.Kill(); } catch { }
                        proc.WaitForExit(5000);
                    }
                    else
                    {
                        Log("avastclear.exe exited with code " + proc.ExitCode);
                    }
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(dest))
                        File.SetAttributes(dest, FileAttributes.Normal);
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (Exception ex)
                {
                    Log("Temp cleanup failed: " + ex.Message);
                }
            }
        }

        // ── icarus.exe (Normal Mode) ─────────────────────────────────────
        private static void RunIcarusUninstaller()
        {
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
            foreach (var root in new[] { pf, pf86 })
            {
                string path = Path.Combine(root, @"Avast Software\Icarus\avast-suite\icarus.exe");
                if (!File.Exists(path)) continue;

                uint wvt = Verify.WinVerifyCode(path);
                if (wvt != 0)
                {
                    Log("icarus.exe WinVerifyTrust failed (0x" + wvt.ToString("X") + "), skipping.");
                    continue;
                }

                string signer;
                if (Verify.TryGetSigner(path, out signer) && !Verify.IsAvastSigner(signer))
                {
                    Log("icarus.exe signer not Avast: " + signer + ", skipping.");
                    continue;
                }

                using (var proc = Process.Start(new ProcessStartInfo(path)
                {
                    Arguments = "/manual_update /uninstall:avast-suite",
                    UseShellExecute = false,
                }))
                {
                    if (proc == null) continue;
                    proc.WaitForExit();
                    Log("icarus.exe exited with code " + proc.ExitCode);
                }
                break;
            }
        }

        // ── Leftover cleanup ─────────────────────────────────────────────
        internal static void CleanAvastLeftovers()
        {
            string[] services = { "AvastSvc", "aswbIDSAgent", "AvastWscReporter",
                "AvastCleanupSvc", "AvastDriverUpdSvc", "aswSP", "aswMonFlt",
                "aswStm", "aswVmm", "aswArPot", "aswRdr", "aswSnx", "aswKbd" };

            foreach (var svc in services)
            {
                try
                {
                    RunSysCmd("sc.exe", "stop " + svc);
                    RunSysCmd("sc.exe", "delete " + svc);
                    Log("Removed service: " + svc);
                }
                catch { }
            }

            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string pd = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string cf = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            string cf86 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string userAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string userLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var folders = new List<string>
            {
                Path.Combine(pf, "Avast Software"),
                Path.Combine(pf86, "Avast Software"),
                Path.Combine(pd, "Avast Software"),
                Path.Combine(cf, "Avast Software"),
                Path.Combine(cf86, "Avast Software"),
                Path.Combine(userAppData, "Avast Software"),
                Path.Combine(Path.GetTempPath(), "_avast_"),
                Path.Combine(winDir, @"Temp\_avast_"),
            };

            try
            {
                string tasksDir = Path.Combine(winDir, @"System32\Tasks\Avast Software");
                if (Directory.Exists(tasksDir)) folders.Add(tasksDir);
            }
            catch { }

            try
            {
                foreach (var ini in Directory.GetFiles(pd, "avast*.ini", SearchOption.TopDirectoryOnly))
                    folders.Add(ini);
            }
            catch { }

            foreach (var item in folders)
            {
                try
                {
                    if (File.Exists(item))
                    {
                        File.SetAttributes(item, FileAttributes.Normal);
                        File.Delete(item);
                        Log("Removed file: " + item);
                    }
                    else if (Directory.Exists(item))
                    {
                        Directory.Delete(item, recursive: true);
                        Log("Removed folder: " + item);
                    }
                }
                catch (Exception ex)
                {
                    Log("Could not remove " + item + ": " + ex.Message);
                }
            }

            CleanLeftoverRegistry();
        }

        private static void CleanLeftoverRegistry()
        {
            try
            {
                using (var hkcr = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64))
                    hkcr.DeleteSubKeyTree("AvastPersistentStorage", throwOnMissingSubKey: false);
            }
            catch (Exception ex) { Log("HKCR cleanup: " + ex.Message); }

            string[] keys = {
                @"SOFTWARE\Classes\AvastPersistentStorage",
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\Avast Software",
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
                    try { hklm.DeleteSubKeyTree(k, throwOnMissingSubKey: false); }
                    catch (Exception ex) { Log("HKLM cleanup " + k + ": " + ex.Message); }
                }
            }
        }

        // ── System Restore point ─────────────────────────────────────────
        internal static void CreateRestorePoint()
        {
            try
            {
                Log("Creating System Restore point...");
                var scope = new ManagementScope(@"\\localhost\root\default");
                {
                    scope.Connect();
                    using (var cls = new ManagementClass(scope, new ManagementPath("SystemRestore"), null))
                    using (var inParams = cls.GetMethodParameters("CreateRestorePoint"))
                    {
                        inParams["Description"] = "Before Avast removal";
                        inParams["RestorePointType"] = 12;
                        inParams["EventType"] = 100;
                        cls.InvokeMethod("CreateRestorePoint", inParams, null);
                    }
                }
                Log("Restore point created.");
            }
            catch (Exception ex)
            {
                Log("Restore point failed: " + ex.Message);
            }
        }

        // ── Network backup/restore ───────────────────────────────────────
        internal static void BackupNetworkSettings()
        {
            try
            {
                string dest = Path.Combine(LocalDir(), "network_backup.txt");
                Directory.CreateDirectory(LocalDir());

                using (var proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = GetSysPath("netsh.exe"),
                        Arguments = "-c interface dump",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.UTF8,
                    };
                    proc.Start();
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    File.WriteAllText(dest, output, Encoding.UTF8);
                    Log("Network backup saved to " + dest);
                }
            }
            catch (Exception ex)
            {
                Log("Network backup failed: " + ex.Message);
            }
        }

        // ── Diagnostics bundle ───────────────────────────────────────────
        internal static string CollectDiagnostics()
        {
            string dir = LocalDir();
            string zipPath = Path.Combine(dir, "diagnostics_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip");

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                if (File.Exists(LogFilePath))
                    zip.CreateEntryFromFile(LogFilePath, Path.GetFileName(LogFilePath));

                string oldLog = LogFilePath + ".old";
                if (File.Exists(oldLog))
                    zip.CreateEntryFromFile(oldLog, "log.old");

                string netBackup = Path.Combine(dir, "network_backup.txt");
                if (File.Exists(netBackup))
                    zip.CreateEntryFromFile(netBackup, "network_backup.txt");

                var entry = zip.CreateEntry("environment.txt");
                using (var w = new StreamWriter(entry.Open()))
                {
                    w.WriteLine("Date: " + DateTime.Now.ToString("o"));
                    w.WriteLine("OS: " + Environment.OSVersion);
                    w.WriteLine("64bit: " + Environment.Is64BitOperatingSystem);
                    w.WriteLine("User: " + Environment.UserName);
                    w.WriteLine("Machine: " + Environment.MachineName);
                    w.WriteLine("CLR: " + Environment.Version);
                    w.WriteLine("SafeMode: " + IsSafeMode());
                    w.WriteLine("AvastInstalled: " + AvastInstalled());
                    w.WriteLine("AppVersion: " + AppVersion);
                }
            }

            Log("Diagnostics bundle: " + zipPath);
            return zipPath;
        }

        // ── Self-staging ─────────────────────────────────────────────────
        internal static void StageSelfLocally()
        {
            try
            {
                string current = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(current)) return;

                string drive = Path.GetPathRoot(current);
                if (drive != null && drive.Length >= 2 && drive[1] == ':')
                    return;

                string dest = Path.Combine(LocalDir(), Path.GetFileName(current));
                Directory.CreateDirectory(LocalDir());
                File.Copy(current, dest, overwrite: true);
                Log("Staged self locally: " + dest);
            }
            catch (Exception ex)
            {
                Log("Self-stage failed: " + ex.Message);
            }
        }

        private static string StagedOrCurrentExe()
        {
            string staged = Path.Combine(LocalDir(), Path.GetFileName(Assembly.GetExecutingAssembly().Location));
            if (File.Exists(staged)) return staged;
            return Application.ExecutablePath;
        }

        // ── Disk space check ─────────────────────────────────────────────
        private static bool CheckDiskSpace()
        {
            try
            {
                string root = Path.GetPathRoot(Path.GetTempPath());
                var drive = new DriveInfo(root);
                if (drive.AvailableFreeSpace < MinDiskSpaceBytes)
                {
                    Log("Insufficient disk space: " + (drive.AvailableFreeSpace / (1024 * 1024)) + " MB available.");
                    return false;
                }
            }
            catch { }
            return true;
        }

        // ── RunOnce helpers ──────────────────────────────────────────────
        internal static bool SetRunOnceForSafeMode()
        {
            return SetRunOnceKey(RunOnceKeySafe, null);
        }

        private static bool SetRunOnceKey(string valueName, string extraArgs, string exeOverride = null)
        {
            try
            {
                string exePath = exeOverride ?? Application.ExecutablePath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    return false;

                string value = "\"" + exePath + "\"";
                if (!string.IsNullOrEmpty(extraArgs))
                    value += " " + extraArgs;

                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(RunOncePath, writable: true))
                {
                    if (key == null) return false;
                    key.SetValue(valueName, value);
                }

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
                Log("SetRunOnceKey failed: " + ex.Message);
                return false;
            }
        }

        private static void DeleteRunOnceKey(string valueName)
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(RunOncePath, writable: true))
                    key?.DeleteValue(valueName, throwOnMissingValue: false);
            }
            catch { }
        }

        // ── Boot helpers ─────────────────────────────────────────────────
        private static void RemoveSafebootAndRestart()
        {
            int rc = RunSysCmd("bcdedit.exe", "/deletevalue {current} safeboot");
            if (rc != 0)
                Log("WARNING: bcdedit deletevalue safeboot failed: " + rc);

            try { RunSysCmd("bcdedit.exe", "/deletevalue {current} safebootalternateshell"); } catch { }

            RunSysCmd("shutdown.exe", "/r /t 10");
        }

        private static void FlushRegistry()
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(RunOncePath, writable: false))
                    key?.Flush();
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(StateRegKey, writable: false))
                    key?.Flush();
                Log("Registry flushed.");
            }
            catch (Exception ex) { Log("Registry flush: " + ex.Message); }
        }

        internal static void TryRollbackSafeboot()
        {
            try
            {
                RunSysCmd("bcdedit.exe", "/deletevalue {current} safeboot");
                RunSysCmd("bcdedit.exe", "/deletevalue {current} safebootalternateshell");
                RestoreAlternateShell();
                try { AutoLogon.Disable(); } catch { }
                ClearState("Armed");
            }
            catch { }
        }

        private static void TryRemoveSafeboot()
        {
            try { RunSysCmd("bcdedit.exe", "/deletevalue {current} safeboot"); } catch { }
            try { RunSysCmd("bcdedit.exe", "/deletevalue {current} safebootalternateshell"); } catch { }
        }

        // ── State persistence ────────────────────────────────────────────
        internal static string GetState(string name)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(StateRegKey))
                    return key?.GetValue(name) as string;
            }
            catch { return null; }
        }

        internal static void SetState(string name, string value)
        {
            using (var key = Registry.LocalMachine.CreateSubKey(StateRegKey))
                key.SetValue(name, value ?? "", RegistryValueKind.String);
        }

        internal static void ClearState(string name)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(StateRegKey, writable: true))
                    key?.DeleteValue(name, throwOnMissingValue: false);
            }
            catch { }
        }

        internal static string GetAndClearLastResult()
        {
            string r = GetState("LastResult");
            ClearState("LastResult");
            return r ?? "Unknown";
        }

        private static bool WasJustUninstalled()
        {
            string result = GetState("LastResult");
            return !string.IsNullOrEmpty(result);
        }

        // ── Completion flag (legacy compat) ──────────────────────────────
        private static void ClearCompletionFlag()
        {
            ClearState("SafeModeCompleted");
        }

        // ── System command runner ────────────────────────────────────────
        internal static int RunSysCmd(string exe, string args)
        {
            string fullPath = GetSysPath(exe);

            using (var p = Process.Start(new ProcessStartInfo(fullPath)
            {
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
            }))
            {
                if (p == null)
                    throw new InvalidOperationException("Failed to start " + exe);
                p.WaitForExit();
                return p.ExitCode;
            }
        }

        private static string GetSysPath(string exe)
        {
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (string.IsNullOrEmpty(winDir) || !Directory.Exists(winDir))
                throw new InvalidOperationException("Windows directory not found.");

            string sysDir = Environment.Is64BitProcess
                ? Path.Combine(winDir, "System32")
                : Path.Combine(winDir, "Sysnative");

            string fullPath = Path.Combine(sysDir, exe);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException(exe + " not found in system directory.", fullPath);
            return fullPath;
        }

        // ── ACL restriction ──────────────────────────────────────────────
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
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None, AccessControlType.Allow));
                security.AddAccessRule(new FileSystemAccessRule(systemSid,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None, AccessControlType.Allow));

                dirInfo.SetAccessControl(security);
            }
            catch (Exception ex)
            {
                Log("ACL restriction failed: " + ex.Message);
            }
        }

        // ── Logging with rotation ────────────────────────────────────────
        internal static void Log(string message)
        {
            try
            {
                string dir = LocalDir();
                Directory.CreateDirectory(dir);
                string logFile = LogFilePath;

                if (File.Exists(logFile))
                {
                    var fi = new FileInfo(logFile);
                    if (fi.Length > MaxLogBytes)
                    {
                        string old = logFile + ".old";
                        if (File.Exists(old)) File.Delete(old);
                        File.Move(logFile, old);
                    }
                }

                string entry = DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine;
                File.AppendAllText(logFile, entry);
            }
            catch { }
        }

        // ── Image loader ─────────────────────────────────────────────────
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

        internal static string SanitizeErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;
            string sanitized = Regex.Replace(message, @"[A-Za-z]:\\[^\s""']+", "[path]");
            sanitized = Regex.Replace(sanitized, @"HKEY_[A-Z_]+\\[^\s""']+", "[registry]");
            return sanitized;
        }

        // ── Helpers ──────────────────────────────────────────────────────
        internal static string LocalDir()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "FrmAvastUninstaller");
        }

        internal static bool IsSafeMode()
        {
            return SystemInformation.BootMode != BootMode.Normal;
        }

        internal static int ParseInt(string s)
        {
            int result;
            return int.TryParse(s, out result) ? result : 0;
        }

        internal static string GetArgValue(string[] args, string prefix)
        {
            if (args == null) return null;
            foreach (var a in args)
            {
                if (a.StartsWith(prefix + "=", StringComparison.OrdinalIgnoreCase))
                    return a.Substring(prefix.Length + 1);
                if (a.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase))
                    return a.Substring(prefix.Length + 1);
            }
            return null;
        }

        private static bool HasArg(string[] args, string flag)
        {
            if (args == null) return false;
            foreach (var a in args)
                if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
