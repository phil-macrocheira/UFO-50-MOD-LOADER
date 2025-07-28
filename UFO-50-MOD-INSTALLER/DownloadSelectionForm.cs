using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text.Json;

namespace UFO_50_MOD_INSTALLER
{
    public partial class DownloadSelectionForm : Form
    {
        // Controls
        private DataGridView dataGridViewMods;
        private Button buttonDownloadSelected;
        private Button buttonCancel;
        private PictureBox pictureBoxPreview;
        private Label labelModTitle;
        private Label labelModder;
        private TextBox textBoxDescription;
        private SplitContainer splitContainer;
        private Label labelMetadata;
        
        // Properties and Data
        public List<ModFile> FinalFilesToDownload { get; private set; } = new List<ModFile>();
        public Dictionary<string, ModInfo> FileToModInfoMap { get; private set; } = new Dictionary<string, ModInfo>();
        private List<string> _installedModNames;
        private List<ModInfo> _allMods = new List<ModInfo>();
        private Dictionary<string, LocalModInfo> _localMods = new Dictionary<string, LocalModInfo>();
        private static HttpClient _httpClient = new HttpClient();

        public DownloadSelectionForm(List<string> installedModNames, string localModInfoPath)
        {
            _installedModNames = installedModNames;
            if (File.Exists(localModInfoPath))
            {
                var json = File.ReadAllText(localModInfoPath);
                _localMods = JsonSerializer.Deserialize<Dictionary<string, LocalModInfo>>(json) ?? new Dictionary<string, LocalModInfo>();
            }
            InitializeComponent();
            this.Load += async (s, e) => await LoadModsList();
        }

        private void InitializeComponent()
        {
            this.Text = "Select Mods to Download";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            splitContainer = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 620, IsSplitterFixed = true, BackColor = Color.FromArgb(85, 85, 85) };
            
            dataGridViewMods = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowTemplate = { Height = 28 }, AllowUserToResizeRows = false };
            splitContainer.Panel1.Controls.Add(dataGridViewMods);
            dataGridViewMods.SelectionChanged += DataGridViewMods_SelectionChanged;
            dataGridViewMods.CellContentClick += DataGridViewMods_CellContentClick;
            SetupDataGridViewStyles();

