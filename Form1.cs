using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace frm_avast_uninstaller
{
    public partial class Form1 : Form
    {
        private Panel      pnlHeader;
        private PictureBox pbIcon;
        private Label      lblTitle;
        private Label      lblSubtitle;
        private Panel      pnlBody;
        private Label      lblStep;
        private Label      lblStatus;
        private ProgressBar pgbCountdown;
        private Label      lblCountdown;
        private Button     btnAction;
        private Button     btnCancel;
        private Panel      pnlFooter;
        private LinkLabel  lnkReport;
        private Label      lblVersion;
        private Timer      countdownTimer;
        private int        countdownSeconds = 10;
        private const int  MaxCountdown     = 10;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Text = Strings.FormTitle;
            BuildHeader();
            BuildBody();
            BuildFooter();

            countdownTimer = new Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            countdownTimer?.Stop();
            countdownTimer?.Dispose();
            base.OnFormClosing(e);
        }

        // ── Header ───────────────────────────────────────────────────────
        private void BuildHeader()
        {
            pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Color.FromArgb(23, 23, 23),
            };
            pnlHeader.Paint += (s, ev) =>
            {
                using (var brush = new LinearGradientBrush(pnlHeader.ClientRectangle,
                    Color.FromArgb(30, 30, 30), Color.FromArgb(50, 50, 50), 0f))
                    ev.Graphics.FillRectangle(brush, pnlHeader.ClientRectangle);
            };

            pbIcon = new PictureBox
            {
                Location = new Point(20, 16),
                Size     = new Size(40, 40),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image    = Program.LoadImage("app_icon.png"),
            };
            if (pbIcon.Image == null)
            {
                pbIcon.BackColor = Color.FromArgb(255, 105, 0);
            }
            pnlHeader.Controls.Add(pbIcon);

            lblTitle = new Label
            {
                AutoSize  = false,
                Location  = new Point(72, 14),
                Size      = new Size(400, 24),
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Text      = Strings.FormTitle,
            };
            pnlHeader.Controls.Add(lblTitle);

            lblSubtitle = new Label
            {
                AutoSize  = false,
                Location  = new Point(72, 40),
                Size      = new Size(400, 18),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(160, 160, 160),
                BackColor = Color.Transparent,
                Text      = Strings.HeaderSubtitle,
            };
            pnlHeader.Controls.Add(lblSubtitle);

            Controls.Add(pnlHeader);
        }

        // ── Body ─────────────────────────────────────────────────────────
        private void BuildBody()
        {
            pnlBody = new Panel
            {
                Location  = new Point(0, 72),
                Size      = new Size(540, 210),
                BackColor = Color.White,
            };

            lblStep = new Label
            {
                AutoSize  = false,
                Location  = new Point(30, 20),
                Size      = new Size(480, 20),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 105, 0),
                Text      = Strings.StepLabel,
            };
            pnlBody.Controls.Add(lblStep);

            lblStatus = new Label
            {
                AutoSize  = false,
                Location  = new Point(30, 46),
                Size      = new Size(480, 44),
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(50, 50, 50),
                Text      = Strings.NormalModeStatus,
            };
            pnlBody.Controls.Add(lblStatus);

            pgbCountdown = new ProgressBar
            {
                Location = new Point(30, 100),
                Size     = new Size(480, 8),
                Minimum  = 0,
                Maximum  = MaxCountdown,
                Value    = MaxCountdown,
                Style    = ProgressBarStyle.Continuous,
            };
            pnlBody.Controls.Add(pgbCountdown);

            lblCountdown = new Label
            {
                AutoSize  = false,
                Location  = new Point(30, 114),
                Size      = new Size(480, 22),
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 60, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Text      = Strings.Countdown(countdownSeconds),
            };
            pnlBody.Controls.Add(lblCountdown);

            btnAction = new Button
            {
                Text      = Strings.RestartButton,
                Location  = new Point(30, 150),
                Size      = new Size(230, 40),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 105, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 90, 0);
            btnAction.Click += (s, ev) => RestartToSafeMode();
            pnlBody.Controls.Add(btnAction);

            btnCancel = new Button
            {
                Text      = Strings.CancelButton,
                Location  = new Point(280, 150),
                Size      = new Size(230, 40),
                Font      = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.Click += (s, ev) => Close();
            pnlBody.Controls.Add(btnCancel);

            Controls.Add(pnlBody);
        }

        // ── Footer ───────────────────────────────────────────────────────
        private void BuildFooter()
        {
            pnlFooter = new Panel
            {
                Location  = new Point(0, 282),
                Size      = new Size(540, 36),
                BackColor = Color.FromArgb(248, 248, 248),
            };

            var topLine = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = Color.FromArgb(224, 224, 224),
            };
            pnlFooter.Controls.Add(topLine);

            lnkReport = new LinkLabel
            {
                AutoSize     = true,
                Location     = new Point(30, 10),
                Font         = new Font("Segoe UI", 8, FontStyle.Regular),
                LinkColor    = Color.FromArgb(0, 102, 204),
                Text         = Strings.ReportIssue,
            };
            lnkReport.LinkClicked += (s, ev) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(Strings.ReportIssueUrl)
                    {
                        UseShellExecute = true,
                    });
                }
                catch { }
            };
            pnlFooter.Controls.Add(lnkReport);

            lblVersion = new Label
            {
                AutoSize  = false,
                Location  = new Point(340, 10),
                Size      = new Size(180, 16),
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(160, 160, 160),
                TextAlign = ContentAlignment.MiddleRight,
                Text      = "v" + Application.ProductVersion,
            };
            pnlFooter.Controls.Add(lblVersion);

            Controls.Add(pnlFooter);
        }

        // ── Timer ────────────────────────────────────────────────────────
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdownSeconds--;
            pgbCountdown.Value   = Math.Max(0, countdownSeconds);
            lblCountdown.Text    = Strings.Countdown(countdownSeconds);
            if (countdownSeconds <= 0)
            {
                countdownTimer.Stop();
                lblCountdown.Visible   = false;
                pgbCountdown.Visible   = false;
                RestartToSafeMode();
            }
        }

        // ── Restart logic ────────────────────────────────────────────────
        private void RestartToSafeMode()
        {
            try
            {
                countdownTimer.Stop();
                lblStep.Text         = Strings.StepRestarting;
                lblStatus.Text       = Strings.Restarting;
                lblCountdown.Visible = false;
                pgbCountdown.Visible = false;
                btnAction.Enabled    = false;
                btnCancel.Enabled    = false;

                if (!SetRunOnceForSafeMode())
                    throw new InvalidOperationException(Strings.RunOnceWriteFailed);

                int exitCode = Program.RunSysCmd("bcdedit.exe", "/set {current} safeboot minimal");
                if (exitCode != 0)
                    throw new InvalidOperationException(Strings.BcdeditFailed(exitCode));

                Program.RunSysCmd("shutdown.exe", "/r /t 5");
                Close();
            }
            catch (Exception ex)
            {
                TryRollbackSafeboot();
                lblStep.Text           = Strings.StepError;
                lblStep.ForeColor      = Color.FromArgb(200, 0, 0);
                lblStatus.Text         = Strings.ErrorPrefix + Program.SanitizeErrorMessage(ex.Message);
                lblStatus.ForeColor    = Color.FromArgb(180, 0, 0);
                lblCountdown.Visible   = true;
                pgbCountdown.Visible   = true;
                btnAction.Enabled      = true;
                btnCancel.Enabled      = true;
                countdownTimer.Start();
            }
        }

        private static bool SetRunOnceForSafeMode()
        {
            try
            {
                using (var hklm = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(Program.RunOncePath, writable: true))
                {
                    if (key == null) return false;
                    string exePath = Application.ExecutablePath;
                    key.SetValue(Program.RunOnceKeySafe, "\"" + exePath + "\"");
                    var written = key.GetValue(Program.RunOnceKeySafe) as string;
                    return written != null && written.Contains(exePath);
                }
            }
            catch
            {
                return false;
            }
        }

        private static void TryRollbackSafeboot()
        {
            try { Program.RunSysCmd("bcdedit.exe", "/deletevalue {current} safeboot"); }
            catch { }
        }
    }
}
