using System.Drawing.Drawing2D;
using System.Reflection;

namespace UFO_50_MOD_INSTALLER
{
    public partial class MainForm : Form
    {
        private string? currentPath = "";
        private string? gamePath = "";
        private string? modsPath = "";
        public string? data_winPath = "";
        public string? exePath = "";
        private Image defaultIcon = null!;
        private ModInstaller modInstaller = new ModInstaller();
        private ConflictChecker conflictChecker = new ConflictChecker();
        public bool conflictsExist = true;
        public string? conflictsText = "";

        public MainForm() {
            InitializeComponent();
            Load += (s, e) => InitializeApplication();
            Resize += (s, e) => ResizeControls();
            buttonInstall.Click += (s, e) => installMods();
        }
        private void ResizeControls() {
            dataGridView1.Size = new Size(ClientSize.Width - 24, ClientSize.Height - textBox1.Height - 24 - 74);
            textBox1.Location = new Point(12, ClientSize.Height - textBox1.Height - 12);
        }

        private void InitializeApplication() {
            CheckGamePath();
            GetVanillaWin();
            InitializeUI();
            InitializeFileSystemWatcher();
            LoadMods();
            CheckForConflicts();
        }

        private void CheckGamePath() {
            string[] possiblePaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "UFO 50"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "UFO 50")
            };

            foreach (string path in possiblePaths) {
                if (IsValidGamePath(path)) {
                    gamePath = path;
                    return;
                }
            }

            while (true) {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog()) {
                    MessageBox.Show("UFO 50 install folder not found. Please enter path to game install folder.");
                    DialogResult result = folderDialog.ShowDialog();
                    if (result == DialogResult.OK) {
                        string selectedPath = folderDialog.SelectedPath;
                        if (IsValidGamePath(selectedPath)) {
                            gamePath = selectedPath;
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
        private void GetVanillaWin() {
            string vanillaPath = Path.Combine(currentPath, "vanilla.win");

            if (!modInstaller.checkVanillaWin(vanillaPath)) {
                File.Copy(data_winPath, vanillaPath);

                if (!modInstaller.checkVanillaHash(data_winPath)) {
                    MessageBox.Show("Currently installed version of UFO 50 is either outdated or modded. If it is modded, please replace the vanilla.win in this folder with an unmodded data.win file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (!modInstaller.checkVanillaHash(vanillaPath)) {
                MessageBox.Show("The vanilla.win in this folder is either outdated or modded. If it is modded, please replace the vanilla.win with an unmodded data.win file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return;
        }

        private bool IsValidGamePath(string path) {
            data_winPath = Path.Combine(path, "data.win");
            exePath = Path.Combine(path, "ufo50.exe");
            return File.Exists(data_winPath) && File.Exists(exePath);
        }

        private void InitializeUI() {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("UFO_50_MOD_INSTALLER.wrench.ico");
            var icon = new Icon(stream);
            defaultIcon = icon.ToBitmap();

            currentPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            modsPath = Path.Combine(currentPath, "my mods");
            if (!Directory.Exists(modsPath)) Directory.CreateDirectory(modsPath);
            InitializeDataGridView();
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
            dataGridView1.Size = new Size(ClientSize.Width - 24, ClientSize.Height - 74 - 12);
            dataGridView1.ReadOnly = false;
            dataGridView1.RowTemplate.Height = 80;
            dataGridView1.ClearSelection();
            dataGridView1.DefaultCellStyle.SelectionBackColor = dataGridView1.DefaultCellStyle.BackColor;
            dataGridView1.DefaultCellStyle.SelectionForeColor = dataGridView1.DefaultCellStyle.ForeColor;

            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
            checkColumn.HeaderText = "Enable";
            checkColumn.Width = 80;
            checkColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            checkColumn.Resizable = DataGridViewTriState.False;
            dataGridView1.Columns.Add(checkColumn);

            DataGridViewImageColumn iconColumn = new DataGridViewImageColumn();
            iconColumn.HeaderText = "";
            iconColumn.Width = 80;
            iconColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            iconColumn.Resizable = DataGridViewTriState.False;
            iconColumn.ReadOnly = true;
            dataGridView1.Columns.Add(iconColumn);

            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.HeaderText = "Mod";
            nameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            nameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            nameColumn.ReadOnly = true;
            dataGridView1.Columns.Add(nameColumn);
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
                dataGridView1.Rows.Clear();
                var modFolders = Directory.GetDirectories(modsPath);
                foreach (var modFolder in modFolders) {
                    string? folderName = Path.GetFileName(modFolder);
                    string? iconPath = Directory.GetFiles(modFolder, "*.png").FirstOrDefault();
                    Image? modIcon = defaultIcon;
                    if (!string.IsNullOrEmpty(iconPath)) {
                        try {
                            modIcon = Image.FromFile(iconPath);
                            modIcon = ResizeImage(modIcon, 80, 80);
                        }
                        catch { modIcon = defaultIcon; }
                    }
                    dataGridView1.Rows.Add(true, modIcon, folderName);
                }
                dataGridView1.ClearSelection();
            });
        }

        private void ReloadMods() {
            if (InvokeRequired) {
                Invoke(new Action(LoadMods));
            }
            else {
                LoadMods();
            }
        }
        private List<string> GetEnabledMods() {
            var mods = Directory.GetDirectories(modsPath).ToList();
            var enabledMods = new List<string>();
            for (int i = 0; i < mods.Count; i++) {
                if ((bool)dataGridView1.Rows[i].Cells[0].Value)
                    enabledMods.Add(mods[i]);
            }
            return enabledMods;
        }
        private void CheckForConflicts() {
            var conflictResult = conflictChecker.CheckConflicts(modsPath);
            conflictsExist = conflictResult.Item1;
            conflictsText = conflictResult.Item2;
            textBox1.AppendText(conflictsText);
        }
        private void installMods() {
            List<string> enabledMods = GetEnabledMods();
            modInstaller.installMods(data_winPath, enabledMods, conflictsExist, currentPath);
        }
    }
}
