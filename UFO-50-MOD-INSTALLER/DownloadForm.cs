namespace UFO_50_MOD_INSTALLER
{
    public partial class DownloadOptionsDialog : Form
    {
        public enum DownloadOption
        {
            Cancel,
            OverwriteExisting,
            SkipExisting
        }

        public DownloadOption Result { get; private set; } = DownloadOption.Cancel;

        public DownloadOptionsDialog() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            this.Text = "GameBanana Mod Download";
            this.Size = new Size(360, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Button btnOverwrite = new Button();
            btnOverwrite.Text = "Overwrite existing mods";
            btnOverwrite.Location = new Point(20, 20);
            btnOverwrite.Size = new Size(280, 40);
            btnOverwrite.Click += (s, e) => {
                Result = DownloadOption.OverwriteExisting;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnOverwrite);

            Button btnNewOnly = new Button();
            btnNewOnly.Text = "Download new mods only";
            btnNewOnly.Location = new Point(20, 80);
            btnNewOnly.Size = new Size(280, 40);
            btnNewOnly.Click += (s, e) => {
                Result = DownloadOption.SkipExisting;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnNewOnly);

            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(90, 160);
            btnCancel.Size = new Size(140, 40);
            btnCancel.Click += (s, e) => {
                Result = DownloadOption.Cancel;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnCancel);
        }
    }
}