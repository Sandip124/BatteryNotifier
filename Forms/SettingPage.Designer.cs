using System.Windows.Forms;

namespace BatteryNotifier.Forms
{
    partial class SettingPage : Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingPage));
            this.SettingContainer = new System.Windows.Forms.TableLayoutPanel();
            this.SettingHeader = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.AppHeaderTitle = new System.Windows.Forms.Label();
            this.CloseIcon = new System.Windows.Forms.PictureBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.LaunchAtStartupPanel = new System.Windows.Forms.Panel();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.LaunchAtStartUpLabel = new System.Windows.Forms.Label();
            this.launchAtStartup = new System.Windows.Forms.CheckBox();
            this.FullBatteryNotificationGroupBox = new System.Windows.Forms.GroupBox();
            this.browseLowBatterySoundButton = new System.Windows.Forms.Button();
            this.lowBatterySoundPath = new System.Windows.Forms.TextBox();
            this.lowBatteryPercentageValue = new System.Windows.Forms.NumericUpDown();
            this.LowBatterySoundLabel = new System.Windows.Forms.Label();
            this.browseFullBatterySoundButton = new System.Windows.Forms.Button();
            this.LowBatteryIcon = new System.Windows.Forms.PictureBox();
            this.fullbatteryPercentageValue = new System.Windows.Forms.NumericUpDown();
            this.fullbatterySoundPath = new System.Windows.Forms.TextBox();
            this.lowBatteryTrackbar = new System.Windows.Forms.TrackBar();
            this.LowBatteryPercentageLabel = new System.Windows.Forms.Label();
            this.FullBatterySoundLabel = new System.Windows.Forms.Label();
            this.FullBatteryIcon = new System.Windows.Forms.PictureBox();
            this.showLowBatteryNotification = new System.Windows.Forms.CheckBox();
            this.ShowFullBatteryNotificationLabel = new System.Windows.Forms.Label();
            this.LowBatteryNotificationLabel = new System.Windows.Forms.Label();
            this.showFullBatteryNotification = new System.Windows.Forms.CheckBox();
            this.fullBatteryTrackbar = new System.Windows.Forms.TrackBar();
            this.BatteryPercentageLabel = new System.Windows.Forms.Label();
            this.DarkModelPanel = new System.Windows.Forms.Panel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.DarkModeLabel = new System.Windows.Forms.Label();
            this.DarkModeCheckbox = new System.Windows.Forms.CheckBox();
            this.ShowAsWindowPanel = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.ShowAsWindowLabel = new System.Windows.Forms.Label();
            this.ShowAsWindow = new System.Windows.Forms.CheckBox();
            this.SettingContainer.SuspendLayout();
            this.SettingHeader.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).BeginInit();
            this.panel2.SuspendLayout();
            this.LaunchAtStartupPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.FullBatteryNotificationGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryPercentageValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullbatteryPercentageValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryTrackbar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullBatteryTrackbar)).BeginInit();
            this.DarkModelPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.ShowAsWindowPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // SettingContainer
            // 
            this.SettingContainer.BackColor = System.Drawing.Color.White;
            this.SettingContainer.ColumnCount = 1;
            this.SettingContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SettingContainer.Controls.Add(this.SettingHeader, 0, 0);
            this.SettingContainer.Controls.Add(this.panel2, 0, 1);
            this.SettingContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SettingContainer.Location = new System.Drawing.Point(1, 1);
            this.SettingContainer.Name = "SettingContainer";
            this.SettingContainer.RowCount = 2;
            this.SettingContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.SettingContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SettingContainer.Size = new System.Drawing.Size(408, 508);
            this.SettingContainer.TabIndex = 0;
            // 
            // SettingHeader
            // 
            this.SettingHeader.BackColor = System.Drawing.Color.AliceBlue;
            this.SettingHeader.ColumnCount = 2;
            this.SettingHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SettingHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.SettingHeader.Controls.Add(this.panel1, 0, 0);
            this.SettingHeader.Controls.Add(this.CloseIcon, 1, 0);
            this.SettingHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SettingHeader.Location = new System.Drawing.Point(0, 0);
            this.SettingHeader.Margin = new System.Windows.Forms.Padding(0);
            this.SettingHeader.Name = "SettingHeader";
            this.SettingHeader.RowCount = 1;
            this.SettingHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SettingHeader.Size = new System.Drawing.Size(408, 40);
            this.SettingHeader.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.AppHeaderTitle);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(372, 40);
            this.panel1.TabIndex = 0;
            // 
            // AppHeaderTitle
            // 
            this.AppHeaderTitle.BackColor = System.Drawing.Color.Transparent;
            this.AppHeaderTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppHeaderTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.AppHeaderTitle.Location = new System.Drawing.Point(0, 0);
            this.AppHeaderTitle.Name = "AppHeaderTitle";
            this.AppHeaderTitle.Size = new System.Drawing.Size(372, 40);
            this.AppHeaderTitle.TabIndex = 17;
            this.AppHeaderTitle.Text = " Setting";
            this.AppHeaderTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.AppHeaderTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AppHeaderTitle_MouseDown);
            this.AppHeaderTitle.MouseMove += new System.Windows.Forms.MouseEventHandler(this.AppHeaderTitle_MouseMove);
            this.AppHeaderTitle.MouseUp += new System.Windows.Forms.MouseEventHandler(this.AppHeaderTitle_MouseUp);
            // 
            // CloseIcon
            // 
            this.CloseIcon.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CloseIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CloseIcon.Image = global::BatteryNotifier.Properties.Resources.closeIconLight;
            this.CloseIcon.Location = new System.Drawing.Point(372, 0);
            this.CloseIcon.Margin = new System.Windows.Forms.Padding(0);
            this.CloseIcon.Name = "CloseIcon";
            this.CloseIcon.Size = new System.Drawing.Size(36, 40);
            this.CloseIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.CloseIcon.TabIndex = 1;
            this.CloseIcon.TabStop = false;
            this.CloseIcon.Click += new System.EventHandler(this.CloseIcon_Click);
            this.CloseIcon.MouseEnter += new System.EventHandler(this.pictureBox1_MouseEnter);
            this.CloseIcon.MouseLeave += new System.EventHandler(this.pictureBox1_MouseLeave);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.LaunchAtStartupPanel);
            this.panel2.Controls.Add(this.FullBatteryNotificationGroupBox);
            this.panel2.Controls.Add(this.DarkModelPanel);
            this.panel2.Controls.Add(this.ShowAsWindowPanel);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 40);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(408, 468);
            this.panel2.TabIndex = 1;
            // 
            // LaunchAtStartupPanel
            // 
            this.LaunchAtStartupPanel.BackColor = System.Drawing.SystemColors.Menu;
            this.LaunchAtStartupPanel.Controls.Add(this.pictureBox3);
            this.LaunchAtStartupPanel.Controls.Add(this.LaunchAtStartUpLabel);
            this.LaunchAtStartupPanel.Controls.Add(this.launchAtStartup);
            this.LaunchAtStartupPanel.Location = new System.Drawing.Point(13, 96);
            this.LaunchAtStartupPanel.Name = "LaunchAtStartupPanel";
            this.LaunchAtStartupPanel.Size = new System.Drawing.Size(383, 34);
            this.LaunchAtStartupPanel.TabIndex = 29;
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox3.Image = global::BatteryNotifier.Properties.Resources.launchatstartup;
            this.pictureBox3.Location = new System.Drawing.Point(1, -2);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(34, 36);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox3.TabIndex = 28;
            this.pictureBox3.TabStop = false;
            // 
            // LaunchAtStartUpLabel
            // 
            this.LaunchAtStartUpLabel.AutoSize = true;
            this.LaunchAtStartUpLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LaunchAtStartUpLabel.Location = new System.Drawing.Point(37, 7);
            this.LaunchAtStartUpLabel.Name = "LaunchAtStartUpLabel";
            this.LaunchAtStartUpLabel.Size = new System.Drawing.Size(139, 20);
            this.LaunchAtStartUpLabel.TabIndex = 16;
            this.LaunchAtStartUpLabel.Text = "Launch At Startup";
            // 
            // launchAtStartup
            // 
            this.launchAtStartup.AutoSize = true;
            this.launchAtStartup.Location = new System.Drawing.Point(357, 13);
            this.launchAtStartup.Name = "launchAtStartup";
            this.launchAtStartup.Size = new System.Drawing.Size(15, 14);
            this.launchAtStartup.TabIndex = 17;
            this.launchAtStartup.UseVisualStyleBackColor = true;
            this.launchAtStartup.CheckedChanged += new System.EventHandler(this.launchAtStartup_CheckedChanged);
            // 
            // FullBatteryNotificationGroupBox
            // 
            this.FullBatteryNotificationGroupBox.BackColor = System.Drawing.Color.Transparent;
            this.FullBatteryNotificationGroupBox.Controls.Add(this.browseLowBatterySoundButton);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.lowBatterySoundPath);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.lowBatteryPercentageValue);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.LowBatterySoundLabel);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.browseFullBatterySoundButton);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.LowBatteryIcon);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.fullbatteryPercentageValue);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.fullbatterySoundPath);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.lowBatteryTrackbar);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.LowBatteryPercentageLabel);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.FullBatterySoundLabel);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.FullBatteryIcon);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.showLowBatteryNotification);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.ShowFullBatteryNotificationLabel);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.LowBatteryNotificationLabel);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.showFullBatteryNotification);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.fullBatteryTrackbar);
            this.FullBatteryNotificationGroupBox.Controls.Add(this.BatteryPercentageLabel);
            this.FullBatteryNotificationGroupBox.Location = new System.Drawing.Point(12, 148);
            this.FullBatteryNotificationGroupBox.Name = "FullBatteryNotificationGroupBox";
            this.FullBatteryNotificationGroupBox.Size = new System.Drawing.Size(383, 308);
            this.FullBatteryNotificationGroupBox.TabIndex = 25;
            this.FullBatteryNotificationGroupBox.TabStop = false;
            this.FullBatteryNotificationGroupBox.Text = "Notification Setting";
            // 
            // browseLowBatterySoundButton
            // 
            this.browseLowBatterySoundButton.BackColor = System.Drawing.Color.LightGray;
            this.browseLowBatterySoundButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.browseLowBatterySoundButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.browseLowBatterySoundButton.Location = new System.Drawing.Point(316, 258);
            this.browseLowBatterySoundButton.Name = "browseLowBatterySoundButton";
            this.browseLowBatterySoundButton.Size = new System.Drawing.Size(61, 23);
            this.browseLowBatterySoundButton.TabIndex = 23;
            this.browseLowBatterySoundButton.Text = "Browse";
            this.browseLowBatterySoundButton.UseVisualStyleBackColor = false;
            this.browseLowBatterySoundButton.Click += new System.EventHandler(this.browseLowBatterySoundButton_Click);
            // 
            // lowBatterySoundPath
            // 
            this.lowBatterySoundPath.Location = new System.Drawing.Point(119, 258);
            this.lowBatterySoundPath.Name = "lowBatterySoundPath";
            this.lowBatterySoundPath.Size = new System.Drawing.Size(183, 23);
            this.lowBatterySoundPath.TabIndex = 22;
            // 
            // lowBatteryPercentageValue
            // 
            this.lowBatteryPercentageValue.Enabled = false;
            this.lowBatteryPercentageValue.Location = new System.Drawing.Point(316, 217);
            this.lowBatteryPercentageValue.Name = "lowBatteryPercentageValue";
            this.lowBatteryPercentageValue.ReadOnly = true;
            this.lowBatteryPercentageValue.Size = new System.Drawing.Size(62, 23);
            this.lowBatteryPercentageValue.TabIndex = 25;
            // 
            // LowBatterySoundLabel
            // 
            this.LowBatterySoundLabel.AutoSize = true;
            this.LowBatterySoundLabel.BackColor = System.Drawing.Color.Transparent;
            this.LowBatterySoundLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LowBatterySoundLabel.Location = new System.Drawing.Point(12, 254);
            this.LowBatterySoundLabel.Name = "LowBatterySoundLabel";
            this.LowBatterySoundLabel.Size = new System.Drawing.Size(56, 20);
            this.LowBatterySoundLabel.TabIndex = 21;
            this.LowBatterySoundLabel.Text = "Sound";
            // 
            // browseFullBatterySoundButton
            // 
            this.browseFullBatterySoundButton.BackColor = System.Drawing.Color.LightGray;
            this.browseFullBatterySoundButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.browseFullBatterySoundButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.browseFullBatterySoundButton.Location = new System.Drawing.Point(316, 131);
            this.browseFullBatterySoundButton.Name = "browseFullBatterySoundButton";
            this.browseFullBatterySoundButton.Size = new System.Drawing.Size(62, 23);
            this.browseFullBatterySoundButton.TabIndex = 23;
            this.browseFullBatterySoundButton.Text = "Browse";
            this.browseFullBatterySoundButton.UseVisualStyleBackColor = false;
            this.browseFullBatterySoundButton.Click += new System.EventHandler(this.browseFullBatterySoundButton_Click);
            // 
            // LowBatteryIcon
            // 
            this.LowBatteryIcon.BackColor = System.Drawing.Color.Transparent;
            this.LowBatteryIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.LowBatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Low;
            this.LowBatteryIcon.Location = new System.Drawing.Point(11, 166);
            this.LowBatteryIcon.Name = "LowBatteryIcon";
            this.LowBatteryIcon.Size = new System.Drawing.Size(41, 37);
            this.LowBatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.LowBatteryIcon.TabIndex = 26;
            this.LowBatteryIcon.TabStop = false;
            // 
            // fullbatteryPercentageValue
            // 
            this.fullbatteryPercentageValue.Enabled = false;
            this.fullbatteryPercentageValue.Location = new System.Drawing.Point(316, 83);
            this.fullbatteryPercentageValue.Name = "fullbatteryPercentageValue";
            this.fullbatteryPercentageValue.ReadOnly = true;
            this.fullbatteryPercentageValue.Size = new System.Drawing.Size(62, 23);
            this.fullbatteryPercentageValue.TabIndex = 24;
            // 
            // fullbatterySoundPath
            // 
            this.fullbatterySoundPath.Location = new System.Drawing.Point(119, 131);
            this.fullbatterySoundPath.Name = "fullbatterySoundPath";
            this.fullbatterySoundPath.Size = new System.Drawing.Size(183, 23);
            this.fullbatterySoundPath.TabIndex = 22;
            // 
            // lowBatteryTrackbar
            // 
            this.lowBatteryTrackbar.LargeChange = 10;
            this.lowBatteryTrackbar.Location = new System.Drawing.Point(119, 210);
            this.lowBatteryTrackbar.Maximum = 100;
            this.lowBatteryTrackbar.Name = "lowBatteryTrackbar";
            this.lowBatteryTrackbar.Size = new System.Drawing.Size(183, 45);
            this.lowBatteryTrackbar.TabIndex = 20;
            this.lowBatteryTrackbar.Scroll += new System.EventHandler(this.lowBatteryTrackbar_Scroll);
            this.lowBatteryTrackbar.ValueChanged += new System.EventHandler(this.lowBatteryTrackbar_ValueChanged);
            // 
            // LowBatteryPercentageLabel
            // 
            this.LowBatteryPercentageLabel.AutoSize = true;
            this.LowBatteryPercentageLabel.BackColor = System.Drawing.Color.Transparent;
            this.LowBatteryPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LowBatteryPercentageLabel.Location = new System.Drawing.Point(10, 210);
            this.LowBatteryPercentageLabel.Name = "LowBatteryPercentageLabel";
            this.LowBatteryPercentageLabel.Size = new System.Drawing.Size(99, 20);
            this.LowBatteryPercentageLabel.TabIndex = 19;
            this.LowBatteryPercentageLabel.Text = "Percentage :";
            // 
            // FullBatterySoundLabel
            // 
            this.FullBatterySoundLabel.AutoSize = true;
            this.FullBatterySoundLabel.BackColor = System.Drawing.Color.Transparent;
            this.FullBatterySoundLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FullBatterySoundLabel.Location = new System.Drawing.Point(11, 126);
            this.FullBatterySoundLabel.Name = "FullBatterySoundLabel";
            this.FullBatterySoundLabel.Size = new System.Drawing.Size(56, 20);
            this.FullBatterySoundLabel.TabIndex = 21;
            this.FullBatterySoundLabel.Text = "Sound";
            // 
            // FullBatteryIcon
            // 
            this.FullBatteryIcon.BackColor = System.Drawing.Color.Transparent;
            this.FullBatteryIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.FullBatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.FullBatteryIcon.Location = new System.Drawing.Point(11, 33);
            this.FullBatteryIcon.Name = "FullBatteryIcon";
            this.FullBatteryIcon.Size = new System.Drawing.Size(41, 36);
            this.FullBatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.FullBatteryIcon.TabIndex = 25;
            this.FullBatteryIcon.TabStop = false;
            // 
            // showLowBatteryNotification
            // 
            this.showLowBatteryNotification.AutoSize = true;
            this.showLowBatteryNotification.Location = new System.Drawing.Point(362, 181);
            this.showLowBatteryNotification.Name = "showLowBatteryNotification";
            this.showLowBatteryNotification.Size = new System.Drawing.Size(15, 14);
            this.showLowBatteryNotification.TabIndex = 18;
            this.showLowBatteryNotification.UseVisualStyleBackColor = true;
            this.showLowBatteryNotification.CheckedChanged += new System.EventHandler(this.showLowBatteryNotification_CheckedChanged);
            // 
            // ShowFullBatteryNotificationLabel
            // 
            this.ShowFullBatteryNotificationLabel.BackColor = System.Drawing.Color.Transparent;
            this.ShowFullBatteryNotificationLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ShowFullBatteryNotificationLabel.Location = new System.Drawing.Point(58, 40);
            this.ShowFullBatteryNotificationLabel.Name = "ShowFullBatteryNotificationLabel";
            this.ShowFullBatteryNotificationLabel.Size = new System.Drawing.Size(193, 27);
            this.ShowFullBatteryNotificationLabel.TabIndex = 18;
            this.ShowFullBatteryNotificationLabel.Text = "Show Full Battery Notification";
            // 
            // LowBatteryNotificationLabel
            // 
            this.LowBatteryNotificationLabel.BackColor = System.Drawing.Color.Transparent;
            this.LowBatteryNotificationLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LowBatteryNotificationLabel.Location = new System.Drawing.Point(58, 171);
            this.LowBatteryNotificationLabel.Name = "LowBatteryNotificationLabel";
            this.LowBatteryNotificationLabel.Size = new System.Drawing.Size(193, 28);
            this.LowBatteryNotificationLabel.TabIndex = 18;
            this.LowBatteryNotificationLabel.Text = "Show Low Battery Notification";
            // 
            // showFullBatteryNotification
            // 
            this.showFullBatteryNotification.AutoSize = true;
            this.showFullBatteryNotification.Location = new System.Drawing.Point(362, 50);
            this.showFullBatteryNotification.Name = "showFullBatteryNotification";
            this.showFullBatteryNotification.Size = new System.Drawing.Size(15, 14);
            this.showFullBatteryNotification.TabIndex = 18;
            this.showFullBatteryNotification.UseVisualStyleBackColor = true;
            this.showFullBatteryNotification.CheckedChanged += new System.EventHandler(this.showFullBatteryNotification_CheckedChanged);
            // 
            // fullBatteryTrackbar
            // 
            this.fullBatteryTrackbar.LargeChange = 10;
            this.fullBatteryTrackbar.Location = new System.Drawing.Point(119, 80);
            this.fullBatteryTrackbar.Maximum = 100;
            this.fullBatteryTrackbar.Name = "fullBatteryTrackbar";
            this.fullBatteryTrackbar.Size = new System.Drawing.Size(183, 45);
            this.fullBatteryTrackbar.TabIndex = 20;
            this.fullBatteryTrackbar.Scroll += new System.EventHandler(this.fullBatteryTrackbar_Scroll);
            this.fullBatteryTrackbar.ValueChanged += new System.EventHandler(this.fullBatteryTrackbar_ValueChanged);
            // 
            // BatteryPercentageLabel
            // 
            this.BatteryPercentageLabel.AutoSize = true;
            this.BatteryPercentageLabel.BackColor = System.Drawing.Color.Transparent;
            this.BatteryPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.BatteryPercentageLabel.Location = new System.Drawing.Point(12, 80);
            this.BatteryPercentageLabel.Name = "BatteryPercentageLabel";
            this.BatteryPercentageLabel.Size = new System.Drawing.Size(91, 20);
            this.BatteryPercentageLabel.TabIndex = 19;
            this.BatteryPercentageLabel.Text = "Percentage";
            // 
            // DarkModelPanel
            // 
            this.DarkModelPanel.BackColor = System.Drawing.SystemColors.Menu;
            this.DarkModelPanel.Controls.Add(this.pictureBox2);
            this.DarkModelPanel.Controls.Add(this.DarkModeLabel);
            this.DarkModelPanel.Controls.Add(this.DarkModeCheckbox);
            this.DarkModelPanel.Location = new System.Drawing.Point(12, 55);
            this.DarkModelPanel.Name = "DarkModelPanel";
            this.DarkModelPanel.Size = new System.Drawing.Size(383, 34);
            this.DarkModelPanel.TabIndex = 19;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox2.Image = global::BatteryNotifier.Properties.Resources.DarkMode;
            this.pictureBox2.Location = new System.Drawing.Point(1, -2);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(34, 36);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 28;
            this.pictureBox2.TabStop = false;
            // 
            // DarkModeLabel
            // 
            this.DarkModeLabel.AutoSize = true;
            this.DarkModeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.DarkModeLabel.Location = new System.Drawing.Point(37, 7);
            this.DarkModeLabel.Name = "DarkModeLabel";
            this.DarkModeLabel.Size = new System.Drawing.Size(87, 20);
            this.DarkModeLabel.TabIndex = 16;
            this.DarkModeLabel.Text = "Dark Mode";
            // 
            // DarkModeCheckbox
            // 
            this.DarkModeCheckbox.AutoSize = true;
            this.DarkModeCheckbox.Location = new System.Drawing.Point(357, 13);
            this.DarkModeCheckbox.Name = "DarkModeCheckbox";
            this.DarkModeCheckbox.Size = new System.Drawing.Size(15, 14);
            this.DarkModeCheckbox.TabIndex = 17;
            this.DarkModeCheckbox.UseVisualStyleBackColor = true;
            this.DarkModeCheckbox.CheckedChanged += new System.EventHandler(this.DarkModeCheckbox_CheckedChanged);
            // 
            // ShowAsWindowPanel
            // 
            this.ShowAsWindowPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ShowAsWindowPanel.Controls.Add(this.pictureBox1);
            this.ShowAsWindowPanel.Controls.Add(this.ShowAsWindowLabel);
            this.ShowAsWindowPanel.Controls.Add(this.ShowAsWindow);
            this.ShowAsWindowPanel.Location = new System.Drawing.Point(12, 12);
            this.ShowAsWindowPanel.Name = "ShowAsWindowPanel";
            this.ShowAsWindowPanel.Size = new System.Drawing.Size(383, 34);
            this.ShowAsWindowPanel.TabIndex = 18;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Image = global::BatteryNotifier.Properties.Resources.Window;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(35, 36);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 27;
            this.pictureBox1.TabStop = false;
            // 
            // ShowAsWindowLabel
            // 
            this.ShowAsWindowLabel.AutoSize = true;
            this.ShowAsWindowLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ShowAsWindowLabel.Location = new System.Drawing.Point(37, 7);
            this.ShowAsWindowLabel.Name = "ShowAsWindowLabel";
            this.ShowAsWindowLabel.Size = new System.Drawing.Size(126, 20);
            this.ShowAsWindowLabel.TabIndex = 16;
            this.ShowAsWindowLabel.Text = "Show as window";
            this.ShowAsWindowLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ShowAsWindow
            // 
            this.ShowAsWindow.AutoSize = true;
            this.ShowAsWindow.Location = new System.Drawing.Point(357, 12);
            this.ShowAsWindow.Name = "ShowAsWindow";
            this.ShowAsWindow.Size = new System.Drawing.Size(15, 14);
            this.ShowAsWindow.TabIndex = 17;
            this.ShowAsWindow.UseVisualStyleBackColor = true;
            this.ShowAsWindow.CheckedChanged += new System.EventHandler(this.ShowAsWindow_CheckedChanged);
            // 
            // SettingPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.ClientSize = new System.Drawing.Size(410, 510);
            this.Controls.Add(this.SettingContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SettingPage";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.Text = "SettingPage";
            this.Activated += new System.EventHandler(this.SettingPage_Activated);
            this.Load += new System.EventHandler(this.SettingPage_Load);
            this.SettingContainer.ResumeLayout(false);
            this.SettingHeader.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).EndInit();
            this.panel2.ResumeLayout(false);
            this.LaunchAtStartupPanel.ResumeLayout(false);
            this.LaunchAtStartupPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.FullBatteryNotificationGroupBox.ResumeLayout(false);
            this.FullBatteryNotificationGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryPercentageValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullbatteryPercentageValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryTrackbar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullBatteryTrackbar)).EndInit();
            this.DarkModelPanel.ResumeLayout(false);
            this.DarkModelPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ShowAsWindowPanel.ResumeLayout(false);
            this.ShowAsWindowPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel SettingContainer;
        private TableLayoutPanel tableLayoutPanel2;
        private Panel panel1;
        private PictureBox CloseIcon;
        private Label AppHeaderTitle;
        private TableLayoutPanel SettingHeader;
        private Panel panel2;
        private Label ShowAsWindowLabel;
        private TrackBar fullBatteryTrackbar;
        private Label BatteryPercentageLabel;
        private CheckBox showFullBatteryNotification;
        private Label ShowFullBatteryNotificationLabel;
        private Panel ShowAsWindowPanel;
        private CheckBox ShowAsWindow;
        private Button browseLowBatterySoundButton;
        private TextBox lowBatterySoundPath;
        private Label LowBatterySoundLabel;
        private TrackBar lowBatteryTrackbar;
        private Label LowBatteryPercentageLabel;
        private CheckBox showLowBatteryNotification;
        private Label LowBatteryNotificationLabel;
        private Button browseFullBatterySoundButton;
        private TextBox fullbatterySoundPath;
        private Label FullBatterySoundLabel;
        private PictureBox LowBatteryIcon;
        private PictureBox FullBatteryIcon;
        private NumericUpDown lowBatteryPercentageValue;
        private NumericUpDown fullbatteryPercentageValue;
        private Panel DarkModelPanel;
        private Label DarkModeLabel;
        private CheckBox DarkModeCheckbox;
        private GroupBox FullBatteryNotificationGroupBox;
        private PictureBox pictureBox2;
        private PictureBox pictureBox1;
        private Panel LaunchAtStartupPanel;
        private PictureBox pictureBox3;
        private Label LaunchAtStartUpLabel;
        private CheckBox launchAtStartup;
    }
}