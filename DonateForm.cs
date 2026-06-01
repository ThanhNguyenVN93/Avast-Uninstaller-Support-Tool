using System;
using System.Drawing;
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

            if (_showCompletion)
            {
                var pnlBanner = new Panel
                {
                    Location  = new Point(0, 0),
                    Size      = new Size(560, 48),
                    BackColor = Color.FromArgb(232, 245, 233),
                };
                var lblDone = new Label
                {
                    AutoSize  = false,
                    Dock      = DockStyle.Fill,
                    Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = Color.FromArgb(27, 128, 60),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text      = Strings.UninstallBanner,
                };
                pnlBanner.Controls.Add(lblDone);
                Controls.Add(pnlBanner);
                y = 48;
            }

            var lblHeader = new Label
            {
                AutoSize  = false,
                Location  = new Point(0, y + 10),
                Size      = new Size(560, 26),
                Font      = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Text      = Strings.DonateThanks,
            };
            Controls.Add(lblHeader);
            y += 44;

            var divider = new Panel
            {
                Location  = new Point(0, y),
                Size      = new Size(560, 1),
                BackColor = Color.FromArgb(224, 224, 224),
            };
            Controls.Add(divider);
            y += 12;

            Controls.Add(MakeBankLabel(new Point(30, y), Strings.DonateTCB));
            pbTcb = MakeQrBox(new Point(30, y + 28));
            pbTcb.Image = Program.LoadImage("tcb.jpg");
            Controls.Add(pbTcb);

            Controls.Add(MakeBankLabel(new Point(300, y), Strings.DonateMoMo));
            pbMomo = MakeQrBox(new Point(300, y + 28));
            pbMomo.Image = Program.LoadImage("momo.jpg");
            Controls.Add(pbMomo);

            int btnY = y + 28 + 320 + 14;
            var btnClose = new Button
            {
                Text      = Strings.DonateClose,
                Location  = new Point(210, btnY),
                Size      = new Size(140, 34),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 105, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, ev) => Close();
            Controls.Add(btnClose);

            ClientSize = new Size(560, btnY + 34 + 14);
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
            Size      = new Size(230, 24),
            Font      = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 105, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            Text      = text,
        };

        private static PictureBox MakeQrBox(Point loc) => new PictureBox
        {
            SizeMode    = PictureBoxSizeMode.Zoom,
            Location    = loc,
            Size        = new Size(230, 320),
            BorderStyle = BorderStyle.FixedSingle,
        };
    }
}
