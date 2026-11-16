using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace frm_avast_uninstaller
{
    public partial class Form1 : Form
    {
        private Panel pnlHeader;
        private PictureBox pbIcon;
        private Label lblTitle;
        private Label lblSubtitle;
        private Panel pnlBody;
        private Label lblStep;
        private Label lblStatus;
        private ProgressBar pgbCountdown;
        private Label lblCountdown;
        private Button btnAction;
        private Button btnCancel;
        private Button btnNoSafeMode;
        private Button btnLang;
        private Button btnAbout;
        private CheckBox chkClean;
        private LinkLabel lnkUpdate;
        private Panel pnlFooter;
        private LinkLabel lnkReport;
        private Label lblVersion;

        private Timer countdownTimer;
        private int countdownSeconds = 10;
        private const int MaxCountdown = 10;

        private bool _abortPhase;
        private Updater.ReleaseInfo _pendingUpdate;
        private string _stagedUpdatePath;
        private ToolTip _toolTip;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyLocale();
            BuildHeader();
            BuildBody();
            BuildFooter();

            _toolTip = new ToolTip { AutoPopDelay = 8000, InitialDelay = 400 };
            _toolTip.SetToolTip(btnAction, Loc.T("tip_restart"));
            _toolTip.SetToolTip(btnNoSafeMode, Loc.T("tip_nosafemode"));
            _toolTip.SetToolTip(chkClean, Loc.T("tip_clean"));

            bool avast = Program.AvastCoreInstalled();
            if (!avast)
            {
                lblStatus.Text = Loc.T("no_avast_normal");
                countdownSeconds = 0;
                pgbCountdown.Visible = false;
                lblCountdown.Visible = false;
            }
            else
            {
                countdownTimer = new Timer { Interval = 1000 };
                countdownTimer.Tick += CountdownTimer_Tick;
                countdownTimer.Start();
            }

            System.Threading.ThreadPool.QueueUserWorkItem(_ => CheckForUpdates());
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            countdownTimer?.Stop();
            countdownTimer?.Dispose();
            _toolTip?.Dispose();
            base.OnFormClosing(e);
        }

        private void ApplyLocale()
        {
            Text = Loc.T("app_title");
        }

        private void RefreshAllText()
        {
            ApplyLocale();
            lblTitle.Text = Loc.T("app_title");
            lblSubtitle.Text = Loc.T("header_subtitle");
            lblStep.Text = Loc.T("step_label");

            if (!_abortPhase)
            {
                bool avast = Program.AvastCoreInstalled();
                lblStatus.Text = avast ? Loc.T("status_normal") : Loc.T("no_avast_normal");
            }

            btnAction.Text = Loc.T("btn_restart");
            btnCancel.Text = Loc.T("btn_cancel");
            btnNoSafeMode.Text = Loc.T("btn_nosafemode");
            btnLang.Text = Loc.T("btn_lang");
            btnAbout.Text = Loc.T("btn_about");
            chkClean.Text = Loc.T("chk_clean");
            lnkReport.Text = Loc.T("btn_report");
            lblVersion.Text = "v" + Program.AppVersion;
            lblCountdown.Text = string.Format(Loc.T("countdown"), countdownSeconds);

            _toolTip.SetToolTip(btnAction, Loc.T("tip_restart"));
            _toolTip.SetToolTip(btnNoSafeMode, Loc.T("tip_nosafemode"));
            _toolTip.SetToolTip(chkClean, Loc.T("tip_clean"));

            if (_pendingUpdate != null && lnkUpdate != null)
                lnkUpdate.Text = string.Format(Loc.T("update_available"), _pendingUpdate.TagName);
        }

        // ── Header ───────────────────────────────────────────────────────
        private void BuildHeader()
        {
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
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
                Size = new Size(40, 40),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Program.LoadImage("app_icon.png"),
            };
            if (pbIcon.Image == null)
                pbIcon.BackColor = Color.FromArgb(255, 105, 0);
            pnlHeader.Controls.Add(pbIcon);

            lblTitle = new Label
            {
                AutoSize = false, Location = new Point(72, 14),
                Size = new Size(340, 24),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                Text = Loc.T("app_title"),
            };
            pnlHeader.Controls.Add(lblTitle);

            lblSubtitle = new Label
            {
                AutoSize = false, Location = new Point(72, 40),
                Size = new Size(340, 18),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 160, 160),
                BackColor = Color.Transparent,
                Text = Loc.T("header_subtitle"),
            };
            pnlHeader.Controls.Add(lblSubtitle);

            btnLang = new Button
            {
                Text = Loc.T("btn_lang"),
                Location = new Point(440, 18),
                Size = new Size(80, 28),
                Font = new Font("Segoe UI", 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
            };
            btnLang.FlatAppearance.BorderSize = 0;
            btnLang.Click += (s, ev) => { Loc.Toggle(); RefreshAllText(); };
            pnlHeader.Controls.Add(btnLang);

            Controls.Add(pnlHeader);
        }

        // ── Body ─────────────────────────────────────────────────────────
        private void BuildBody()
        {
            pnlBody = new Panel
            {
                Location = new Point(0, 72),
                Size = new Size(560, 270),
                BackColor = Color.White,
            };

            lblStep = new Label
            {
                AutoSize = false, Location = new Point(30, 16),
                Size = new Size(500, 20),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 105, 0),
                Text = Loc.T("step_label"),
            };
            pnlBody.Controls.Add(lblStep);

            lblStatus = new Label
            {
                AutoSize = false, Location = new Point(30, 40),
                Size = new Size(500, 44),
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(50, 50, 50),
                Text = Loc.T("status_normal"),
            };
            pnlBody.Controls.Add(lblStatus);

            pgbCountdown = new ProgressBar
            {
                Location = new Point(30, 92),
                Size = new Size(500, 8),
                Minimum = 0, Maximum = MaxCountdown,
                Value = MaxCountdown,
                Style = ProgressBarStyle.Continuous,
            };
            pnlBody.Controls.Add(pgbCountdown);

            lblCountdown = new Label
            {
                AutoSize = false, Location = new Point(30, 104),
                Size = new Size(500, 22),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 60, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = string.Format(Loc.T("countdown"), countdownSeconds),
            };
            pnlBody.Controls.Add(lblCountdown);

            chkClean = new CheckBox
            {
                Text = Loc.T("chk_clean"),
                Location = new Point(30, 130),
                Size = new Size(500, 22),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 80, 80),
                Checked = true,
            };
            pnlBody.Controls.Add(chkClean);

            btnAction = new Button
            {
                Text = Loc.T("btn_restart"),
                Location = new Point(30, 162),
                Size = new Size(240, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 105, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 90, 0);
            btnAction.Click += (s, ev) => OnRestartClicked();
            pnlBody.Controls.Add(btnAction);

            btnCancel = new Button
            {
                Text = Loc.T("btn_cancel"),
                Location = new Point(290, 162),
                Size = new Size(240, 40),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.Click += (s, ev) =>
            {
                if (_abortPhase) AbortRestart();
                else Close();
            };
            pnlBody.Controls.Add(btnCancel);

            btnNoSafeMode = new Button
            {
                Text = Loc.T("btn_nosafemode"),
                Location = new Point(30, 214),
                Size = new Size(500, 32),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
            };
            btnNoSafeMode.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 210);
            btnNoSafeMode.Click += (s, ev) => OnNoSafeModeClicked();
            pnlBody.Controls.Add(btnNoSafeMode);

            Controls.Add(pnlBody);
        }

        // ── Footer ───────────────────────────────────────────────────────
        private void BuildFooter()
        {
            pnlFooter = new Panel
            {
                Location = new Point(0, 342),
                Size = new Size(560, 48),
                BackColor = Color.FromArgb(248, 248, 248),
            };

            var topLine = new Panel
            {
                Dock = DockStyle.Top, Height = 1,
                BackColor = Color.FromArgb(224, 224, 224),
            };
            pnlFooter.Controls.Add(topLine);

            lnkReport = new LinkLabel
            {
                AutoSize = true, Location = new Point(16, 10),
                Font = new Font("Segoe UI", 8),
                LinkColor = Color.FromArgb(0, 102, 204),
                Text = Loc.T("btn_report"),
            };
            lnkReport.LinkClicked += (s, ev) =>
            {
                try { Process.Start(new ProcessStartInfo(Loc.ReportIssuesUrl) { UseShellExecute = true }); }
                catch { }
            };
            pnlFooter.Controls.Add(lnkReport);

            lnkUpdate = new LinkLabel
            {
                AutoSize = true, Location = new Point(16, 28),
                Font = new Font("Segoe UI", 7.5f),
                LinkColor = Color.FromArgb(0, 140, 70),
                Visible = false,
            };
            lnkUpdate.LinkClicked += (s, ev) => ApplyPendingUpdate();
            pnlFooter.Controls.Add(lnkUpdate);

            btnAbout = new Button
            {
                Text = Loc.T("btn_about"),
                Location = new Point(380, 8),
                Size = new Size(70, 28),
                Font = new Font("Segoe UI", 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(100, 100, 100),
                Cursor = Cursors.Hand,
            };
            btnAbout.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnAbout.Click += (s, ev) =>
            {
                string text = string.Format(Loc.T("about_text"), Program.AppVersion, Loc.ReportIssuesUrl);
                MessageBox.Show(text, Loc.T("btn_about"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlFooter.Controls.Add(btnAbout);

            lblVersion = new Label
            {
                AutoSize = false, Location = new Point(460, 14),
                Size = new Size(90, 16),
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(160, 160, 160),
                TextAlign = ContentAlignment.MiddleRight,
                Text = "v" + Program.AppVersion,
            };
            pnlFooter.Controls.Add(lblVersion);

            Controls.Add(pnlFooter);
            ClientSize = new Size(560, 390);
        }

        // ── Timer ────────────────────────────────────────────────────────
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdownSeconds--;
            pgbCountdown.Value = Math.Max(0, countdownSeconds);
            lblCountdown.Text = string.Format(Loc.T("countdown"), countdownSeconds);

            if (_abortPhase)
            {
                lblStatus.Text = string.Format(Loc.T("abort_text"), countdownSeconds);
            }

            if (countdownSeconds <= 0)
            {
                countdownTimer.Stop();
                if (_abortPhase)
                    Close();
                else
                    OnRestartClicked();
            }
        }

        // ── Restart to Safe Mode ─────────────────────────────────────────
        private void OnRestartClicked()
        {
            countdownTimer?.Stop();

            var dr = MessageBox.Show(Loc.T("preflight"), Loc.T("preflight_title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes)
            {
                countdownSeconds = MaxCountdown;
                pgbCountdown.Value = MaxCountdown;
                lblCountdown.Text = string.Format(Loc.T("countdown"), countdownSeconds);
                countdownTimer?.Start();
                return;
            }

            try
            {
                lblStep.Text = Loc.T("step_restarting");
                lblStatus.Text = Loc.T("status_preparing");
                lblCountdown.Visible = false;
                pgbCountdown.Visible = false;
                btnAction.Enabled = false;
                btnNoSafeMode.Enabled = false;

                Program.SetState("CleanLeftovers", chkClean.Checked ? "1" : "0");

                lblStatus.Text = Loc.T("status_downloading");
                Application.DoEvents();
                Program.TryStageLatestUninstaller();

                lblStatus.Text = Loc.T("status_preparing");
                Application.DoEvents();
                Program.CreateRestorePoint();
                Program.BackupNetworkSettings();

                string password = null;
                if (PasswordPrompt.TryGet(out password))
                    Program.Log("User provided password for autologon.");

                Program.ArmSafeMode(password);

                EnterAbortPhase();
            }
            catch (Exception ex)
            {
                Program.TryRollbackSafeboot();
                lblStep.Text = Loc.T("step_error");
                lblStep.ForeColor = Color.FromArgb(200, 0, 0);
                lblStatus.Text = Loc.T("err_prefix") + Program.SanitizeErrorMessage(ex.Message);
                lblStatus.ForeColor = Color.FromArgb(180, 0, 0);
                btnAction.Enabled = true;
                btnNoSafeMode.Enabled = true;
                pgbCountdown.Visible = true;
                lblCountdown.Visible = true;
                countdownSeconds = MaxCountdown;
                pgbCountdown.Value = MaxCountdown;
                countdownTimer?.Start();
            }
        }

        private void EnterAbortPhase()
        {
            _abortPhase = true;
            countdownSeconds = 20;
            pgbCountdown.Maximum = 20;
            pgbCountdown.Value = 20;
            pgbCountdown.Visible = true;
            lblCountdown.Visible = true;
            lblStatus.Text = string.Format(Loc.T("abort_text"), countdownSeconds);
            lblStatus.ForeColor = Color.FromArgb(50, 50, 50);

            btnCancel.Text = Loc.T("abort_btn");
            btnCancel.BackColor = Color.FromArgb(244, 67, 54);
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Enabled = true;

            btnAction.Visible = false;
            btnNoSafeMode.Visible = false;

            countdownTimer = countdownTimer ?? new Timer { Interval = 1000 };
            countdownTimer.Tick -= CountdownTimer_Tick;
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();

            Program.RunSysCmd("shutdown.exe", "/r /t 20");
        }

        private void AbortRestart()
        {
            try
            {
                countdownTimer?.Stop();
                Program.RunSysCmd("shutdown.exe", "/a");
                Program.UndoAllBootTriggers();

                _abortPhase = false;
                lblStep.Text = Loc.T("step_label");
                lblStep.ForeColor = Color.FromArgb(255, 105, 0);
                lblStatus.Text = Loc.T("aborted");
                lblStatus.ForeColor = Color.FromArgb(50, 50, 50);
                pgbCountdown.Visible = false;
                lblCountdown.Visible = false;

                btnCancel.Text = Loc.T("btn_cancel");
                btnCancel.BackColor = Color.FromArgb(240, 240, 240);
                btnCancel.ForeColor = Color.FromArgb(80, 80, 80);
                btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                btnCancel.FlatAppearance.BorderSize = 1;

                btnAction.Visible = true;
                btnAction.Enabled = true;
                btnNoSafeMode.Visible = true;
                btnNoSafeMode.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Loc.T("err_prefix") + ex.Message, Loc.T("app_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── No Safe Mode removal ─────────────────────────────────────────
        private void OnNoSafeModeClicked()
        {
            countdownTimer?.Stop();

            var dr = MessageBox.Show(Loc.T("nosafemode_confirm"), Loc.T("nosafemode_title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes)
            {
                countdownSeconds = MaxCountdown;
                pgbCountdown.Value = MaxCountdown;
                countdownTimer?.Start();
                return;
            }

            lblStep.Text = Loc.T("step_restarting");
            lblStatus.Text = Loc.T("working");
            btnAction.Enabled = false;
            btnNoSafeMode.Enabled = false;
            btnCancel.Enabled = false;
            pgbCountdown.Visible = false;
            lblCountdown.Visible = false;
            Application.DoEvents();

            Program.RunEmbeddedUninstaller(interactive: true);

            if (chkClean.Checked && !Program.AvastCoreInstalled())
                Program.CleanAvastLeftovers();

            bool still = Program.AvastInstalled();
            if (!still)
            {
                Application.Run(new DonateForm(showCompletion: true));
                Close();
            }
            else
            {
                lblStep.Text = Loc.T("step_label");
                lblStatus.Text = Loc.T("result_warning");
                btnAction.Enabled = true;
                btnNoSafeMode.Enabled = true;
                btnCancel.Enabled = true;
                pgbCountdown.Visible = true;
                lblCountdown.Visible = true;
                countdownSeconds = MaxCountdown;
                pgbCountdown.Value = MaxCountdown;
                countdownTimer?.Start();
            }
        }

        // ── Update check ─────────────────────────────────────────────────
        private void CheckForUpdates()
        {
            try
            {
                var info = Updater.CheckForUpdate();
                if (info == null) return;

                _pendingUpdate = info;
                if (IsHandleCreated)
                {
                    BeginInvoke(new Action(() =>
                    {
                        lnkUpdate.Text = string.Format(Loc.T("update_available"), info.TagName);
                        lnkUpdate.Visible = true;
                    }));
                }
            }
            catch { }
        }

        private void ApplyPendingUpdate()
        {
            if (_pendingUpdate == null) return;

            lnkUpdate.Text = Loc.T("update_downloading");
            lnkUpdate.Enabled = false;
            Application.DoEvents();

            string staged;
            if (!Updater.DownloadAndStage(_pendingUpdate, out staged))
            {
                lnkUpdate.Text = Loc.T("update_fail");
                lnkUpdate.Enabled = true;
                return;
            }

            _stagedUpdatePath = staged;

            var dr = MessageBox.Show(
                string.Format(Loc.T("update_confirm"), _pendingUpdate.TagName),
                Loc.T("update_confirm_title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                Updater.ApplyUpdate(_stagedUpdatePath);
                Application.Exit();
            }
            else
            {
                lnkUpdate.Text = string.Format(Loc.T("update_available"), _pendingUpdate.TagName);
                lnkUpdate.Enabled = true;
            }
        }
    }
}
