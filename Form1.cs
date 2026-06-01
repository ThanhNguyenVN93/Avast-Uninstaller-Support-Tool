using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace frm_avast_uninstaller
{
    public partial class Form1 : Form
    {
        private Label  lblTitle;
        private Panel  divider;
        private Label  lblStatus;
        private Label  lblCountdown;
        private Button btnAction;
        private Timer  countdownTimer;
        private int    countdownSeconds = 10;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Text = Strings.FormTitle;

            lblTitle = new Label
            {
                AutoSize  = false,
                Location  = new Point(10, 18),
                Size      = new Size(480, 30),
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 26, 26),
                TextAlign = ContentAlignment.MiddleLeft,
                Text      = Strings.FormTitle,
            };
            Controls.Add(lblTitle);

            divider = new Panel
            {
                Location  = new Point(0, 58),
                Size      = new Size(500, 1),
                BackColor = Color.FromArgb(224, 224, 224),
            };
            Controls.Add(divider);

            lblStatus = MakeLabel(new Point(0, 68), new Size(500, 40), 11, FontStyle.Regular,
                Color.FromArgb(0, 120, 215), Strings.NormalModeStatus);
            Controls.Add(lblStatus);

            lblCountdown = MakeLabel(new Point(0, 108), new Size(500, 26), 10, FontStyle.Bold,
                Color.FromArgb(200, 0, 0), Strings.Countdown(countdownSeconds));
            Controls.Add(lblCountdown);

            btnAction = new Button
            {
                Text      = Strings.RestartButton,
                Location  = new Point(150, 144),
                Size      = new Size(200, 36),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 105, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Click += (s, ev) => RestartToSafeMode();
            Controls.Add(btnAction);

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

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdownSeconds--;
            lblCountdown.Text = Strings.Countdown(countdownSeconds);
            if (countdownSeconds <= 0)
            {
                countdownTimer.Stop();
                lblCountdown.Visible = false;
                RestartToSafeMode();
            }
        }

        private void RestartToSafeMode()
        {
            try
            {
                countdownTimer.Stop();
                lblStatus.Text       = Strings.Restarting;
                lblCountdown.Visible = false;
                btnAction.Enabled    = false;

                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key  = hklm.OpenSubKey(Program.RunOncePath, writable: true))
                    key.SetValue(Program.RunOnceKeySafe, "\"" + Application.ExecutablePath + "\"");

                Program.RunSysCmd("bcdedit.exe", "/set {current} safeboot minimal");
                Program.RunSysCmd("shutdown.exe", "/r /t 5");
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text    = Strings.ErrorPrefix + ex.Message;
                lblCountdown.Visible = true;
                btnAction.Enabled    = true;
                countdownTimer.Start();
            }
        }

        private static Label MakeLabel(Point loc, Size size, float fontSize,
            FontStyle style, Color fore, string text)
        {
            return new Label
            {
                AutoSize  = false,
                Location  = loc,
                Size      = size,
                Font      = new Font("Segoe UI", fontSize, style),
                ForeColor = fore,
                TextAlign = ContentAlignment.MiddleCenter,
                Text      = text,
            };
        }
    }
}
