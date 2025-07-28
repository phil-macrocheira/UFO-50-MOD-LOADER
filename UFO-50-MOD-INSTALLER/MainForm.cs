using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace UFO_50_MOD_INSTALLER
{
    public partial class MainForm : Form
    {
        private string? currentPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        private string? gamePath = "";
        private string? modsPath = "";
        public string? data_winPath = "";
        public string? exePath = "";
        public string? localizationPath = "";
        public string? vanilla_localizationPath = "";
        private Image defaultIcon = null!;
        private ModInstaller modInstaller = new ModInstaller();
        private ModDownloader modDownloader = new ModDownloader();
        private ConflictChecker conflictChecker = new ConflictChecker();
        public bool conflictsExist = true;
        public string? conflictsText = "";
        public List<string> enabledMods = new List<string>();
        public bool DOWNLOADING_MODS = false;

        public MainForm()
        {
            InitializeComponent();
            this.Size = SettingsService.Settings.MainWindowSize; // Restore window size
            
            Load += (s, e) => InitializeApplication();
            FormClosing += (s, e) => SaveModStates(); // Save settings on close
            Resize += (s, e) => ResizeControls();
            buttonInstall.Click += (s, e) => installMods();
            buttonDownload.Click += async (s, e) => await downloadMods();
            buttonLaunch.Click += (s, e) => LaunchGame();
            buttonSettings.Click += (s, e) => OpenSettings(); // New settings button event
        }
        private void SaveModStates()
        {
            SettingsService.Settings.MainWindowSize = this.Size;
            SettingsService.Settings.EnabledMods.Clear();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if ((bool)row.Cells[0].Value)
                {
                    SettingsService.Settings.EnabledMods.Add(row.Cells[2].Value.ToString());
                }
            }
            SettingsService.Save();
        }

        private void OpenSettings()
        {
            using (var settingsForm = new SettingsForm())
            {
                // Apply theme to the settings form as well
                bool isDarkMode = SettingsService.Settings.DarkModeEnabled;
                settingsForm.BackColor = isDarkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
                settingsForm.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;

                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    settingsForm.SaveSettings();
                    ApplyTheme();
                }
            }
        }
        private void ResizeControls() {
            dataGridView1.Size = new Size(ClientSize.Width - 24, ClientSize.Height - textBox1.Height - 24 - 74);
            textBox1.Location = new Point(12, ClientSize.Height - textBox1.Height - 12);
        }
        private void InitializeApplication() {
            CheckGamePath();
            GetVanillaWin();
            //GetLocalization();
            InitializeUI();
            InitializeFileSystemWatcher();
            CleanupMods();
            LoadMods();
            CheckForConflicts();
            if (SettingsService.Settings.IsFirstRun)
            {
                SettingsService.Settings.IsFirstRun = false;
            }
        }

        private void CheckGamePath()
        {
            // Use the centralized settings service to get the game path
            if (!string.IsNullOrEmpty(SettingsService.Settings.GamePath) && IsValidGamePath(SettingsService.Settings.GamePath))
            {
                gamePath = SettingsService.Settings.GamePath;
                return;
            }

            // Fallback to searching if the saved path is missing or invalid
            string[] possiblePaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "UFO 50"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "UFO 50")
            };

            foreach (string path in possiblePaths)
            {
                if (IsValidGamePath(path))
                {
                    gamePath = path;
                    SettingsService.Settings.GamePath = gamePath; // Save the found path
                    SettingsService.Save();
                    return;
                }
            }

            // If still not found, prompt the user
            while (true)
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    MessageBox.Show("UFO 50 install folder not found. Please select the game's installation folder.");
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        if (IsValidGamePath(folderDialog.SelectedPath))
                        {
                            gamePath = folderDialog.SelectedPath;
                            SettingsService.Settings.GamePath = gamePath; // Save the user's selected path
                            SettingsService.Save();
                            return;
                        }
                    }
                    else
                    {
                        Application.Exit();
                        return;
                    }
                }
            }
        }
        private void GetVanillaWin() {
            string vanillaPath = Path.Combine(currentPath, "vanilla.win");
            string iniPath = Path.Combine(currentPath, "GMLoader.ini");

            if (!modInstaller.checkVanillaWin(vanillaPath)) {
                File.Copy(data_winPath, vanillaPath);

                if (!modInstaller.checkVanillaHash(data_winPath, iniPath)) {
                    MessageBox.Show("Currently installed version of UFO 50 is either outdated or modded. If it is modded, please replace the vanilla.win in this folder with an unmodded data.win file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (!modInstaller.checkVanillaHash(vanillaPath, iniPath)) {
                MessageBox.Show("The vanilla.win in this folder is either outdated or modded. If it is modded, please replace the vanilla.win with an unmodded data.win file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return;
        }
        private void GetLocalization() {
            localizationPath = Path.Combine(gamePath, "ext");
            vanilla_localizationPath = Path.Combine(currentPath, "localization", "vanilla", "ext");

            // ext files in game path are assumed to be vanilla for now, no hash checking
            if (!Directory.Exists(vanilla_localizationPath)) {
                modInstaller.CopyDirectory(localizationPath, vanilla_localizationPath);
            }
            return;
        }
        private bool IsValidGamePath(string path) {
            data_winPath = Path.Combine(path, "data.win");
            exePath = Path.Combine(path, "ufo50.exe");
            return File.Exists(data_winPath) && File.Exists(exePath);
        }
        
        private void ApplyTheme()
        {
            bool isDarkMode = SettingsService.Settings.DarkModeEnabled;
            Color formBgColor, controlBgColor, textColor, borderColor;

            if (isDarkMode)
            {
                formBgColor = Color.FromArgb(45, 45, 48);
                controlBgColor = Color.FromArgb(63, 63, 70);
                textColor = Color.FromArgb(241, 241, 241);
                borderColor = Color.FromArgb(85, 85, 85);
            }
            else
            {
                formBgColor = SystemColors.Control;
                controlBgColor = SystemColors.Window;
                textColor = SystemColors.ControlText;
                borderColor = SystemColors.ControlDark;
            }

            this.BackColor = formBgColor;
            
            // Apply theme to all buttons, including the Settings button
            var buttons = new[] { buttonInstall, buttonDownload, buttonLaunch, buttonSettings };
            foreach (var btn in buttons)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = controlBgColor;
                btn.ForeColor = textColor;
                btn.FlatAppearance.BorderColor = borderColor;
            }

            textBox1.BackColor = controlBgColor;
            textBox1.ForeColor = textColor;
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            
            dataGridView1.BackgroundColor = controlBgColor;
            dataGridView1.GridColor = borderColor;
            dataGridView1.DefaultCellStyle.BackColor = formBgColor;
            dataGridView1.DefaultCellStyle.ForeColor = textColor;
            dataGridView1.DefaultCellStyle.SelectionBackColor = isDarkMode ? Color.FromArgb(85, 85, 95) : SystemColors.Highlight;
            dataGridView1.DefaultCellStyle.SelectionForeColor = isDarkMode ? textColor : SystemColors.HighlightText;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = controlBgColor;
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = textColor;
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = controlBgColor;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.RowHeadersDefaultCellStyle.BackColor = controlBgColor;
            dataGridView1.RowHeadersDefaultCellStyle.SelectionBackColor = controlBgColor;
            dataGridView1.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            this.Refresh();
        }
        
        private void InitializeUI() {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("UFO_50_MOD_INSTALLER.wrench.ico");
            var icon = new Icon(stream);
            defaultIcon = icon.ToBitmap();

            modsPath = Path.Combine(currentPath, "my mods");
            if (!Directory.Exists(modsPath)) Directory.CreateDirectory(modsPath);
            InitializeDataGridView();
            ApplyTheme(); 
        }
        
        private void LaunchGame()
        {
            try
            {
                // The Steam AppID for UFO 50 is 1147860
                // Using "steam://run/" is the official way to launch games via Steam protocol.
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "steam://run/1147860",
                    UseShellExecute = true // This is required to handle URL-like protocols
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch game via Steam: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void InitializeFileSystemWatcher() {
            fileSystemWatcher1.Path = modsPath;
            fileSystemWatcher1.IncludeSubdirectories = true;
            fileSystemWatcher1.EnableRaisingEvents = true;
            fileSystemWatcher1.Created += (s, e) => ReloadMods();
            fileSystemWatcher1.Deleted += (s, e) => ReloadMods();
            fileSystemWatcher1.Renamed += (s, e) => ReloadMods();
            fileSystemWatcher1.Changed += (s, e) => ReloadMods();
        }

        private void InitializeDataGridView() {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.Location = new Point(12, 74);
            dataGridView1.Size = new Size(ClientSize.Width - 24, ClientSize.Height - textBox1.Height - 24 - 74 - 20);
            dataGridView1.ReadOnly = false;
            dataGridView1.RowTemplate.Height = 80;
            dataGridView1.ClearSelection();
            dataGridView1.DefaultCellStyle.SelectionBackColor = dataGridView1.DefaultCellStyle.BackColor;
            dataGridView1.DefaultCellStyle.SelectionForeColor = dataGridView1.DefaultCellStyle.ForeColor;

            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
            checkColumn.Width = 80;
            checkColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            checkColumn.Resizable = DataGridViewTriState.False;
            checkColumn.HeaderCell = new DataGridViewCheckBoxHeaderCell();
            checkColumn.HeaderCell.Value = "";
            dataGridView1.Columns.Add(checkColumn);
            ((DataGridViewCheckBoxHeaderCell)checkColumn.HeaderCell).OnCheckBoxClicked += HeaderCheckBoxClicked;

            DataGridViewImageColumn iconColumn = new DataGridViewImageColumn();
            iconColumn.HeaderText = "";
            iconColumn.Width = 80;
            iconColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            iconColumn.Resizable = DataGridViewTriState.False;
            iconColumn.ReadOnly = true;
            dataGridView1.Columns.Add(iconColumn);

            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.HeaderText = "Mod";
            nameColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            nameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            nameColumn.ReadOnly = true;
            dataGridView1.Columns.Add(nameColumn);

            DataGridViewTextBoxColumn creatorColumn = new DataGridViewTextBoxColumn();
            creatorColumn.HeaderText = "Modder";
            creatorColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            creatorColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            creatorColumn.ReadOnly = true;
            dataGridView1.Columns.Add(creatorColumn);

            DataGridViewTextBoxColumn descColumn = new DataGridViewTextBoxColumn();
            descColumn.HeaderText = "Description";
            descColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            descColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            descColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            descColumn.ReadOnly = true;
            dataGridView1.Columns.Add(descColumn);

            dataGridView1.CellValueChanged += (s, e) => {
                if (e.ColumnIndex == 0) {
                    CheckForConflicts();
                }
            };
            dataGridView1.CurrentCellDirtyStateChanged += (s, e) => {
                if (dataGridView1.IsCurrentCellDirty) {
                    dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
        }
        private Image ResizeImage(Image image, int width, int height) {
            Bitmap resized = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resized)) {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(image, new Rectangle(0, 0, width, height));
            }
            return resized;
        }

        private void LoadMods() {
            dataGridView1.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                var modStates = new Dictionary<string, bool>();
                foreach (DataGridViewRow row in dataGridView1.Rows) {
                    string modName = row.Cells[2].Value.ToString();
                    bool isEnabled = (bool)row.Cells[0].Value;
                    modStates[modName] = isEnabled;
                }
                dataGridView1.Rows.Clear();

                var modFolders = Directory.GetDirectories(modsPath);

                foreach (string modFolder in modFolders)
                {
                    string? modPath = FindMod(modFolder);
                    if (modPath == null) continue;

                    string? folderName = Path.GetFileName(modFolder);
                    string? iconPath = Directory.GetFiles(modFolder, "*.png").FirstOrDefault();
                    Image? modIcon = defaultIcon;
                    if (!string.IsNullOrEmpty(iconPath))
                    {
                        try
                        {
                            using (FileStream stream = new FileStream(iconPath, FileMode.Open, FileAccess.Read))
                            using (Image img = Image.FromStream(stream))
                            {
                                modIcon = ResizeImage(new Bitmap(img), 80, 80);
                            }
                        }
                        catch { modIcon = defaultIcon; }
                    }

                    string? txtPath = Directory.GetFiles(modFolder, "*.txt").FirstOrDefault();
                    string creator = "", desc = "";
                    if (!string.IsNullOrEmpty(txtPath))
                    {
                        try
                        {
                            var lines = File.ReadLines(txtPath).Take(2).ToArray();
                            if (lines.Length > 0) creator = lines[0];
                            if (lines.Length > 1) desc = lines[1];
                        }
                        catch { }
                    }

                    bool isEnabled;
                    if (SettingsService.Settings.IsFirstRun)
                    {
                        // If it's the first time running the app, enable all mods by default.
                        isEnabled = true;
                    }
                    else
                    {
                        // On subsequent runs, load the user's saved preferences.
                        isEnabled = SettingsService.Settings.EnabledMods.Contains(folderName);
                    }
                    dataGridView1.Rows.Add(isEnabled, modIcon, folderName, creator, desc);
                }
                dataGridView1.ClearSelection();
            });
        }

        private void CleanupMods() {
            fileSystemWatcher1.EnableRaisingEvents = false;

            // Unzip zipped mods
            var modZips = Directory.GetFiles(modsPath, "*.zip");
            foreach (string zipPath in modZips) {
                string extractPath = Path.Combine(modsPath, Path.GetFileNameWithoutExtension(zipPath));
                if (Directory.Exists(extractPath))
                {
                    continue;
                }
                
                try
                {
                    using (ZipArchive archive = ZipFile.OpenRead(zipPath)) {
                        foreach (var entry in archive.Entries) {
                            string destinationPath = Path.Combine(modsPath, entry.FullName);
                            if (string.IsNullOrEmpty(entry.Name)) {
                                Directory.CreateDirectory(destinationPath);
                            }
                            else {
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                                entry.ExtractToFile(destinationPath, overwrite: true);
                            }
                        }
                    }
                    File.Delete(zipPath);
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine($"Skipping incomplete or invalid zip file: {Path.GetFileName(zipPath)}");
                }
            }

            var modFolders = Directory.GetDirectories(modsPath);
            foreach (string folder in modFolders) {
                string modFolder = folder;
                string? modPath = FindMod(modFolder);
                if (modPath == null) {
                    continue;
                }
                if (modFolder != modPath) {
                    if (Path.GetFileName(modFolder).Equals(Path.GetFileName(modPath), StringComparison.OrdinalIgnoreCase)) {
                        string renamedPath = modFolder + "_renamed";
                        modInstaller.CopyDirectory(modFolder, renamedPath);
                        Directory.Delete(modFolder, recursive: true);
                        modFolder = renamedPath;
                    }

                    string newModPath = Path.Combine(modsPath, Path.GetFileName(modPath));
                    modInstaller.CopyDirectory(modPath, newModPath);
                    Directory.Delete(modFolder, recursive: true);
                }
            }

            fileSystemWatcher1.EnableRaisingEvents = true;
        }
        private string? FindMod(string root) {
            var validModFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "code", "textures", "config", "ext", "audio" };
            foreach (var subDir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories)) {
                if (validModFolders.Contains(Path.GetFileName(subDir))) {
                    return Path.GetDirectoryName(subDir);
                }
            }
            return null;
        }
        private void ReloadMods() {
            if (DOWNLOADING_MODS)
                return;

            if (InvokeRequired) {
                Invoke(new Action(LoadMods));
            }
            else {
                CleanupMods();
                LoadMods();
            }
        }
        private List<string> GetEnabledMods() {
            var enabledMods = new List<string>();
            foreach (DataGridViewRow row in dataGridView1.Rows) {
                if ((bool)row.Cells[0].Value) {
                    string modName = row.Cells[2].Value.ToString();
                    string modPath = Path.Combine(modsPath, modName);
                    enabledMods.Add(modPath);
                }
            }
            return enabledMods;
        }
        private void CheckForConflicts() {
            enabledMods = GetEnabledMods();
            var conflictResult = conflictChecker.CheckConflicts(modsPath, enabledMods);
            conflictsExist = conflictResult.Item1;
            conflictsText = conflictResult.Item2;
            textBox1.Text = conflictsText;
        }
        private void installMods() {
            enabledMods = GetEnabledMods();
            modInstaller.installMods(currentPath, gamePath, enabledMods, conflictsExist);
        }
        private async Task downloadMods()
        {
            DOWNLOADING_MODS = true;
            buttonDownload.Enabled = false;
            buttonInstall.Enabled = false;
            buttonDownload.Text = "Downloading...";

            try
            {
                var installedModNames = new List<string>();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    installedModNames.Add(row.Cells[2].Value.ToString());
                }
                
                string localModInfoPath = Path.Combine(currentPath, "local_mod_info.json");
                
                using (var selectionForm = new DownloadSelectionForm(installedModNames, localModInfoPath))
                {
                    if (selectionForm.ShowDialog() == DialogResult.OK)
                    {
                        await selectionForm.FinalizeSelection();
                        var filesToDownload = selectionForm.FinalFilesToDownload;
                        var fileToModInfoMap = selectionForm.FileToModInfoMap;

                        if (filesToDownload.Count > 0)
                        {
                            await modDownloader.DownloadMods(modsPath, filesToDownload, localModInfoPath, fileToModInfoMap);
                            MessageBox.Show("Selected mods downloaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mod download failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonDownload.Enabled = true;
                buttonInstall.Enabled = true;
                buttonDownload.Text = "Download Mods";
                DOWNLOADING_MODS = false;
                ReloadMods();
            }
        }
        private void HeaderCheckBoxClicked(bool state) {
            dataGridView1.EndEdit();

            foreach (DataGridViewRow row in dataGridView1.Rows) {
                string modName = row.Cells[2].Value.ToString();
                if (modName == "UFO 50 Modding Framework") {
                    if (row.Cells[0].Value is bool isChecked && isChecked)
                        continue;
                }
                row.Cells[0].Value = state;
            }
            CheckForConflicts();
        }
    }
    public class DataGridViewCheckBoxHeaderCell : DataGridViewColumnHeaderCell
    {
        public delegate void CheckBoxClickedHandler(bool state);
        public event CheckBoxClickedHandler OnCheckBoxClicked;

        private bool _checked = true;
        private Point _location;
        private Size _size;

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts) {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            Point p = new Point();
            p.X = cellBounds.Location.X + (cellBounds.Width / 2) - 7;
            p.Y = cellBounds.Location.Y + (cellBounds.Height / 2) - 7;
            _location = p;
            _size = new Size(14, 14);
            CheckBoxRenderer.DrawCheckBox(graphics, _location, _checked ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
        }

        protected override void OnMouseClick(DataGridViewCellMouseEventArgs e) {
            Rectangle rect = new Rectangle(_location, _size);
            Point clickLocation = new Point(e.X, e.Y);
            if (rect.Contains(clickLocation)) {
                _checked = !_checked;
                if (OnCheckBoxClicked != null) {
                    OnCheckBoxClicked(_checked);
                }
                this.DataGridView.InvalidateCell(this);
            }
            base.OnMouseClick(e);
        }
    }
}
