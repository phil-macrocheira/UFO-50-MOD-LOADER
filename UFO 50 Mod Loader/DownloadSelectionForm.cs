using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace UFO_50_Mod_Loader
{
    public partial class DownloadSelectionForm : Form
    {
        // Controls
        public DataGridView dataGridViewMods;
        private Button buttonDownloadSelected;
        private PictureBox pictureBoxPreview;
        private Label labelModTitle;
        private Label labelModder;
        private RichTextBox textBoxDescription;
        private SplitContainer splitContainer;
        private Label labelMetadata;

        // Properties and Data
        public List<ModFile> FinalFilesToDownload { get; private set; } = new List<ModFile>();
        public Dictionary<string, ModInfo> FileToModInfoMap { get; private set; } = new Dictionary<string, ModInfo>();
        private Dictionary<string, (string version, long date)> _installedModVersions;
        private List<ModInfo> _allMods = new List<ModInfo>();
        private static HttpClient _httpClient = new HttpClient();

        // Using the corrected constructor
        public DownloadSelectionForm(Dictionary<string, (string version, long date)> installedModVersions) {
            _installedModVersions = installedModVersions;
            InitializeComponent();
            this.Load += async (s, e) => await LoadModsList();
        }

        private void InitializeComponent() {
            this.ShowIcon = false;
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterParent;
            bool isDarkMode = SettingsService.Settings.DarkModeEnabled;
            Color formBgColor, controlBgColor, textColor, borderColor, selectionBgColor, subPanelColor, linkColor, activeLinkColor, visitedLinkColor, dimTextColor;

            if (isDarkMode) {
                formBgColor = Color.FromArgb(45, 45, 48);
                controlBgColor = Color.FromArgb(63, 63, 70);
                textColor = Color.White;
                dimTextColor = Color.Gray;
                borderColor = Color.FromArgb(85, 85, 85);
                selectionBgColor = Color.FromArgb(0, 122, 204);
                subPanelColor = Color.FromArgb(30, 30, 30);
                linkColor = Color.Cyan;
                activeLinkColor = Color.White;
                visitedLinkColor = Color.Plum;
            }
            else {
                formBgColor = SystemColors.Control;
                controlBgColor = SystemColors.Window;
                textColor = SystemColors.ControlText;
                dimTextColor = SystemColors.GrayText;
                borderColor = SystemColors.ControlDark;
                selectionBgColor = Color.LightBlue;
                subPanelColor = SystemColors.Control;
                linkColor = Color.Blue;
                activeLinkColor = Color.White;
                visitedLinkColor = Color.Purple;
            }

            this.BackColor = formBgColor;
            this.ForeColor = textColor;

            splitContainer = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 620, IsSplitterFixed = true, BackColor = Color.FromArgb(85, 85, 85) };
            splitContainer.Panel1.BackColor = formBgColor;
            splitContainer.Panel2.BackColor = formBgColor;

            dataGridViewMods = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowTemplate = { Height = 40 }, AllowUserToResizeRows = false };
            splitContainer.Panel1.Controls.Add(dataGridViewMods);
            dataGridViewMods.SelectionChanged += DataGridViewMods_SelectionChanged;
            dataGridViewMods.CellContentClick += DataGridViewMods_CellContentClick;

            // Select All button
            var checkHeaderCell = new DataGridViewCheckBoxHeaderCellDownload();
            checkHeaderCell.OnCheckBoxClicked += (state) => {
                dataGridViewMods.EndEdit();
                foreach (DataGridViewRow row in dataGridViewMods.Rows) {
                    // Only change if the checkbox is not read-only
                    if (!row.Cells["select"].ReadOnly) {
                        row.Cells["select"].Value = state;
                    }
                }
                dataGridViewMods.RefreshEdit();
            };

            var linkStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = textColor, SelectionForeColor = textColor };
            var checkColumn = new DataGridViewCheckBoxColumn { HeaderText = "", Width = 60, Name = "select", Resizable = DataGridViewTriState.False, HeaderCell = checkHeaderCell };
            var nameColumn = new DataGridViewLinkColumn
            {
                HeaderText = "Mod",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.Automatic,
                Name = "name",
                ReadOnly = true,
                TrackVisitedState = true, // Set to true to see color change
                LinkColor = linkColor,
                ActiveLinkColor = activeLinkColor,
                VisitedLinkColor = visitedLinkColor
            };
            var versionColumn = new DataGridViewTextBoxColumn { HeaderText = "Version", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, Name = "version", ReadOnly = true };
            var creatorColumn = new DataGridViewTextBoxColumn { HeaderText = "Modder", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, Name = "creator", ReadOnly = true };
            var dateColumn = new DataGridViewTextBoxColumn { HeaderText = "Updated", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, Name = "date", ReadOnly = true };
            //var statusColumn = new DataGridViewTextBoxColumn { HeaderText = "Status", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, Name = "status", ReadOnly = true };
            dataGridViewMods.Columns.AddRange(new DataGridViewColumn[] { checkColumn, nameColumn, versionColumn, creatorColumn, dateColumn});

            Controls.Add(splitContainer);
            splitContainer.SplitterDistance = this.ClientSize.Width - 450;
            splitContainer.IsSplitterFixed = false;
            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = formBgColor };
            pictureBoxPreview = new PictureBox { Dock = DockStyle.Top, Height = 220, SizeMode = PictureBoxSizeMode.Zoom, BackColor = subPanelColor };
            labelModTitle = new Label { Dock = DockStyle.Top, Padding = new Padding(0, 8, 0, 0), Font = new Font("Segoe UI", 12F, FontStyle.Bold), Height = 40, AutoSize = false, TextAlign = ContentAlignment.BottomLeft };
            labelModder = new Label { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F, FontStyle.Italic), ForeColor = dimTextColor, Height = 30 };
            textBoxDescription = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = controlBgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            textBoxDescription.SetInnerMargins(8, 8, 8, 8); // Custom method to add padding

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(0, 10, 0, 0) };
            labelMetadata = new Label { Dock = DockStyle.Fill, ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleLeft };
            buttonDownloadSelected = new Button { Text = "Download", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 140, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White};
            buttonDownloadSelected.FlatAppearance.BorderSize = 0;
            if (!isDarkMode) {
                buttonDownloadSelected.FlatAppearance.BorderSize = 2;
                buttonDownloadSelected.FlatAppearance.BorderColor = Color.DarkBlue;
                buttonDownloadSelected.Font = new Font(buttonDownloadSelected.Font, FontStyle.Bold);
                buttonDownloadSelected.BackColor = Color.FromArgb(76, 152, 255);
            }

            buttonPanel.Controls.Add(labelMetadata);
            buttonPanel.Controls.Add(buttonDownloadSelected);

            rightPanel.Controls.Add(textBoxDescription);
            rightPanel.Controls.Add(labelModder);
            rightPanel.Controls.Add(labelModTitle);
            rightPanel.Controls.Add(pictureBoxPreview);
            rightPanel.Controls.Add(buttonPanel);
            splitContainer.Panel2.Controls.Add(rightPanel);

            this.Controls.Add(splitContainer);
            this.AcceptButton = buttonDownloadSelected;
            SetupDataGridViewStyles(formBgColor, controlBgColor, textColor, borderColor, selectionBgColor);
        }

        private async Task LoadModsList() {
            try {
                this.Text = "Fetching Mod List...";
                _allMods = await ModDownloader.GetModInfo("23000");
                dataGridViewMods.Rows.Clear();
                foreach (var mod in _allMods) {
                    string status = "";
                    string version = mod.Version.Replace("v", "") ?? "1.0";
                    if (string.IsNullOrEmpty(version))
                        version = "1.0";

                    var updatedDate = FormatUnixTimestamp(mod.DateUpdated);
                    var addedDate = FormatUnixTimestamp(mod.DateAdded);
                    var date = updatedDate == "N/A" ? addedDate : updatedDate;

                    /* Comment out for now
                    if (_installedModVersions.TryGetValue(mod.Id, out var localVersion)) {
                        status = $"Installed ({localVersion.version ?? "1.0"})";
                        if (mod.DateUpdated > localVersion.date || mod.Version != localVersion.version) {
                            status = "Update Available";
                        }
                    }
                    */
                    var rowIndex = dataGridViewMods.Rows.Add(status == "Update Available", mod.Name, version, mod.Creator, date);
                    dataGridViewMods.Rows[rowIndex].Tag = mod;

                    var checkboxCell = (DataGridViewCheckBoxCell)dataGridViewMods.Rows[rowIndex].Cells["select"];
                    /* Comment out for now
                    if (status.StartsWith("Installed")) {
                        dataGridViewMods.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DimGray;
                    }
                    if (status == "Update Available") {
                        dataGridViewMods.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(40, 80, 40);
                        dataGridViewMods.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.LightGreen;
                    }
                    checkboxCell.ReadOnly = status.StartsWith("Installed") && !SettingsService.Settings.AllowReinstall;
                    */
                }
                this.Text = "GameBanana Mod Downloader";
            }
            catch (Exception ex) {
                MessageBox.Show($"Failed to fetch mod list from Gamebanana: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            finally {
                dataGridViewMods.Sort(dataGridViewMods.Columns["date"], ListSortDirection.Descending);
                if (dataGridViewMods.Rows.Count > 0) {
                    dataGridViewMods.ClearSelection();
                    dataGridViewMods.Rows[0].Selected = true;
                }
            }
        }

        private async void DataGridViewMods_SelectionChanged(object sender, EventArgs e) {
            if (dataGridViewMods.SelectedRows.Count > 0) {
                var mod = dataGridViewMods.SelectedRows[0].Tag as ModInfo;
                if (mod != null) {
                    labelModTitle.Text = mod.Name;
                    labelModder.Text = $"by {mod.Creator}";
                    var addedDate = FormatUnixTimestamp(mod.DateAdded);
                    labelMetadata.Text = $"Uploaded: {addedDate}";

                    textBoxDescription.Text = string.IsNullOrEmpty(mod.Description) ? "Loading full details..." : $"{mod.Description}\n\nLoading full details...";

                    await LoadPreviewImageAsync(mod.ImageUrl);
                    string fullDescription = await ModDownloader.GetModFullDescription(mod.Id);

                    if (dataGridViewMods.SelectedRows.Count > 0 && (dataGridViewMods.SelectedRows[0].Tag as ModInfo)?.Id == mod.Id) {
                        textBoxDescription.Text = fullDescription;
                    }
                }
            }
        }

        private string FormatUnixTimestamp(long unixTimestamp) {
            if (unixTimestamp == 0) return "N/A";
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).LocalDateTime;
            return dateTime.ToString("yyyy-MM-dd");
        }

        private void DataGridViewMods_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == dataGridViewMods.Columns["name"].Index) {
                var mod = dataGridViewMods.Rows[e.RowIndex].Tag as ModInfo;
                if (mod != null && !string.IsNullOrEmpty(mod.PageUrl)) {
                    try { Process.Start(new ProcessStartInfo(mod.PageUrl) { UseShellExecute = true }); }
                    catch (Exception ex) { MessageBox.Show($"Could not open link: {ex.Message}"); }
                }
            }
        }

        private async Task LoadPreviewImageAsync(string imageUrl) {
            pictureBoxPreview.Image = null;
            if (string.IsNullOrEmpty(imageUrl)) return;
            try {
                var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync()) {
                    pictureBoxPreview.Image = Image.FromStream(stream);
                }
            }
            catch (Exception) { }
        }

        public async Task FinalizeSelection() {
            foreach (DataGridViewRow row in dataGridViewMods.Rows) {
                if (Convert.ToBoolean(row.Cells["select"].Value)) {
                    var mod = row.Tag as ModInfo;
                    if (mod == null) continue;

                    /* Comment out for now
                    bool isInstalled = _installedModVersions.ContainsKey(mod.Id);
                    if (isInstalled && !SettingsService.Settings.AllowReinstall && row.Cells["status"].Value.ToString() != "Update Available") {
                        continue;
                    }
                    */

                    List<ModFile> availableFiles = await ModDownloader.GetModFileInfo(mod.Id);
                    if (availableFiles.Count > 1 && SettingsService.Settings.AlwaysSelectFile) {
                        using (var fileForm = new FileSelectionForm(mod.Name, availableFiles)) {
                            if (fileForm.ShowDialog() == DialogResult.OK && fileForm.SelectedFile != null) {
                                FinalFilesToDownload.Add(fileForm.SelectedFile);
                                FileToModInfoMap[fileForm.SelectedFile.FileName] = mod;
                            }
                        }
                    }
                    else if (availableFiles.Count > 0) {
                        FinalFilesToDownload.Add(availableFiles[0]);
                        FileToModInfoMap[availableFiles[0].FileName] = mod;
                    }
                }
            }
        }

        private void SetupDataGridViewStyles(Color formBgColor, Color controlBgColor, Color textColor, Color borderColor, Color selectionBgColor) {
            dataGridViewMods.BackgroundColor = controlBgColor;
            dataGridViewMods.BorderStyle = BorderStyle.None;
            dataGridViewMods.GridColor = borderColor;
            dataGridViewMods.DefaultCellStyle.BackColor = formBgColor;
            dataGridViewMods.DefaultCellStyle.ForeColor = textColor;
            dataGridViewMods.DefaultCellStyle.SelectionBackColor = selectionBgColor;
            dataGridViewMods.DefaultCellStyle.SelectionForeColor = textColor;
            dataGridViewMods.ColumnHeadersDefaultCellStyle.BackColor = controlBgColor;
            dataGridViewMods.ColumnHeadersDefaultCellStyle.ForeColor = textColor;
            dataGridViewMods.ColumnHeadersDefaultCellStyle.SelectionBackColor = controlBgColor;
            dataGridViewMods.EnableHeadersVisualStyles = false;
            dataGridViewMods.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewMods.ColumnHeadersHeight = 40;
        }
    }

    public class DataGridViewCheckBoxHeaderCellDownload : DataGridViewColumnHeaderCell
    {
        public delegate void CheckBoxClickedHandler(bool state);
        public event CheckBoxClickedHandler OnCheckBoxClicked;

        private bool _checked = false;
        private Point _location;
        private Size _size;

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds,
            int rowIndex, DataGridViewElementStates dataGridViewElementState,
            object value, object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts) {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState,
                "", "", errorText, cellStyle, advancedBorderStyle,
                paintParts & ~DataGridViewPaintParts.ContentForeground);

            Point p = new Point(
                cellBounds.X + (cellBounds.Width / 2) - 10,
                cellBounds.Y + (cellBounds.Height / 2) - 7
            );
            _location = p;
            _size = new Size(14, 14);
            CheckBoxRenderer.DrawCheckBox(graphics, _location, _checked
                ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
        }

        protected override void OnMouseClick(DataGridViewCellMouseEventArgs e) {
            Rectangle rect = new Rectangle(_location, _size);
            Point clickLocation = new Point(e.X, e.Y);
            if (rect.Contains(clickLocation)) {
                _checked = !_checked;
                OnCheckBoxClicked?.Invoke(_checked);
                this.DataGridView.InvalidateCell(this);
            }
            base.OnMouseClick(e);
        }
    }

    // Helper extension for RichTextBox padding
    public static class RichTextBoxExtensions
    {
        public static void SetInnerMargins(this RichTextBox richTextBox, int left, int top, int right, int bottom) {
            Rectangle r = richTextBox.ClientRectangle;
            richTextBox.SelectAll();
            richTextBox.SelectionIndent = left;
            richTextBox.SelectionRightIndent = right;
            richTextBox.DeselectAll();
        }
    }

    public class CustomLinkLabel : LinkLabel
    {
        protected override void OnPaint(PaintEventArgs e) {
            // Do not call the base OnPaint to prevent default underlining
            // MyBase.OnPaint(e); // This line is commented out

            using (SolidBrush brush = new SolidBrush(this.ForeColor)) {
                // Draw the text without any underline
                e.Graphics.DrawString(this.Text, this.Font, brush, e.ClipRectangle.X, e.ClipRectangle.Y);
            }
        }
    }
}