using System.Windows.Forms;

namespace UFO_50_Mod_Loader
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            fileSystemWatcher1 = new FileSystemWatcher();
            dataGridView1 = new DataGridView();
            buttonInstall = new Button();
            textBox1 = new TextBox();
            buttonDownload = new Button();
            buttonLaunch = new Button();
            buttonSettings = new Button();
            textBoxSearch = new TextBox();
            labelSearch = new Label();
            buttonUninstall = new Button();
            checkBoxHide = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)fileSystemWatcher1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // fileSystemWatcher1
            // 
            fileSystemWatcher1.EnableRaisingEvents = true;
            fileSystemWatcher1.SynchronizingObject = this;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(11, 93);
            dataGridView1.Margin = new Padding(2);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new Size(312, 132);
            dataGridView1.TabIndex = 0;
            // 
            // buttonInstall
            // 
            buttonInstall.Location = new Point(11, 31);
            buttonInstall.Margin = new Padding(2);
            buttonInstall.Name = "buttonInstall";
            buttonInstall.Size = new Size(149, 47);
            buttonInstall.TabIndex = 1;
            buttonInstall.Text = "Install Mods";
            buttonInstall.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBox1.Location = new Point(0, 661);
            textBox1.Margin = new Padding(4);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(1434, 239);
            textBox1.TabIndex = 2;
            // 
            // buttonDownload
            // 
            buttonDownload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonDownload.Location = new Point(931, 31);
            buttonDownload.Margin = new Padding(2);
            buttonDownload.Name = "buttonDownload";
            buttonDownload.Size = new Size(182, 47);
            buttonDownload.TabIndex = 3;
            buttonDownload.Text = "Download Mods";
            buttonDownload.UseVisualStyleBackColor = true;
            // 
            // buttonLaunch
            // 
            buttonLaunch.Location = new Point(180, 31);
            buttonLaunch.Margin = new Padding(2);
            buttonLaunch.Name = "buttonLaunch";
            buttonLaunch.Size = new Size(160, 47);
            buttonLaunch.TabIndex = 4;
            buttonLaunch.Text = "Launch Game";
            buttonLaunch.UseVisualStyleBackColor = true;
            // 
            // buttonSettings
            // 
            buttonSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonSettings.Location = new Point(1324, 31);
            buttonSettings.Name = "buttonSettings";
            buttonSettings.Size = new Size(102, 47);
            buttonSettings.TabIndex = 6;
            buttonSettings.Text = "Settings";
            buttonSettings.UseVisualStyleBackColor = true;
            // 
            // textBoxSearch
            // 
            textBoxSearch.BorderStyle = BorderStyle.None;
            textBoxSearch.Location = new Point(364, 44);
            textBoxSearch.MaxLength = 100;
            textBoxSearch.Name = "textBoxSearch";
            textBoxSearch.Size = new Size(329, 28);
            textBoxSearch.TabIndex = 7;
            textBoxSearch.KeyDown += textBoxSearch_KeyDown;
            // 
            // labelSearch
            // 
            labelSearch.AutoSize = true;
            labelSearch.Location = new Point(360, 9);
            labelSearch.Name = "labelSearch";
            labelSearch.Size = new Size(75, 30);
            labelSearch.TabIndex = 8;
            labelSearch.Text = "Search";
            // 
            // buttonUninstall
            // 
            buttonUninstall.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonUninstall.Location = new Point(1137, 31);
            buttonUninstall.Margin = new Padding(2);
            buttonUninstall.Name = "buttonUninstall";
            buttonUninstall.Size = new Size(166, 47);
            buttonUninstall.TabIndex = 9;
            buttonUninstall.Text = "Uninstall Mods";
            buttonUninstall.UseVisualStyleBackColor = true;
            // 
            // checkBoxHide
            // 
            checkBoxHide.AutoSize = true;
            checkBoxHide.Location = new Point(733, 38);
            checkBoxHide.Name = "checkBoxHide";
            checkBoxHide.Size = new Size(168, 34);
            checkBoxHide.TabIndex = 10;
            checkBoxHide.Text = "Hide Disabled";
            checkBoxHide.UseVisualStyleBackColor = true;
            checkBoxHide.Checked = false;
            checkBoxHide.CheckedChanged += checkBoxHide_CheckedChanged;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1438, 905);
            Controls.Add(checkBoxHide);
            Controls.Add(buttonUninstall);
            Controls.Add(labelSearch);
            Controls.Add(textBoxSearch);
            Controls.Add(buttonDownload);
            Controls.Add(buttonLaunch);
            Controls.Add(textBox1);
            Controls.Add(buttonInstall);
            Controls.Add(dataGridView1);
            Controls.Add(buttonSettings);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            Name = "MainForm";
            Text = "UFO 50 Mod Loader";
            ((System.ComponentModel.ISupportInitialize)fileSystemWatcher1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private FileSystemWatcher fileSystemWatcher1;
        private DataGridView dataGridView1;
        private Button buttonInstall;
        private TextBox textBox1;
        private Button buttonDownload;
        private Button buttonLaunch;
        private Button buttonSettings;
        private TextBox textBoxSearch;
        private Label labelSearch;
        private Button buttonUninstall;
        private CheckBox checkBoxHide;
    }
}
