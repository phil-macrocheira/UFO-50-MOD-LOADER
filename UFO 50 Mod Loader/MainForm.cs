using SharpCompress.Archives;
using SharpCompress.Common;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace UFO_50_Mod_Loader
{
    public partial class MainForm : Form
    {
        public static string version = "1.4.0";
        private string? currentPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        private string? downloadedModsJsonPath = "";
        private string? gamePath = "";
        private string? modsPath = "";
        public string? data_winPath = "";
        public string? exePath = "";
        public string? localizationPath = "";
        public string? vanilla_localizationPath = "";
        public string? vanilla_audioPath = "";
        private Image defaultIcon = null!;
        private ModInstaller modInstaller = new ModInstaller();
        private ModUninstaller modUninstaller = new ModUninstaller();
        private ModDownloader modDownloader = new ModDownloader();
        private ConflictChecker conflictChecker = new ConflictChecker();
        public bool conflictsExist = true;
        public string? conflictsText = "";
        public List<string> enabledMods = new List<string>();
        public bool DOWNLOADING_MODS = false;
        private int lastSearchRowIndex = -1;
        private string lastSearchQuery = "";

        private List<ModInfo>? _allModsCache = null;
        private class ModRowTag
        {
            public required string FolderPath { get; set; }
        }
        public MainForm() {
            InitializeComponent();
            this.Text = $"{this.Text} v{version}";
            this.Size = SettingsService.Settings.MainWindowSize;
            this.MinimumSize = new Size(700, 550);

            Load += async (s, e) => InitializeApplication();
            FormClosing += (s, e) => SaveAfterClose();
            Resize += (s, e) => ResizeControls();
            buttonInstall.Click += (s, e) => installMods();
            buttonUninstall.Click += (s, e) => uninstallMods();
            buttonDownload.Click += async (s, e) => await downloadMods();
            buttonLaunch.Click += (s, e) => LaunchGame();
            buttonSettings.Click += (s, e) => OpenSettings();
        }
        private void SaveAfterClose() {
            SettingsService.Settings.MainWindowSize = this.Size;
            SaveEnabledMods();
        }
        private void SaveEnabledMods() {
            SettingsService.Settings.EnabledMods.Clear();
            foreach (DataGridViewRow row in dataGridView1.Rows) {
                if (row.Tag is ModRowTag tag && (bool)row.Cells["columnEnabled"].Value)
                    SettingsService.Settings.EnabledMods.Add(Path.GetFileName(tag.FolderPath));
            }
            SettingsService.Save();
        }
        private void OpenSettings() {
            using (var settingsForm = new SettingsForm()) {
                bool isDarkMode = SettingsService.Settings.DarkModeEnabled;
                settingsForm.BackColor = isDarkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
                settingsForm.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;

                if (settingsForm.ShowDialog() == DialogResult.OK) {
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
            bool IsVanilla = GetVanillaWin();
            if (IsVanilla && !CheckLocalization())
                GetLocalization();
            if (IsVanilla && !CheckAudio())
                GetAudio();
            InitializeUI();
            InitializeFileSystemWatcher();
            CleanupMods();
            LoadMods();
            CheckForConflicts();
        }
        private void CheckGamePath() {
            if (!string.IsNullOrEmpty(SettingsService.Settings.GamePath) && IsValidGamePath(SettingsService.Settings.GamePath)) {
                gamePath = SettingsService.Settings.GamePath;
                return;
            }

            string[] possiblePaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "UFO 50"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "UFO 50")
            };

            foreach (string path in possiblePaths) {
                if (IsValidGamePath(path)) {
                    gamePath = path;
                    SettingsService.Settings.GamePath = gamePath;
                    SettingsService.Save();
                    return;
                }
            }

            while (true) {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog()) {
                    MessageBox.Show("UFO 50 install folder not found. Please select the game's installation folder.");
                    if (folderDialog.ShowDialog() == DialogResult.OK) {
                        if (IsValidGamePath(folderDialog.SelectedPath)) {
                            gamePath = folderDialog.SelectedPath;
                            SettingsService.Settings.GamePath = gamePath;
                            SettingsService.Save();
                            return;
                        }
                    }
                    else {
                        Application.Exit();
                        return;
                    }
                }
            }
        }
        private bool GetVanillaWin() {
            string vanillaPath = Path.Combine(currentPath, "vanilla.win");
            string iniPath = Path.Combine(currentPath, "GMLoader.ini");

            if (!modInstaller.checkVanillaWin(vanillaPath)) {
                File.Copy(data_winPath, vanillaPath);

                if (!modInstaller.checkVanillaHash(data_winPath, iniPath)) {
                    MessageBox.Show("Currently installed version of UFO 50 is either outdated or modded. If it is modded, please replace the vanilla.win in this folder with an unmodded data.win file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
            else if (!modInstaller.checkVanillaHash(vanillaPath, iniPath)) {
                MessageBox.Show("The vanilla.win in this folder is either outdated or modded. If it is modded, please replace the vanilla.win with an unmodded data.win file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
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
        private void GetAudio() {
            vanilla_audioPath = Path.Combine(currentPath, "audio", "vanilla");
            // audio files in game path are assumed to be vanilla for now, no hash checking
            if (!Directory.Exists(vanilla_audioPath)) {
                modInstaller.CopyDirectory(gamePath, vanilla_audioPath, false, ".dat");
            }
        }
        private bool CheckLocalization() {
            string test_vanilla_localizationPath = Path.Combine(currentPath, "localization", "vanilla", "ext", "ENGLISH", "0_Text.json");
            return File.Exists(test_vanilla_localizationPath);
        }
        private bool CheckAudio() {
            string test_vanilla_audioPath = Path.Combine(currentPath, "audio", "vanilla", "audiogroup1.dat");
            return File.Exists(test_vanilla_audioPath);
        }
        private bool IsValidGamePath(string path) {
            data_winPath = Path.Combine(path, "data.win");
            exePath = Path.Combine(path, "ufo50.exe");
            return File.Exists(data_winPath) && File.Exists(exePath);
        }
        private void ApplyTheme() {
            bool isDarkMode = SettingsService.Settings.DarkModeEnabled;
            Color formBgColor, controlBgColor, textColor, borderColor;

            if (isDarkMode) {
                formBgColor = Color.FromArgb(45, 45, 48);
                controlBgColor = Color.FromArgb(63, 63, 70);
                textColor = Color.FromArgb(241, 241, 241);
                borderColor = Color.FromArgb(85, 85, 85);
            }
            else {
                formBgColor = SystemColors.Control;
                controlBgColor = SystemColors.Window;
                textColor = SystemColors.ControlText;
                borderColor = SystemColors.ControlDark;
            }

            this.BackColor = formBgColor;

            // Apply theme to all buttons, including the Settings button
            var buttons = new[] { buttonInstall, buttonDownload, buttonLaunch, buttonSettings, buttonUninstall };
            foreach (var btn in buttons) {
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = controlBgColor;
                btn.ForeColor = textColor;
                btn.FlatAppearance.BorderColor = borderColor;
            }

            textBox1.BackColor = controlBgColor;
            textBox1.ForeColor = textColor;
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBoxSearch.BackColor = controlBgColor;
            textBoxSearch.ForeColor = textColor;
            labelSearch.ForeColor = textColor;
            checkBoxHide.ForeColor = textColor;

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
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView1.ColumnHeadersHeight = 50;
            dataGridView1.Location = new Point(11, 100);
            this.Refresh();
        }
        private void InitializeUI() {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("UFO_50_Mod_Loader.wrench.ico");
            var icon = new Icon(stream);
            defaultIcon = icon.ToBitmap();

            modsPath = Path.Combine(currentPath, "my mods");
            if (!Directory.Exists(modsPath)) Directory.CreateDirectory(modsPath);
            InitializeDataGridView();
            ApplyTheme();
        }
        private void LaunchGame() {
            try {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "steam://run/1147860",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex) {
                MessageBox.Show($"Failed to launch game via Steam: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void InitializeFileSystemWatcher() {
            fileSystemWatcher1.Path = modsPath;
            fileSystemWatcher1.IncludeSubdirectories = true;
            fileSystemWatcher1.EnableRaisingEvents = true;
            fileSystemWatcher1.Created += async (s, e) => ReloadMods();
            fileSystemWatcher1.Deleted += async (s, e) => ReloadMods();
            fileSystemWatcher1.Renamed += async (s, e) => ReloadMods();
            fileSystemWatcher1.Changed += async (s, e) => ReloadMods();
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
            dataGridView1.SelectionChanged += (s, e) => dataGridView1.ClearSelection();

            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
            checkColumn.Name = "columnEnabled";
            //checkColumn.SortMode = DataGridViewColumnSortMode.Automatic; // This breaks saving unfortunately
            checkColumn.Width = 80;
            checkColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            checkColumn.Resizable = DataGridViewTriState.False;
            checkColumn.HeaderCell = new DataGridViewCheckBoxHeaderCell();
            checkColumn.HeaderCell.Value = "";
            dataGridView1.Columns.Add(checkColumn);
            // dataGridView1.Sort(checkColumn, ListSortDirection.Descending); // This breaks saving unfortunately
            // dataGridView1.RowsAdded += (s, e) => { dataGridView1.Sort(checkColumn, ListSortDirection.Descending); }; // This breaks saving unfortunately
            ((DataGridViewCheckBoxHeaderCell)checkColumn.HeaderCell).OnCheckBoxClicked += HeaderCheckBoxClicked;

            DataGridViewImageColumn iconColumn = new DataGridViewImageColumn();
            iconColumn.HeaderText = "";
            iconColumn.Width = 80;
            iconColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            iconColumn.Resizable = DataGridViewTriState.False;
            iconColumn.ReadOnly = true;
            dataGridView1.Columns.Add(iconColumn);

            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.Name = "columnModName";
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

                foreach (string modFolder in modFolders) {
                    string? modPath = FindMod(modFolder);
                    if (modPath == null) continue;

                    string? folderName = Path.GetFileName(modFolder);
                    string? iconPath = Directory.GetFiles(modFolder, "*.png").FirstOrDefault();
                    Image? modIcon = defaultIcon;
                    if (!string.IsNullOrEmpty(iconPath)) {
                        try {
                            using (FileStream stream = new FileStream(iconPath, FileMode.Open, FileAccess.Read))
                            using (Image img = Image.FromStream(stream)) {
                                modIcon = ResizeImage(new Bitmap(img), 80, 80);
                            }
                        }
                        catch { modIcon = defaultIcon; }
                    }

                    string? txtPath = Directory.GetFiles(modFolder, "*.txt").FirstOrDefault();
                    string creator = "", desc = "";
                    if (!string.IsNullOrEmpty(txtPath)) {
                        try {
                            var lines = File.ReadLines(txtPath).Take(2).ToArray();
                            if (lines.Length > 0) creator = lines[0];
                            if (lines.Length > 1) desc = lines[1];
                        }
                        catch { }
                    }

                    bool isEnabled = SettingsService.Settings.EnabledMods.Contains(folderName);
                    int rowIndex = dataGridView1.Rows.Add(isEnabled, modIcon, folderName, creator, desc);
                    dataGridView1.Rows[rowIndex].Tag = new ModRowTag
                    {
                        FolderPath = Path.Combine(modsPath, folderName),
                    };
                }
                dataGridView1.ClearSelection();
            });
        }
        private void CleanupMods() {
            fileSystemWatcher1.EnableRaisingEvents = false;
            if (string.IsNullOrEmpty(modsPath)) return;

            var modZips = Directory.GetFiles(modsPath, "*.*")
                .Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".7z", StringComparison.OrdinalIgnoreCase));
            foreach (string zipPath in modZips) {
                string extractPath = Path.Combine(modsPath, Path.GetFileNameWithoutExtension(zipPath));
                if (Directory.Exists(extractPath))
                    continue;

                try {
                    if (zipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
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
                    }
                    else {
                        using (var archive = ArchiveFactory.Open(zipPath)) {
                            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory)) {
                                string destinationPath = Path.Combine(modsPath, entry.Key);
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                                entry.WriteToFile(destinationPath, new ExtractionOptions() { Overwrite = true });
                            }
                        }
                    }
                    File.Delete(zipPath);
                }
                catch (InvalidDataException) {
                    Console.WriteLine($"Skipping incomplete or invalid zip file: {Path.GetFileName(zipPath)}");
                }
                catch (IOException ex) {
                    Console.WriteLine($"Cannot open zip file: {Path.GetFileName(zipPath)}");
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
                if (checkBoxHide.Checked)
                    ToggleHideDisabledMods(true);
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
            SaveEnabledMods();
            modInstaller.installMods(currentPath, gamePath, enabledMods, conflictsExist);
        }
        private void uninstallMods() {
            modUninstaller.uninstallMods(currentPath, gamePath);
        }
        private async Task downloadMods() {
            DOWNLOADING_MODS = true;
            buttonDownload.Enabled = false;
            buttonInstall.Enabled = false;
            buttonDownload.Text = "Downloading...";

            try {
                using (var selectionForm = new DownloadSelectionForm()) {
                    if (selectionForm.ShowDialog() == DialogResult.OK) {
                        await selectionForm.FinalizeSelection();
                        var filesToDownload = selectionForm.FinalFilesToDownload;

                        if (filesToDownload.Count > 0) {
                            await modDownloader.DownloadMods(modsPath, filesToDownload);
                            MessageBox.Show("Selected mods downloaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                        // Save downloaded_mods.json
                        downloadedModsJsonPath = Path.Combine(currentPath, "downloaded_mods.json");

                        var previouslyDownloadedMods = File.Exists(downloadedModsJsonPath)
                            ? JsonSerializer.Deserialize<List<Dictionary<string, object>>>(File.ReadAllText(downloadedModsJsonPath)) ?? new List<Dictionary<string, object>>()
                            : new List<Dictionary<string, object>>();

                        var newDownloadedMods = filesToDownload
                            .Select(f => selectionForm._allMods.FirstOrDefault(m => m.Id == f.Id))
                            .Where(m => m != null)
                            .Select(m => new Dictionary<string, object>
                            {
                                ["Id"] = m!.Id,
                                ["Name"] = m.Name,
                                ["Creator"] = m.Creator,
                                ["DateUpdated"] = m.DateUpdated,
                                ["DateAdded"] = m.DateAdded,
                                ["Version"] = m.Version
                            })
                            .ToList();

                        var mergedMods = previouslyDownloadedMods
                            .Concat(newDownloadedMods)
                            .GroupBy(m => m["Id"])
                            .Select(g => g.First())
                            .ToList();
                        File.WriteAllText(downloadedModsJsonPath, JsonSerializer.Serialize(mergedMods, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"Mod download failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally {
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
                if (modName == "UFO 50 Modding Settings") {
                    if (row.Cells[0].Value is bool isChecked && isChecked)
                        continue;
                }
                row.Cells[0].Value = state;
            }
            CheckForConflicts();
        }

        private void textBoxSearch_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                string query = textBoxSearch.Text.Trim().ToLower();
                if (string.IsNullOrEmpty(query)) return;
                if (query != lastSearchQuery) {
                    lastSearchQuery = query;
                    lastSearchRowIndex = -1;
                }

                int startRow = lastSearchRowIndex + 1;
                bool found = false;

                for (int i = startRow; i < dataGridView1.Rows.Count; i++) {
                    if (dataGridView1.Rows[i].IsNewRow) continue;
                    if (!dataGridView1.Rows[i].Visible) continue;

                    string modName = dataGridView1.Rows[i].Cells["columnModName"].Value?.ToString().ToLower() ?? "";
                    if (modName.Contains(query)) {
                        dataGridView1.ClearSelection();
                        dataGridView1.Rows[i].Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = i;

                        lastSearchRowIndex = i;
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    lastSearchRowIndex = -1;
                }
            }
        }
        private void checkBoxHide_CheckedChanged(object sender, EventArgs e) {
            ToggleHideDisabledMods(checkBoxHide.Checked);
        }
        private void ToggleHideDisabledMods(bool hideDisabled) {
            foreach (DataGridViewRow row in dataGridView1.Rows) {
                if (row.IsNewRow) continue;

                bool isEnabled = false;
                if (row.Cells["columnEnabled"].Value is bool b)
                    isEnabled = b;

                row.Visible = !hideDisabled || isEnabled;
            }
        }
    }
    public class DataGridViewCheckBoxHeaderCell : DataGridViewColumnHeaderCell
    {
        public delegate void CheckBoxClickedHandler(bool state);
        public event CheckBoxClickedHandler OnCheckBoxClicked;

        private bool _checked = false;
        private Point _location;
        private Size _size;

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts) {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            Point p = new Point();
            p.X = cellBounds.Location.X + (cellBounds.Width / 2) - 10;
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