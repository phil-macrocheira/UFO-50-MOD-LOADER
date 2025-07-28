namespace UFO_50_MOD_INSTALLER
{
    public partial class FileSelectionForm : Form
    {
        private ListBox listBoxFiles;
        private Button buttonOk;
        private Button buttonCancel;
        public ModFile SelectedFile { get; private set; }

        public FileSelectionForm(string modName, List<ModFile> files) {
            InitializeComponent();
            this.Text = $"Select a file for '{modName}'";
            listBoxFiles.DataSource = files;
            listBoxFiles.DisplayMember = "FileName";
        }

        private void InitializeComponent() {
            this.Size = new Size(400, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            listBoxFiles = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(63, 63, 70), ForeColor = Color.White, BorderStyle = BorderStyle.None };

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(5) };
            buttonOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
            buttonOk.FlatAppearance.BorderSize = 0;
            buttonCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(85, 85, 85), ForeColor = Color.White };
            buttonCancel.FlatAppearance.BorderSize = 0;

            buttonPanel.Controls.Add(buttonOk);
            buttonPanel.Controls.Add(buttonCancel);

            this.Controls.Add(listBoxFiles);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = buttonOk;
            this.CancelButton = buttonCancel;

            buttonOk.Click += (s, e) => {
                if (listBoxFiles.SelectedItem != null) {
                    SelectedFile = (ModFile)listBoxFiles.SelectedItem;
                }
            };
        }
    }
}