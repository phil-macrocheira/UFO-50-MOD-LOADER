namespace UFO_50_Mod_Loader
{
    public partial class SettingsForm : Form
    {
        private CheckBox checkDarkMode;
        private CheckBox checkSelectFile;
        private CheckBox checkQuickInstall;
        private Button buttonSave;
        private Button buttonCancel;

        // --- NEW CONTROLS ---
        private Label labelGamePath;
        private TextBox textBoxGamePath;
        private Button buttonBrowse;

        public SettingsForm() {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent() {
            this.Text = "Settings";
            this.Size = new Size(510, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;

            checkDarkMode = new CheckBox { Text = "Dark Mode", Location = new Point(20, 20), AutoSize = true };
            checkSelectFile = new CheckBox { Text = "Select file when downloading mod", Location = new Point(20, 50), AutoSize = true };
            checkQuickInstall = new CheckBox { Text = "Skip \"Mods Installed\" message", Location = new Point(20, 80), AutoSize = true };

            // --- NEW CONTROLS INITIALIZATION ---
            labelGamePath = new Label { Text = "UFO 50 Game Path:", Location = new Point(20, 120), AutoSize = true };
            textBoxGamePath = new TextBox { Location = new Point(20, 156), Size = new Size(350, 23) };
            buttonBrowse = new Button { Text = "Browse...", Location = new Point(380, 154), Size = new Size(90, 40) };
            buttonBrowse.Click += ButtonBrowse_Click;

            buttonSave = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(280, 210), Size = new Size(90, 40) };
            buttonCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(380, 210), Size = new Size(90, 40) };

            this.Controls.AddRange(new Control[] { checkDarkMode, checkSelectFile, checkQuickInstall, labelGamePath, textBoxGamePath, buttonBrowse, buttonSave, buttonCancel });
            this.AcceptButton = buttonSave;
            this.CancelButton = buttonCancel;
        }

        private void LoadSettings() {
            checkDarkMode.Checked = SettingsService.Settings.DarkModeEnabled;
            checkSelectFile.Checked = SettingsService.Settings.AlwaysSelectFile;
            checkQuickInstall.Checked = SettingsService.Settings.QuickInstall;
            textBoxGamePath.Text = SettingsService.Settings.GamePath;
        }

        public void SaveSettings() {
            SettingsService.Settings.DarkModeEnabled = checkDarkMode.Checked;
            SettingsService.Settings.AlwaysSelectFile = checkSelectFile.Checked;
            SettingsService.Settings.QuickInstall = checkQuickInstall.Checked;
            SettingsService.Settings.GamePath = textBoxGamePath.Text;
            SettingsService.Save();
        }

        private void ButtonBrowse_Click(object sender, EventArgs e) {
            using (var folderDialog = new FolderBrowserDialog()) {
                folderDialog.Description = "Select the UFO 50 installation folder";
                if (folderDialog.ShowDialog() == DialogResult.OK) {
                    // Basic validation to check if it looks like the right folder
                    if (File.Exists(Path.Combine(folderDialog.SelectedPath, "ufo50.exe"))) {
                        textBoxGamePath.Text = folderDialog.SelectedPath;
                    }
                    else {
                        MessageBox.Show("This doesn't appear to be a valid UFO 50 folder (ufo50.exe not found).", "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
    }
}