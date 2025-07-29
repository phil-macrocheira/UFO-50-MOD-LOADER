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
        
        private List<ModInfo>? _allModsCache = null;

        private class ModRowTag
        {
            public required string FolderPath { get; set; }
            public InstallerMetadata? Metadata { get; set; }
        }

        public MainForm()
        {
            InitializeComponent();
            this.Size = SettingsService.Settings.MainWindowSize;
            
            Load += async (s, e) => await InitializeApplication();
            FormClosing += (s, e) => SaveModStates();
            Resize += (s, e) => ResizeControls();
            buttonInstall.Click += (s, e) => installMods();
            buttonDownload.Click += async (s, e) => await downloadMods();
            buttonLaunch.Click += (s, e) => LaunchGame();
            buttonSettings.Click += (s, e) => OpenSettings();
        }
        private void SaveModStates()
        {
            SettingsService.Settings.MainWindowSize = this.Size;
            SettingsService.Settings.EnabledMods.Clear();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Tag is ModRowTag tag && (bool)row.Cells["Enabled"].Value)
                {
                    SettingsService.Settings.EnabledMods.Add(Path.GetFileName(tag.FolderPath));
                }
            }
            SettingsService.Save();
        }

        private void OpenSettings()
        {
            using (var settingsForm = new SettingsForm())
            {
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
        private async Task InitializeApplication() {
            CheckGamePath();
            GetVanillaWin();
            InitializeUI();
            InitializeFileSystemWatcher();
            CleanupMods();
            await LoadAndLinkMods();
            CheckForConflicts();
        }

        private void CheckGamePath()
        {
            if (!string.IsNullOrEmpty(SettingsService.Settings.GamePath) && IsValidGamePath(SettingsService.Settings.GamePath))
            {
                gamePath = SettingsService.Settings.GamePath;
                return;
            }

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
                    SettingsService.Settings.GamePath = gamePath;
                    SettingsService.Save();
                    return;
                }
            }

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
                            SettingsService.Settings.GamePath = gamePath;
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
            var buttons = new[] { buttonInstall, buttonDownload, buttonLaunch, buttonSettings };
            foreach (var btn in buttons) {
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
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "steam://run/1147860",
                    UseShellExecute = true
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
            fileSystemWatcher1.Created += async (s, e) => await ReloadMods();
            fileSystemWatcher1.Deleted += async (s, e) => await ReloadMods();
            fileSystemWatcher1.Renamed += async (s, e) => await ReloadMods();
            fileSystemWatcher1.Changed += async (s, e) => await ReloadMods();
        }

        private void InitializeDataGridView() {
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
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            var checkColumn = new DataGridViewCheckBoxColumn { Width = 60, Resizable = DataGridViewTriState.False, Name = "Enabled", HeaderCell = new DataGridViewCheckBoxHeaderCell { Value = "" } };
            ((DataGridViewCheckBoxHeaderCell)checkColumn.HeaderCell).OnCheckBoxClicked += HeaderCheckBoxClicked;
            dataGridView1.Columns.Add(checkColumn);

            var iconColumn = new DataGridViewImageColumn { HeaderText = "", Width = 80, ImageLayout = DataGridViewImageCellLayout.Zoom, Resizable = DataGridViewTriState.False, ReadOnly = true, Name = "Icon" };
            dataGridView1.Columns.Add(iconColumn);

            var titleColumn = new DataGridViewTextBoxColumn { HeaderText = "Mod", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = true, Name = "Title" };
            dataGridView1.Columns.Add(titleColumn);
            
            var creatorColumn = new DataGridViewTextBoxColumn { HeaderText = "Creator", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = true, Name = "Creator" };
            dataGridView1.Columns.Add(creatorColumn);
            
            var statusColumn = new DataGridViewTextBoxColumn { HeaderText = "Status", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = true, Name = "Status" };
            dataGridView1.Columns.Add(statusColumn);
            
            var descCellStyle = new DataGridViewCellStyle {
                WrapMode = DataGridViewTriState.True,
                Padding = new Padding(5, 10, 5, 10) // Adds padding around the text
            };
            
            var descColumn = new DataGridViewTextBoxColumn { HeaderText = "Description", DefaultCellStyle = descCellStyle, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true, Name = "Description" };
            dataGridView1.Columns.Add(descColumn);
            
            dataGridView1.CellValueChanged += (s, e) => { if (e.ColumnIndex == 0) CheckForConflicts(); };
            dataGridView1.CurrentCellDirtyStateChanged += (s, e) => { if (dataGridView1.IsCurrentCellDirty) dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit); };
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

        private async Task LoadAndLinkMods() {
            
            if (modsPath == null) return;
        
            dataGridView1.Rows.Clear();
            var modFolders = Directory.GetDirectories(modsPath);
        
            if (_allModsCache == null)
            {
                try { _allModsCache = await ModDownloader.GetModInfo("23000"); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not fetch mod list from GameBanana. Update checks will be disabled. Error: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _allModsCache = new List<ModInfo>();
                }
            }
        
            foreach (string modFolder in modFolders)
            {
                string folderName = Path.GetFileName(modFolder);
                var metadata = InstallerMetadata.Load(modFolder) ?? new InstallerMetadata();
                bool needsSave = false;
                
                string? titleFromTxt = null, creatorFromTxt = null, descriptionFromTxt = null;
                var txtPath = Directory.GetFiles(modFolder, "*.txt", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (txtPath != null)
                {
                    var lines = File.ReadAllLines(txtPath);
                    if (lines.Length >= 3) {
                        creatorFromTxt = lines[0]; descriptionFromTxt = lines[1]; titleFromTxt = lines[2];
                    } else if (lines.Length >= 2) {
                        creatorFromTxt = lines[0]; descriptionFromTxt = lines[1];
                    }
                }
                
                string title = titleFromTxt ?? metadata.Title ?? folderName;
                string creator = metadata.Creator ?? creatorFromTxt ?? "Unknown";
                string description = metadata.Description ?? descriptionFromTxt ?? "No description found.";

                if (metadata.Title != title) { metadata.Title = title; needsSave = true; }
                if (metadata.Creator != creator) { metadata.Creator = creator; needsSave = true; }
                if (metadata.Description != description) { metadata.Description = description; needsSave = true; }
                
                string status = "";

                if (folderName.Equals("UFO 50 Modding Framework", StringComparison.OrdinalIgnoreCase))
                {
                    status = "Framework";
                }
                else if (metadata.GameBananaId == 0)
                {
                    status = "Linking...";
                    var bestMatch = ModDownloader.FindBestModMatch(_allModsCache, title, creator);
                    
                    if (bestMatch == null && creator != "Unknown")
                    {
                        var modsByAuthor = _allModsCache.Where(m => m.Creator.Equals(creator, StringComparison.OrdinalIgnoreCase)).ToList();
                        bestMatch = ModDownloader.FindBestModMatch(modsByAuthor, title, creator);
                    }

                    if (bestMatch != null && long.TryParse(bestMatch.Id, out long gameBananaId))
                    {
                        metadata.GameBananaId = gameBananaId;
                        metadata.LatestVersionDate = bestMatch.DateUpdated;
                        metadata.Version = bestMatch.Version; // Save version on link
                        status = "Installed"; 
                        needsSave = true;
                    } else { status = "Unlinked"; }
                }
                else
                {
                    status = "Installed";
                    var remoteMod = _allModsCache.FirstOrDefault(m => m.Id == metadata.GameBananaId.ToString());
                    if (remoteMod != null)
                    {
                        // Update check using both date and version string
                        if (remoteMod.DateUpdated > metadata.LatestVersionDate || remoteMod.Version != metadata.Version)
                        {
                            status = "Update Available";
                        }
                    }
                }
                
                if (needsSave) metadata.Save(modFolder);

                Image modIcon = defaultIcon;
                string? iconPath = Directory.GetFiles(modFolder, "*.png", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (!string.IsNullOrEmpty(iconPath))
                {
                    try { using (var bmpTemp = new Bitmap(iconPath)) { modIcon = new Bitmap(bmpTemp); } }
                    catch { /* Use default icon on failure */ }
                }
                
                bool isEnabled = SettingsService.Settings.EnabledMods.Contains(folderName);
                
                var rowIndex = dataGridView1.Rows.Add(isEnabled, modIcon, title, creator, status, description);
                dataGridView1.Rows[rowIndex].Tag = new ModRowTag { FolderPath = modFolder, Metadata = metadata };
            }
        }


        private void CleanupMods() {
            fileSystemWatcher1.EnableRaisingEvents = false;
            if (string.IsNullOrEmpty(modsPath)) return;

            var modZips = Directory.GetFiles(modsPath, "*.zip");
            foreach (string zipPath in modZips) {
                string extractPath = Path.Combine(modsPath, Path.GetFileNameWithoutExtension(zipPath));
                if (Directory.Exists(extractPath))
                {
                    continue;
                }
                
                try
                {
                    ZipFile.ExtractToDirectory(zipPath, extractPath, true);
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
            var rootFiles = Directory.GetFiles(root).Where(f => !f.ToLower().EndsWith("info.txt") && !f.ToLower().EndsWith(".png") && !f.ToLower().EndsWith(".json"));
            if(rootFiles.Any()) return root;
            
            return null;
        }
        private async Task ReloadMods() {
            if (DOWNLOADING_MODS)
                return;

            if (InvokeRequired) {
                await Invoke(new Func<Task>(async () => {
                    CleanupMods();
                    await LoadAndLinkMods();
                }));
            }
            else {
                CleanupMods();
                await LoadAndLinkMods();
            }
        }
        private List<string> GetEnabledMods() {
            var enabledModPaths = new List<string>();
            foreach (DataGridViewRow row in dataGridView1.Rows) {
                if (row.Tag is ModRowTag tag && (bool)row.Cells["Enabled"].Value) {
                    enabledModPaths.Add(tag.FolderPath);
                }
            }
            return enabledModPaths;
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
                var installedModVersions = new Dictionary<string, (string version, long date)>();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                   if (row.Tag is ModRowTag { Metadata: not null } tag && tag.Metadata.GameBananaId > 0)
                   {
                       installedModVersions[tag.Metadata.GameBananaId.ToString()] = (tag.Metadata.Version ?? "N/A", tag.Metadata.LatestVersionDate);
                   }
                }
                
                using (var selectionForm = new DownloadSelectionForm(installedModVersions))
                {
                    if (selectionForm.ShowDialog() == DialogResult.OK)
                    {
                        await selectionForm.FinalizeSelection();
                        var filesToDownload = selectionForm.FinalFilesToDownload;
                        var fileToModInfoMap = selectionForm.FileToModInfoMap;

                        if (filesToDownload.Count > 0)
                        {
                            await modDownloader.DownloadMods(modsPath, filesToDownload, null, fileToModInfoMap);
                            foreach (var modInfo in fileToModInfoMap.Values)
                            {
                                // Find the corresponding row in our main grid to get the folder path
                                foreach (DataGridViewRow row in dataGridView1.Rows)
                                {
                                    if (row.Tag is ModRowTag tag && tag.Metadata?.GameBananaId.ToString() == modInfo.Id)
                                    {
                                        // Load the existing metadata, update it, and save.
                                        var metadata = InstallerMetadata.Load(tag.FolderPath);
                                        if (metadata != null)
                                        {
                                            metadata.LatestVersionDate = modInfo.DateUpdated;
                                            metadata.Version = modInfo.Version;
                                            metadata.Save(tag.FolderPath);
                                        }
                                        break; // Move to the next downloaded mod
                                    }
                                }
                            }
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
                await ReloadMods();
            }
        }
        private void HeaderCheckBoxClicked(bool state) {
            dataGridView1.EndEdit();

            foreach (DataGridViewRow row in dataGridView1.Rows) {
                if (row.Cells["Title"].Value.ToString() == "UFO 50 Modding Framework") {
                    if (row.Cells["Enabled"].Value is bool isChecked && isChecked)
                        continue;
                }
                row.Cells["Enabled"].Value = state;
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