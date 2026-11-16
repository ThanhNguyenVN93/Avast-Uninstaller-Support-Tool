using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace frm_avast_uninstaller
{
    public partial class DonateForm : Form
    {
        private readonly bool _showCompletion;
        private PictureBox pbTcb;
        private PictureBox pbMomo;

        public DonateForm(bool showCompletion = false)
        {
            _showCompletion = showCompletion;
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int y = 0;

            // ── Success banner ───────────────────────────────────────────
            if (_showCompletion)
            {
                var pnlBanner = new Panel
                {
                    Location  = new Point(0, 0),
                    Size      = new Size(580, 56),
                    BackColor = Color.FromArgb(232, 245, 233),
                };
                pnlBanner.Paint += (s, ev) =>
                {
                    using (var brush = new LinearGradientBrush(pnlBanner.ClientRectangle,
                        Color.FromArgb(232, 245, 233), Color.FromArgb(200, 235, 205), 0f))
                        ev.Graphics.FillRectangle(brush, pnlBanner.ClientRectangle);
                };

                var lblCheck = new Label
                {
                    AutoSize  = false,
                    Location  = new Point(20, 8),
                    Size      = new Size(40, 40),
                    Font      = new Font("Segoe UI", 20, FontStyle.Bold),
                    ForeColor = Color.FromArgb(27, 128, 60),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text      = "✓",
                };
                pnlBanner.Controls.Add(lblCheck);

                var lblDone = new Label
                {
                    AutoSize  = false,
                    Location  = new Point(60, 8),
                    Size      = new Size(500, 40),
                    Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.FromArgb(27, 128, 60),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Text      = Loc.T("uninstall_banner"),
                };
                pnlBanner.Controls.Add(lblDone);
                Controls.Add(pnlBanner);
                y = 56;
            }

            // ── Thank you header ─────────────────────────────────────────
            var lblHeader = new Label
            {
                AutoSize  = false,
                Location  = new Point(0, y + 12),
                Size      = new Size(580, 28),
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter,
                Text      = Loc.T("donate_thanks"),
            };
            Controls.Add(lblHeader);
            y += 50;

            var divider = new Panel
            {
                Location  = new Point(20, y),
                Size      = new Size(540, 1),
                BackColor = Color.FromArgb(230, 230, 230),
            };
            Controls.Add(divider);
            y += 16;

            // ── QR columns ───────────────────────────────────────────────
            int colLeft = 40, colRight = 310;

            Controls.Add(MakeBankLabel(new Point(colLeft, y), Loc.T("donate_tcb")));
            pbTcb = MakeQrBox(new Point(colLeft, y + 30));
            pbTcb.Image = Program.LoadImage("tcb.jpg");
            Controls.Add(pbTcb);

            Controls.Add(MakeBankLabel(new Point(colRight, y), Loc.T("donate_momo")));
            pbMomo = MakeQrBox(new Point(colRight, y + 30));
            pbMomo.Image = Program.LoadImage("momo.jpg");
            Controls.Add(pbMomo);

            int btnY = y + 30 + 300 + 20;

            // ── Footer divider ───────────────────────────────────────────
            var divider2 = new Panel
            {
                Location  = new Point(20, btnY - 6),
                Size      = new Size(540, 1),
                BackColor = Color.FromArgb(230, 230, 230),
            };
            Controls.Add(divider2);
            btnY += 6;

            // ── Buttons row ──────────────────────────────────────────────
            var btnClose = new Button
            {
                Text      = Loc.T("donate_close"),
                Location  = new Point(310, btnY),
                Size      = new Size(230, 38),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 105, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 90, 0);
            btnClose.Click += (s, ev) => Close();
            Controls.Add(btnClose);

            // ── Report Issues link ───────────────────────────────────────
            var lnkReport = new LinkLabel
            {
                AutoSize  = true,
                Location  = new Point(40, btnY + 10),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                LinkColor = Color.FromArgb(0, 102, 204),
                Text      = Loc.T("btn_report"),
            };
            lnkReport.LinkClicked += (s, ev) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(Loc.ReportIssuesUrl)
                    {
                        UseShellExecute = true,
                    });
                }
                catch { }
            };
            Controls.Add(lnkReport);

            ClientSize = new Size(580, btnY + 38 + 16);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            pbTcb?.Image?.Dispose();
            pbMomo?.Image?.Dispose();
            base.OnFormClosing(e);
        }

        private static Label MakeBankLabel(Point loc, string text) => new Label
        {
            AutoSize  = false,
            Location  = loc,
            Size      = new Size(230, 26),
            Font      = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 105, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            Text      = text,
        };

        private static PictureBox MakeQrBox(Point loc) => new PictureBox
        {
            SizeMode    = PictureBoxSizeMode.Zoom,
            Location    = loc,
            Size        = new Size(230, 300),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor   = Color.FromArgb(250, 250, 250),
        };
    }
}
