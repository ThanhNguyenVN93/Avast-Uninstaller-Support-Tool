using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace frm_avast_uninstaller
{
    internal sealed class ResultForm : Form
    {
        internal ResultForm()
        {
            Text = Loc.T("result_title");
            ClientSize = new Size(520, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = false;
            MaximizeBox = false;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            string result = Program.GetAndClearLastResult();
            bool avastPresent = Program.AvastInstalled();
            string logPath = Program.LogFilePath;
            bool hasLog = File.Exists(logPath);

            string headline;
            Color headColor;
            switch (result)
            {
                case "Success":
                    headline = avastPresent ? Loc.T("result_warning") : Loc.T("result_success");
                    headColor = avastPresent ? Color.FromArgb(255, 193, 7) : Color.FromArgb(76, 175, 80);
                    break;
                case "Error":
                    headline = Loc.T("result_error");
                    headColor = Color.FromArgb(244, 67, 54);
                    break;
                case "None":
                    headline = Loc.T("result_none");
                    headColor = Color.FromArgb(33, 150, 243);
                    break;
                default:
                    headline = Loc.T("result_unknown");
                    headColor = Color.Gray;
                    break;
            }

            var lblHead = new Label
            {
                Text = headline,
                ForeColor = headColor,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(496, 60),
                Location = new Point(12, 12),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            Controls.Add(lblHead);

            var txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Consolas", 8.5f),
                Location = new Point(12, 80),
                Size = new Size(496, 180),
                BorderStyle = BorderStyle.FixedSingle,
            };

            if (hasLog)
            {
                try
                {
                    string[] lines = File.ReadAllLines(logPath);
                    int skip = Math.Max(0, lines.Length - 40);
                    txtLog.Text = string.Join(Environment.NewLine, lines, skip, lines.Length - skip);
                    txtLog.SelectionStart = txtLog.Text.Length;
                }
                catch { txtLog.Text = Loc.T("result_nolog"); }
            }
            else
            {
                txtLog.Text = Loc.T("result_nolog");
            }
            Controls.Add(txtLog);

            int y = 274;
            int x = 12;

            var btnOpenLog = MakeButton(Loc.T("result_openlog"), x, y, 150);
            btnOpenLog.Click += (s, e) =>
            {
                if (hasLog)
                    try { Process.Start(new ProcessStartInfo(logPath) { UseShellExecute = true }); } catch { }
            };
            btnOpenLog.Enabled = hasLog;
            Controls.Add(btnOpenLog);

            var btnOpenFolder = MakeButton(Loc.T("result_openfolder"), x + 160, y, 150);
            btnOpenFolder.Click += (s, e) =>
            {
                string dir = Path.GetDirectoryName(logPath);
                if (Directory.Exists(dir))
                    try { Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true }); } catch { }
            };
            Controls.Add(btnOpenFolder);

            y += 42;
            var btnRestoreNet = MakeButton(Loc.T("result_restorenet"), x, y, 150);
            btnRestoreNet.Click += (s, e) => RestoreNetwork();
            Controls.Add(btnRestoreNet);

            var btnDiag = MakeButton(Loc.T("result_diag"), x + 160, y, 150);
            btnDiag.Click += (s, e) => CollectDiagnostics();
            Controls.Add(btnDiag);

            var btnReport = MakeButton(Loc.T("btn_report"), x + 320, y, 150);
            btnReport.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(Loc.ReportIssuesUrl) { UseShellExecute = true }); } catch { }
            };
            Controls.Add(btnReport);

            y += 52;
            var btnClose = MakeButton(Loc.T("result_close"), 400, y, 108);
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
        }

        private static Button MakeButton(string text, int x, int y, int w)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
            };
        }

        private void RestoreNetwork()
        {
            string backup = Path.Combine(Program.LocalDir(), "network_backup.txt");
            if (File.Exists(backup))
            {
                try
                {
                    Program.RunSysCmd("netsh", "-f \"" + backup + "\"");
                    MessageBox.Show(Loc.T("restored_ok"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(Loc.T("restored_fail"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CollectDiagnostics()
        {
            try
            {
                string dir = Program.LocalDir();
                string zipPath = Path.Combine(dir, "diagnostics_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip");

                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    string logPath = Program.LogFilePath;
                    if (File.Exists(logPath))
                        zip.CreateEntryFromFile(logPath, Path.GetFileName(logPath));

                    string oldLog = logPath + ".old";
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
                        w.WriteLine("64bit OS: " + Environment.Is64BitOperatingSystem);
                        w.WriteLine("User: " + Environment.UserName);
                        w.WriteLine("Machine: " + Environment.MachineName);
                        w.WriteLine("CLR: " + Environment.Version);
                        w.WriteLine("SafeMode: " + Program.IsSafeMode());
                        w.WriteLine("AvastInstalled: " + Program.AvastInstalled());
                    }
                }

                MessageBox.Show(string.Format(Loc.T("diag_done"), zipPath), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
