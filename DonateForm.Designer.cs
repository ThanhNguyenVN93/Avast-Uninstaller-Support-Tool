namespace frm_avast_uninstaller
{
    partial class DonateForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components      = new System.ComponentModel.Container();
            this.AutoScaleMode   = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize      = new System.Drawing.Size(560, 468);
            this.Text            = Strings.DonateTitle;
            this.BackColor       = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            try
            {
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Windows.Forms.Application.ExecutablePath);
            }
            catch { }
        }

        #endregion
    }
}