            var checkColumn = new DataGridViewCheckBoxColumn { HeaderText = "Select", Width = 50, Name = "select" };
            var nameColumn = new DataGridViewLinkColumn { HeaderText = "Mod", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, Name = "name", ReadOnly = true, TrackVisitedState = false, LinkColor = Color.Cyan, ActiveLinkColor = Color.LightCyan };
            var creatorColumn = new DataGridViewTextBoxColumn { HeaderText = "Modder", Width = 120, Name = "creator", ReadOnly = true };
            var statusColumn = new DataGridViewTextBoxColumn { HeaderText = "Status", Width = 110, Name = "status", ReadOnly = true };
            dataGridViewMods.Columns.AddRange(new DataGridViewColumn[] { checkColumn, nameColumn, creatorColumn, statusColumn });
            
            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pictureBoxPreview = new PictureBox { Dock = DockStyle.Top, Height = 220, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(30,30,30) };
            labelModTitle = new Label { Dock = DockStyle.Top, Padding = new Padding(0, 8, 0, 0), Font = new Font("Segoe UI", 12F, FontStyle.Bold), Height = 40, AutoSize = false, TextAlign = ContentAlignment.BottomLeft };
            labelModder = new Label { Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 8), Font = new Font("Segoe UI", 9F, FontStyle.Italic), ForeColor = Color.Gray, Height = 30 };
            textBoxDescription = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                Multiline = true, 
                ReadOnly = true, 
                BackColor = Color.FromArgb(55, 55, 58),
                ForeColor = Color.White, 
                BorderStyle = BorderStyle.FixedSingle, 
                Font = new Font("Segoe UI", 9F), 
                Padding = new Padding(8), 
                ScrollBars = ScrollBars.Vertical
            };
            
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(0, 10, 0, 0) };
            labelMetadata = new Label { Dock = DockStyle.Fill, ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleLeft };
            buttonDownloadSelected = new Button { Text = "Download Selected", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 140, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            buttonDownloadSelected.FlatAppearance.BorderSize = 0;
            buttonCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 90, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(85, 85, 85), ForeColor = Color.White };
            buttonCancel.FlatAppearance.BorderSize = 0;
            
            buttonPanel.Controls.Add(labelMetadata);
            buttonPanel.Controls.Add(buttonDownloadSelected);
            buttonPanel.Controls.Add(buttonCancel);
            
            rightPanel.Controls.Add(textBoxDescription);
            rightPanel.Controls.Add(labelModder);
            rightPanel.Controls.Add(labelModTitle);
            rightPanel.Controls.Add(pictureBoxPreview);
            rightPanel.Controls.Add(buttonPanel);
            splitContainer.Panel2.Controls.Add(rightPanel);

            this.Controls.Add(splitContainer);
            this.AcceptButton = buttonDownloadSelected;
            this.CancelButton = buttonCancel;
        }

        private async Task LoadModsList()
        {
            try
            {
                this.Text = "Fetching Mod List...";
                _allMods = await ModDownloader.GetModInfo("23000");

                dataGridViewMods.Rows.Clear();
                foreach (var mod in _allMods)
                {
                    var rowIndex = dataGridViewMods.Rows.Add(false, mod.Name, mod.Creator, "");
                    dataGridViewMods.Rows[rowIndex].Tag = mod;

                    string status = "";
                    bool isInstalled = _installedModNames.Contains(mod.Name, StringComparer.OrdinalIgnoreCase);

                    if (isInstalled)
                    {
                        status = "Installed";
                        var checkboxCell = (DataGridViewCheckBoxCell)dataGridViewMods.Rows[rowIndex].Cells["select"];
                        checkboxCell.Value = false;
                        checkboxCell.ReadOnly = false; // Always allow re-selection
                        dataGridViewMods.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DimGray;
                        
                        if (_localMods.TryGetValue(mod.Id, out var localInfo) && mod.DateUpdated > localInfo.DateUpdated)
                        {
                            status = "Update Available";
                            dataGridViewMods.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(40, 80, 40);
                            dataGridViewMods.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.LightGreen;
                        }
                    }
                    dataGridViewMods.Rows[rowIndex].Cells["status"].Value = status;
                }
                this.Text = "Select Mods to Download";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch mod list from Gamebanana: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }
        
        // In DownloadSelectionForm.cs

        private async void DataGridViewMods_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewMods.SelectedRows.Count > 0)
            {
                var mod = dataGridViewMods.SelectedRows[0].Tag as ModInfo;
                if (mod != null)
                {
                    labelModTitle.Text = mod.Name;
                    labelModder.Text = $"by {mod.Creator}";
                    var addedDate = FormatUnixTimestamp(mod.DateAdded);
                    labelMetadata.Text = $"Added: {addedDate}   Views: {mod.Views}   Likes: {mod.Likes}";
            
                    // Show the short summary from the main API call immediately
                    textBoxDescription.Text = $"{mod.Description}\n\nLoading full details...";
            
                    await LoadPreviewImageAsync(mod.ImageUrl);

                    // Fetch the full description asynchronously
                    string fullDescription = await ModDownloader.GetModFullDescription(mod.Id);
            
                    // Check if the user is still on the same mod before updating the text
                    if (dataGridViewMods.SelectedRows.Count > 0 && (dataGridViewMods.SelectedRows[0].Tag as ModInfo)?.Id == mod.Id)
                    {
                        textBoxDescription.Text = fullDescription;
                    }
                }
            }
        }
        
        private string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "No description available.";
        
            // Simple HTML tag removal - for more complex HTML, consider using HtmlAgilityPack
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", " ")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Trim();
        }
        
        private string FormatUnixTimestamp(long unixTimestamp)
        {
            if (unixTimestamp == 0) return "N/A";
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).LocalDateTime;
            return dateTime.ToString("yyyy-MM-dd");
        }
        
        private void DataGridViewMods_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == dataGridViewMods.Columns["name"].Index)
            {
                var mod = dataGridViewMods.Rows[e.RowIndex].Tag as ModInfo;
                if (mod != null && !string.IsNullOrEmpty(mod.PageUrl))
                {
                    try { Process.Start(new ProcessStartInfo(mod.PageUrl) { UseShellExecute = true }); }
                    catch (Exception ex) { MessageBox.Show($"Could not open link: {ex.Message}"); }
                }
            }
        }

        private async Task LoadPreviewImageAsync(string imageUrl)
        {
            pictureBoxPreview.Image = null;
            if (string.IsNullOrEmpty(imageUrl)) return;
            try
            {
                var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    pictureBoxPreview.Image = Image.FromStream(stream);
                }
            }
            catch (Exception) {}
        }
        
        public async Task FinalizeSelection()
        {
            foreach (DataGridViewRow row in dataGridViewMods.Rows)
            {
                if (Convert.ToBoolean(row.Cells["select"].Value))
                {
                    var mod = row.Tag as ModInfo;
                    if (mod == null) continue;

                    bool isInstalled = _installedModNames.Contains(mod.Name, StringComparer.OrdinalIgnoreCase);
                    bool hasUpdate = row.Cells["status"].Value.ToString() == "Update Available";

                    // Respect the "AllowReinstall" setting
                    if (isInstalled && !hasUpdate && SettingsService.Settings.AllowReinstall)
                    {
                        var result = MessageBox.Show($"The mod '{mod.Name}' is already installed. Do you want to reinstall it?", 
                            "Reinstall Mod?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.No) continue;
                    }
            
                    List<ModFile> availableFiles = await ModDownloader.GetModFileInfo(mod.Id);
                    if (availableFiles.Count > 1 && SettingsService.Settings.AlwaysSelectFile)
                    {
                        // Show file selection form
                        using (var fileForm = new FileSelectionForm(mod.Name, availableFiles))
                        {
                            if (fileForm.ShowDialog() == DialogResult.OK && fileForm.SelectedFile != null)
                            {
                                FinalFilesToDownload.Add(fileForm.SelectedFile);
                                FileToModInfoMap[fileForm.SelectedFile.FileName] = mod;
                            }
                        }
                    }
                    else if (availableFiles.Count > 0)
                    {
                        // Auto-select the first file
                        FinalFilesToDownload.Add(availableFiles[0]);
                        FileToModInfoMap[availableFiles[0].FileName] = mod;
                    }
                }
            }
        }

        private void SetupDataGridViewStyles()
        {
            dataGridViewMods.BackgroundColor = Color.FromArgb(63, 63, 70);
            dataGridViewMods.BorderStyle = BorderStyle.None;
            dataGridViewMods.GridColor = Color.FromArgb(85, 85, 85);
            dataGridViewMods.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
            dataGridViewMods.DefaultCellStyle.ForeColor = Color.White;
            dataGridViewMods.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204);
            dataGridViewMods.DefaultCellStyle.SelectionForeColor = Color.White;
            dataGridViewMods.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 63, 70);
            dataGridViewMods.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridViewMods.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(63, 63, 70);
            dataGridViewMods.EnableHeadersVisualStyles = false;
        }
    }
}