using System.Windows.Forms;

namespace BatteryNotifier.Forms
{
    partial class Dashboard
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Dashboard));
            this.BatteryNotifierIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBox4 = new System.Windows.Forms.PictureBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.AppContainer = new System.Windows.Forms.TableLayoutPanel();
            this.AppFooter = new System.Windows.Forms.Panel();
            this.VersionLabel = new System.Windows.Forms.Label();
            this.NotificationText = new System.Windows.Forms.Label();
            this.AppHeader = new System.Windows.Forms.TableLayoutPanel();
            this.AppHeaderTitle = new System.Windows.Forms.Label();
            this.CloseIcon = new System.Windows.Forms.PictureBox();
            this.AppTabControl = new BatteryNotifier.CustomControls.FlatTabControl.FlatTabControl();
            this.DashboardTab = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.NotificationSettingGroupBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel12 = new System.Windows.Forms.TableLayoutPanel();
            this.LowBatteryIcon = new System.Windows.Forms.PictureBox();
            this.LowBatteryNotificationCheckbox = new System.Windows.Forms.CheckBox();
            this.LowBatteryLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel13 = new System.Windows.Forms.TableLayoutPanel();
            this.FullBatteryIcon = new System.Windows.Forms.PictureBox();
            this.FullBatteryNotificationCheckbox = new System.Windows.Forms.CheckBox();
            this.FullBatteryLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.BatteryImage = new System.Windows.Forms.PictureBox();
            this.BatteryPercentage = new System.Windows.Forms.Label();
            this.BatteryStatus = new System.Windows.Forms.Label();
            this.RemainingTime = new System.Windows.Forms.Label();
            this.SettingTab = new System.Windows.Forms.TabPage();
            this.NotificationGroupBox = new System.Windows.Forms.GroupBox();
            this.lowBatteryPercentageValue = new System.Windows.Forms.NumericUpDown();
            this.pictureBox8 = new System.Windows.Forms.PictureBox();
            this.fullbatteryPercentageValue = new System.Windows.Forms.NumericUpDown();
            this.lowBatteryTrackbar = new System.Windows.Forms.TrackBar();
            this.LowBatteryPercentageLabel = new System.Windows.Forms.Label();
            this.pictureBox9 = new System.Windows.Forms.PictureBox();
            this.ShowFullBatteryNotificationLabel = new System.Windows.Forms.Label();
            this.LowBatteryNotificationLabel = new System.Windows.Forms.Label();
            this.fullBatteryTrackbar = new System.Windows.Forms.TrackBar();
            this.FullBatteryPercentageLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.ShowAsWindowPanel = new System.Windows.Forms.Panel();
            this.PinToNotificationAreaPictureBox = new System.Windows.Forms.PictureBox();
            this.PinToNotificationAreaLabel = new System.Windows.Forms.Label();
            this.PinToNotificationArea = new System.Windows.Forms.CheckBox();
            this.LaunchAtStartupPanel = new System.Windows.Forms.Panel();
            this.LaunchAtStartUpPictureBox = new System.Windows.Forms.PictureBox();
            this.LaunchAtStartUpLabel = new System.Windows.Forms.Label();
            this.launchAtStartup = new System.Windows.Forms.CheckBox();
            this.ThemeConfigurationPanel = new System.Windows.Forms.TableLayoutPanel();
            this.DarkThemeLabel = new System.Windows.Forms.RadioButton();
            this.ThemePanel = new System.Windows.Forms.Panel();
            this.ThemePictureBox = new System.Windows.Forms.PictureBox();
            this.ThemeLabel = new System.Windows.Forms.Label();
            this.SystemThemeLabel = new System.Windows.Forms.RadioButton();
            this.LightThemeLabel = new System.Windows.Forms.RadioButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.BatteryIcon = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.BatteryPercentageLabel = new System.Windows.Forms.Label();
            this.BatteryStatusLabel = new System.Windows.Forms.Label();
            this.BatteryDetail = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.Notification = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBox5 = new System.Windows.Forms.PictureBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.pictureBox6 = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.BatteryStatusTimer = new System.Windows.Forms.Timer(this.components);
            this.ShowNotificationTimer = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.tableLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
            this.AppContainer.SuspendLayout();
            this.AppFooter.SuspendLayout();
            this.AppHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).BeginInit();
            this.AppTabControl.SuspendLayout();
            this.DashboardTab.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.NotificationSettingGroupBox.SuspendLayout();
            this.tableLayoutPanel12.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryIcon)).BeginInit();
            this.tableLayoutPanel13.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BatteryImage)).BeginInit();
            this.SettingTab.SuspendLayout();
            this.NotificationGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryPercentageValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox8)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullbatteryPercentageValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryTrackbar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox9)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullBatteryTrackbar)).BeginInit();
            this.flowLayoutPanel2.SuspendLayout();
            this.ShowAsWindowPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PinToNotificationAreaPictureBox)).BeginInit();
            this.LaunchAtStartupPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LaunchAtStartUpPictureBox)).BeginInit();
            this.ThemeConfigurationPanel.SuspendLayout();
            this.ThemePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ThemePictureBox)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BatteryIcon)).BeginInit();
            this.tableLayoutPanel3.SuspendLayout();
            this.Notification.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).BeginInit();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // BatteryNotifierIcon
            // 
            this.BatteryNotifierIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.BatteryNotifierIcon.BalloonTipText = " Hi there";
            this.BatteryNotifierIcon.BalloonTipTitle = " and this is the title";
            this.BatteryNotifierIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("BatteryNotifierIcon.Icon")));
            this.BatteryNotifierIcon.Text = "Battery Notifier";
            this.BatteryNotifierIcon.Visible = true;
            this.BatteryNotifierIcon.BalloonTipClicked += new System.EventHandler(this.BatteryNotifierIcon_BalloonTipClicked);
            this.BatteryNotifierIcon.BalloonTipClosed += new System.EventHandler(this.BatteryNotifierIcon_BalloonTipClosed);
            this.BatteryNotifierIcon.Click += new System.EventHandler(this.BatteryNotifierIcon_Click);
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 4;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 71F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel5.Controls.Add(this.pictureBox2, 0, 0);
            this.tableLayoutPanel5.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel5.TabIndex = 0;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox2.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.pictureBox2.Location = new System.Drawing.Point(3, 3);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(65, 94);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 5;
            this.pictureBox2.TabStop = false;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBox2.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.checkBox2.Location = new System.Drawing.Point(97, 3);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(60, 94);
            this.checkBox2.TabIndex = 6;
            this.checkBox2.Text = "Off";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 4;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 71F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel6.Controls.Add(this.pictureBox4, 0, 0);
            this.tableLayoutPanel6.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 1;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel6.TabIndex = 0;
            // 
            // pictureBox4
            // 
            this.pictureBox4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox4.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.pictureBox4.Location = new System.Drawing.Point(3, 3);
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.Size = new System.Drawing.Size(65, 94);
            this.pictureBox4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox4.TabIndex = 5;
            this.pictureBox4.TabStop = false;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBox3.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.checkBox3.Location = new System.Drawing.Point(97, 3);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(60, 94);
            this.checkBox3.TabIndex = 6;
            this.checkBox3.Text = "Off";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // AppContainer
            // 
            this.AppContainer.BackColor = System.Drawing.Color.Transparent;
            this.AppContainer.ColumnCount = 1;
            this.AppContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AppContainer.Controls.Add(this.AppFooter, 0, 2);
            this.AppContainer.Controls.Add(this.AppHeader, 0, 0);
            this.AppContainer.Controls.Add(this.AppTabControl, 0, 1);
            this.AppContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppContainer.Location = new System.Drawing.Point(1, 1);
            this.AppContainer.Margin = new System.Windows.Forms.Padding(0);
            this.AppContainer.Name = "AppContainer";
            this.AppContainer.RowCount = 3;
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.04274F));
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 89.95727F));
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.AppContainer.Size = new System.Drawing.Size(408, 508);
            this.AppContainer.TabIndex = 6;
            // 
            // AppFooter
            // 
            this.AppFooter.BackColor = System.Drawing.Color.AliceBlue;
            this.AppFooter.Controls.Add(this.VersionLabel);
            this.AppFooter.Controls.Add(this.NotificationText);
            this.AppFooter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppFooter.ForeColor = System.Drawing.Color.Crimson;
            this.AppFooter.Location = new System.Drawing.Point(0, 468);
            this.AppFooter.Margin = new System.Windows.Forms.Padding(0);
            this.AppFooter.Name = "AppFooter";
            this.AppFooter.Size = new System.Drawing.Size(408, 40);
            this.AppFooter.TabIndex = 8;
            // 
            // VersionLabel
            // 
            this.VersionLabel.AutoSize = true;
            this.VersionLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.VersionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.VersionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.VersionLabel.Location = new System.Drawing.Point(334, 14);
            this.VersionLabel.Name = "VersionLabel";
            this.VersionLabel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.VersionLabel.Size = new System.Drawing.Size(63, 16);
            this.VersionLabel.TabIndex = 23;
            this.VersionLabel.Text = "v 1.0.0.0";
            // 
            // NotificationText
            // 
            this.NotificationText.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.NotificationText.Location = new System.Drawing.Point(9, 14);
            this.NotificationText.Name = "NotificationText";
            this.NotificationText.Size = new System.Drawing.Size(272, 16);
            this.NotificationText.TabIndex = 17;
            this.NotificationText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // AppHeader
            // 
            this.AppHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(19)))), ((int)(((byte)(20)))));
            this.AppHeader.ColumnCount = 2;
            this.AppHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AppHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.AppHeader.Controls.Add(this.AppHeaderTitle, 0, 0);
            this.AppHeader.Controls.Add(this.CloseIcon, 1, 0);
            this.AppHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppHeader.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.AppHeader.Location = new System.Drawing.Point(0, 0);
            this.AppHeader.Margin = new System.Windows.Forms.Padding(0);
            this.AppHeader.Name = "AppHeader";
            this.AppHeader.RowCount = 1;
            this.AppHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AppHeader.Size = new System.Drawing.Size(408, 47);
            this.AppHeader.TabIndex = 7;
            // 
            // AppHeaderTitle
            // 
            this.AppHeaderTitle.AutoSize = true;
            this.AppHeaderTitle.BackColor = System.Drawing.Color.Transparent;
            this.AppHeaderTitle.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.AppHeaderTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppHeaderTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.AppHeaderTitle.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.AppHeaderTitle.Location = new System.Drawing.Point(3, 0);
            this.AppHeaderTitle.Name = "AppHeaderTitle";
            this.AppHeaderTitle.Size = new System.Drawing.Size(358, 47);
            this.AppHeaderTitle.TabIndex = 16;
            this.AppHeaderTitle.Text = "Battery Notifier";
            this.AppHeaderTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // CloseIcon
            // 
            this.CloseIcon.BackColor = System.Drawing.Color.Transparent;
            this.CloseIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.CloseIcon.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CloseIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CloseIcon.Image = global::BatteryNotifier.Properties.Resources.closeIconDark;
            this.CloseIcon.Location = new System.Drawing.Point(364, 0);
            this.CloseIcon.Margin = new System.Windows.Forms.Padding(0);
            this.CloseIcon.Name = "CloseIcon";
            this.CloseIcon.Size = new System.Drawing.Size(44, 47);
            this.CloseIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.CloseIcon.TabIndex = 0;
            this.CloseIcon.TabStop = false;
            // 
            // AppTabControl
            // 
            this.AppTabControl.Controls.Add(this.DashboardTab);
            this.AppTabControl.Controls.Add(this.SettingTab);
            this.AppTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppTabControl.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.AppTabControl.Location = new System.Drawing.Point(0, 47);
            this.AppTabControl.Margin = new System.Windows.Forms.Padding(0);
            this.AppTabControl.MyBackColor = System.Drawing.Color.Transparent;
            this.AppTabControl.MyBorderColor = System.Drawing.SystemColors.ControlText;
            this.AppTabControl.Name = "AppTabControl";
            this.AppTabControl.SelectedIndex = 0;
            this.AppTabControl.ShowToolTips = true;
            this.AppTabControl.Size = new System.Drawing.Size(408, 421);
            this.AppTabControl.TabIndex = 9;
            // 
            // DashboardTab
            // 
            this.DashboardTab.BackColor = System.Drawing.Color.Transparent;
            this.DashboardTab.Controls.Add(this.tableLayoutPanel9);
            this.DashboardTab.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DashboardTab.Location = new System.Drawing.Point(4, 29);
            this.DashboardTab.Margin = new System.Windows.Forms.Padding(0);
            this.DashboardTab.Name = "DashboardTab";
            this.DashboardTab.Size = new System.Drawing.Size(400, 388);
            this.DashboardTab.TabIndex = 0;
            this.DashboardTab.Text = "Dashboard";
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.ColumnCount = 1;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel9.Controls.Add(this.NotificationSettingGroupBox, 0, 1);
            this.tableLayoutPanel9.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel9.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel9.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 2;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 240F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tableLayoutPanel9.Size = new System.Drawing.Size(400, 388);
            this.tableLayoutPanel9.TabIndex = 7;
            // 
            // NotificationSettingGroupBox
            // 
            this.NotificationSettingGroupBox.Controls.Add(this.tableLayoutPanel12);
            this.NotificationSettingGroupBox.Controls.Add(this.tableLayoutPanel13);
            this.NotificationSettingGroupBox.Font = new System.Drawing.Font("Roboto", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.NotificationSettingGroupBox.Location = new System.Drawing.Point(8, 248);
            this.NotificationSettingGroupBox.Margin = new System.Windows.Forms.Padding(8);
            this.NotificationSettingGroupBox.Name = "NotificationSettingGroupBox";
            this.NotificationSettingGroupBox.Size = new System.Drawing.Size(384, 126);
            this.NotificationSettingGroupBox.TabIndex = 5;
            this.NotificationSettingGroupBox.TabStop = false;
            this.NotificationSettingGroupBox.Text = "Notification Setting";
            // 
            // tableLayoutPanel12
            // 
            this.tableLayoutPanel12.ColumnCount = 3;
            this.tableLayoutPanel12.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.tableLayoutPanel12.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel12.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel12.Controls.Add(this.LowBatteryIcon, 0, 0);
            this.tableLayoutPanel12.Controls.Add(this.LowBatteryNotificationCheckbox, 2, 0);
            this.tableLayoutPanel12.Controls.Add(this.LowBatteryLabel, 1, 0);
            this.tableLayoutPanel12.Location = new System.Drawing.Point(4, 77);
            this.tableLayoutPanel12.Name = "tableLayoutPanel12";
            this.tableLayoutPanel12.RowCount = 1;
            this.tableLayoutPanel12.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel12.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tableLayoutPanel12.Size = new System.Drawing.Size(376, 42);
            this.tableLayoutPanel12.TabIndex = 8;
            // 
            // LowBatteryIcon
            // 
            this.LowBatteryIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.LowBatteryIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LowBatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Low;
            this.LowBatteryIcon.Location = new System.Drawing.Point(0, 0);
            this.LowBatteryIcon.Margin = new System.Windows.Forms.Padding(0);
            this.LowBatteryIcon.Name = "LowBatteryIcon";
            this.LowBatteryIcon.Size = new System.Drawing.Size(56, 42);
            this.LowBatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.LowBatteryIcon.TabIndex = 5;
            this.LowBatteryIcon.TabStop = false;
            // 
            // LowBatteryNotificationCheckbox
            // 
            this.LowBatteryNotificationCheckbox.AutoSize = true;
            this.LowBatteryNotificationCheckbox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.LowBatteryNotificationCheckbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LowBatteryNotificationCheckbox.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LowBatteryNotificationCheckbox.Location = new System.Drawing.Point(313, 3);
            this.LowBatteryNotificationCheckbox.Name = "LowBatteryNotificationCheckbox";
            this.LowBatteryNotificationCheckbox.Size = new System.Drawing.Size(60, 36);
            this.LowBatteryNotificationCheckbox.TabIndex = 6;
            this.LowBatteryNotificationCheckbox.Text = "Off";
            this.LowBatteryNotificationCheckbox.UseVisualStyleBackColor = true;
            // 
            // LowBatteryLabel
            // 
            this.LowBatteryLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.LowBatteryLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LowBatteryLabel.Location = new System.Drawing.Point(59, 0);
            this.LowBatteryLabel.Name = "LowBatteryLabel";
            this.LowBatteryLabel.Size = new System.Drawing.Size(248, 42);
            this.LowBatteryLabel.TabIndex = 5;
            this.LowBatteryLabel.Text = "Low Battery Notification";
            this.LowBatteryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel13
            // 
            this.tableLayoutPanel13.ColumnCount = 3;
            this.tableLayoutPanel13.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 55F));
            this.tableLayoutPanel13.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel13.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel13.Controls.Add(this.FullBatteryIcon, 0, 0);
            this.tableLayoutPanel13.Controls.Add(this.FullBatteryNotificationCheckbox, 2, 0);
            this.tableLayoutPanel13.Controls.Add(this.FullBatteryLabel, 1, 0);
            this.tableLayoutPanel13.Location = new System.Drawing.Point(4, 29);
            this.tableLayoutPanel13.Name = "tableLayoutPanel13";
            this.tableLayoutPanel13.RowCount = 1;
            this.tableLayoutPanel13.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel13.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tableLayoutPanel13.Size = new System.Drawing.Size(376, 42);
            this.tableLayoutPanel13.TabIndex = 7;
            // 
            // FullBatteryIcon
            // 
            this.FullBatteryIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.FullBatteryIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FullBatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.FullBatteryIcon.Location = new System.Drawing.Point(0, 0);
            this.FullBatteryIcon.Margin = new System.Windows.Forms.Padding(0);
            this.FullBatteryIcon.Name = "FullBatteryIcon";
            this.FullBatteryIcon.Size = new System.Drawing.Size(55, 42);
            this.FullBatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.FullBatteryIcon.TabIndex = 5;
            this.FullBatteryIcon.TabStop = false;
            // 
            // FullBatteryNotificationCheckbox
            // 
            this.FullBatteryNotificationCheckbox.AutoSize = true;
            this.FullBatteryNotificationCheckbox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.FullBatteryNotificationCheckbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FullBatteryNotificationCheckbox.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FullBatteryNotificationCheckbox.Location = new System.Drawing.Point(313, 3);
            this.FullBatteryNotificationCheckbox.Name = "FullBatteryNotificationCheckbox";
            this.FullBatteryNotificationCheckbox.Size = new System.Drawing.Size(60, 36);
            this.FullBatteryNotificationCheckbox.TabIndex = 6;
            this.FullBatteryNotificationCheckbox.Text = "Off";
            this.FullBatteryNotificationCheckbox.UseVisualStyleBackColor = true;
            // 
            // FullBatteryLabel
            // 
            this.FullBatteryLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FullBatteryLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FullBatteryLabel.Location = new System.Drawing.Point(58, 0);
            this.FullBatteryLabel.Name = "FullBatteryLabel";
            this.FullBatteryLabel.Size = new System.Drawing.Size(249, 42);
            this.FullBatteryLabel.TabIndex = 5;
            this.FullBatteryLabel.Text = "Full Battery Notification";
            this.FullBatteryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.BatteryImage);
            this.flowLayoutPanel1.Controls.Add(this.BatteryPercentage);
            this.flowLayoutPanel1.Controls.Add(this.BatteryStatus);
            this.flowLayoutPanel1.Controls.Add(this.RemainingTime);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(400, 240);
            this.flowLayoutPanel1.TabIndex = 6;
            // 
            // BatteryImage
            // 
            this.BatteryImage.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.BatteryImage.Location = new System.Drawing.Point(0, 0);
            this.BatteryImage.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryImage.Name = "BatteryImage";
            this.BatteryImage.Size = new System.Drawing.Size(402, 126);
            this.BatteryImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.BatteryImage.TabIndex = 23;
            this.BatteryImage.TabStop = false;
            // 
            // BatteryPercentage
            // 
            this.BatteryPercentage.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.BatteryPercentage.Location = new System.Drawing.Point(0, 126);
            this.BatteryPercentage.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryPercentage.Name = "BatteryPercentage";
            this.BatteryPercentage.Size = new System.Drawing.Size(391, 54);
            this.BatteryPercentage.TabIndex = 22;
            this.BatteryPercentage.Text = "0%";
            this.BatteryPercentage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BatteryStatus
            // 
            this.BatteryStatus.AutoEllipsis = true;
            this.BatteryStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.BatteryStatus.ForeColor = System.Drawing.Color.Gray;
            this.BatteryStatus.Location = new System.Drawing.Point(0, 180);
            this.BatteryStatus.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryStatus.Name = "BatteryStatus";
            this.BatteryStatus.Size = new System.Drawing.Size(391, 28);
            this.BatteryStatus.TabIndex = 24;
            this.BatteryStatus.Text = "Charging status";
            this.BatteryStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // RemainingTime
            // 
            this.RemainingTime.AutoEllipsis = true;
            this.RemainingTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.RemainingTime.Location = new System.Drawing.Point(3, 208);
            this.RemainingTime.Name = "RemainingTime";
            this.RemainingTime.Size = new System.Drawing.Size(388, 36);
            this.RemainingTime.TabIndex = 25;
            this.RemainingTime.Text = "2 Hour 15  minutes";
            this.RemainingTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SettingTab
            // 
            this.SettingTab.BackColor = System.Drawing.Color.Transparent;
            this.SettingTab.Controls.Add(this.NotificationGroupBox);
            this.SettingTab.Controls.Add(this.flowLayoutPanel2);
            this.SettingTab.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SettingTab.Location = new System.Drawing.Point(4, 29);
            this.SettingTab.Margin = new System.Windows.Forms.Padding(0);
            this.SettingTab.Name = "SettingTab";
            this.SettingTab.Padding = new System.Windows.Forms.Padding(8);
            this.SettingTab.Size = new System.Drawing.Size(400, 388);
            this.SettingTab.TabIndex = 1;
            this.SettingTab.Text = "Setting";
            // 
            // NotificationGroupBox
            // 
            this.NotificationGroupBox.BackColor = System.Drawing.Color.Transparent;
            this.NotificationGroupBox.Controls.Add(this.lowBatteryPercentageValue);
            this.NotificationGroupBox.Controls.Add(this.pictureBox8);
            this.NotificationGroupBox.Controls.Add(this.fullbatteryPercentageValue);
            this.NotificationGroupBox.Controls.Add(this.lowBatteryTrackbar);
            this.NotificationGroupBox.Controls.Add(this.LowBatteryPercentageLabel);
            this.NotificationGroupBox.Controls.Add(this.pictureBox9);
            this.NotificationGroupBox.Controls.Add(this.ShowFullBatteryNotificationLabel);
            this.NotificationGroupBox.Controls.Add(this.LowBatteryNotificationLabel);
            this.NotificationGroupBox.Controls.Add(this.fullBatteryTrackbar);
            this.NotificationGroupBox.Controls.Add(this.FullBatteryPercentageLabel);
            this.NotificationGroupBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.NotificationGroupBox.Location = new System.Drawing.Point(8, 152);
            this.NotificationGroupBox.Margin = new System.Windows.Forms.Padding(0);
            this.NotificationGroupBox.Name = "NotificationGroupBox";
            this.NotificationGroupBox.Size = new System.Drawing.Size(384, 228);
            this.NotificationGroupBox.TabIndex = 35;
            this.NotificationGroupBox.TabStop = false;
            this.NotificationGroupBox.Text = "Notification Setting for";
            // 
            // lowBatteryPercentageValue
            // 
            this.lowBatteryPercentageValue.Enabled = false;
            this.lowBatteryPercentageValue.Location = new System.Drawing.Point(72, 161);
            this.lowBatteryPercentageValue.Name = "lowBatteryPercentageValue";
            this.lowBatteryPercentageValue.ReadOnly = true;
            this.lowBatteryPercentageValue.Size = new System.Drawing.Size(62, 27);
            this.lowBatteryPercentageValue.TabIndex = 25;
            // 
            // pictureBox8
            // 
            this.pictureBox8.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox8.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox8.Image = global::BatteryNotifier.Properties.Resources.Low;
            this.pictureBox8.Location = new System.Drawing.Point(4, 138);
            this.pictureBox8.Name = "pictureBox8";
            this.pictureBox8.Size = new System.Drawing.Size(56, 50);
            this.pictureBox8.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox8.TabIndex = 26;
            this.pictureBox8.TabStop = false;
            // 
            // fullbatteryPercentageValue
            // 
            this.fullbatteryPercentageValue.Enabled = false;
            this.fullbatteryPercentageValue.Location = new System.Drawing.Point(70, 80);
            this.fullbatteryPercentageValue.Name = "fullbatteryPercentageValue";
            this.fullbatteryPercentageValue.ReadOnly = true;
            this.fullbatteryPercentageValue.Size = new System.Drawing.Size(62, 27);
            this.fullbatteryPercentageValue.TabIndex = 24;
            // 
            // lowBatteryTrackbar
            // 
            this.lowBatteryTrackbar.Cursor = System.Windows.Forms.Cursors.SizeWE;
            this.lowBatteryTrackbar.LargeChange = 10;
            this.lowBatteryTrackbar.Location = new System.Drawing.Point(162, 138);
            this.lowBatteryTrackbar.Maximum = 100;
            this.lowBatteryTrackbar.Name = "lowBatteryTrackbar";
            this.lowBatteryTrackbar.Size = new System.Drawing.Size(219, 45);
            this.lowBatteryTrackbar.TabIndex = 20;
            // 
            // LowBatteryPercentageLabel
            // 
            this.LowBatteryPercentageLabel.AutoSize = true;
            this.LowBatteryPercentageLabel.BackColor = System.Drawing.Color.Transparent;
            this.LowBatteryPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LowBatteryPercentageLabel.Location = new System.Drawing.Point(136, 163);
            this.LowBatteryPercentageLabel.Name = "LowBatteryPercentageLabel";
            this.LowBatteryPercentageLabel.Size = new System.Drawing.Size(23, 20);
            this.LowBatteryPercentageLabel.TabIndex = 19;
            this.LowBatteryPercentageLabel.Text = "%";
            // 
            // pictureBox9
            // 
            this.pictureBox9.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox9.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox9.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.pictureBox9.Location = new System.Drawing.Point(6, 57);
            this.pictureBox9.Name = "pictureBox9";
            this.pictureBox9.Size = new System.Drawing.Size(55, 50);
            this.pictureBox9.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox9.TabIndex = 25;
            this.pictureBox9.TabStop = false;
            // 
            // ShowFullBatteryNotificationLabel
            // 
            this.ShowFullBatteryNotificationLabel.AutoSize = true;
            this.ShowFullBatteryNotificationLabel.BackColor = System.Drawing.Color.Transparent;
            this.ShowFullBatteryNotificationLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ShowFullBatteryNotificationLabel.Location = new System.Drawing.Point(67, 57);
            this.ShowFullBatteryNotificationLabel.Name = "ShowFullBatteryNotificationLabel";
            this.ShowFullBatteryNotificationLabel.Size = new System.Drawing.Size(89, 20);
            this.ShowFullBatteryNotificationLabel.TabIndex = 18;
            this.ShowFullBatteryNotificationLabel.Text = "Full Battery";
            // 
            // LowBatteryNotificationLabel
            // 
            this.LowBatteryNotificationLabel.AutoSize = true;
            this.LowBatteryNotificationLabel.BackColor = System.Drawing.Color.Transparent;
            this.LowBatteryNotificationLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LowBatteryNotificationLabel.Location = new System.Drawing.Point(66, 138);
            this.LowBatteryNotificationLabel.Name = "LowBatteryNotificationLabel";
            this.LowBatteryNotificationLabel.Size = new System.Drawing.Size(93, 20);
            this.LowBatteryNotificationLabel.TabIndex = 18;
            this.LowBatteryNotificationLabel.Text = "Low Battery";
            // 
            // fullBatteryTrackbar
            // 
            this.fullBatteryTrackbar.Cursor = System.Windows.Forms.Cursors.SizeWE;
            this.fullBatteryTrackbar.LargeChange = 10;
            this.fullBatteryTrackbar.Location = new System.Drawing.Point(162, 57);
            this.fullBatteryTrackbar.Maximum = 100;
            this.fullBatteryTrackbar.Name = "fullBatteryTrackbar";
            this.fullBatteryTrackbar.Size = new System.Drawing.Size(219, 45);
            this.fullBatteryTrackbar.TabIndex = 20;
            // 
            // FullBatteryPercentageLabel
            // 
            this.FullBatteryPercentageLabel.AutoSize = true;
            this.FullBatteryPercentageLabel.BackColor = System.Drawing.Color.Transparent;
            this.FullBatteryPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FullBatteryPercentageLabel.Location = new System.Drawing.Point(133, 84);
            this.FullBatteryPercentageLabel.Name = "FullBatteryPercentageLabel";
            this.FullBatteryPercentageLabel.Size = new System.Drawing.Size(23, 20);
            this.FullBatteryPercentageLabel.TabIndex = 19;
            this.FullBatteryPercentageLabel.Text = "%";
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.ShowAsWindowPanel);
            this.flowLayoutPanel2.Controls.Add(this.LaunchAtStartupPanel);
            this.flowLayoutPanel2.Controls.Add(this.ThemeConfigurationPanel);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(8, 8);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(384, 135);
            this.flowLayoutPanel2.TabIndex = 34;
            // 
            // ShowAsWindowPanel
            // 
            this.ShowAsWindowPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ShowAsWindowPanel.Controls.Add(this.PinToNotificationAreaPictureBox);
            this.ShowAsWindowPanel.Controls.Add(this.PinToNotificationAreaLabel);
            this.ShowAsWindowPanel.Controls.Add(this.PinToNotificationArea);
            this.ShowAsWindowPanel.Location = new System.Drawing.Point(4, 4);
            this.ShowAsWindowPanel.Margin = new System.Windows.Forms.Padding(4);
            this.ShowAsWindowPanel.Name = "ShowAsWindowPanel";
            this.ShowAsWindowPanel.Size = new System.Drawing.Size(376, 36);
            this.ShowAsWindowPanel.TabIndex = 30;
            // 
            // PinToNotificationAreaPictureBox
            // 
            this.PinToNotificationAreaPictureBox.BackColor = System.Drawing.Color.Transparent;
            this.PinToNotificationAreaPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.PinToNotificationAreaPictureBox.Image = global::BatteryNotifier.Properties.Resources.Window;
            this.PinToNotificationAreaPictureBox.Location = new System.Drawing.Point(0, 0);
            this.PinToNotificationAreaPictureBox.Name = "PinToNotificationAreaPictureBox";
            this.PinToNotificationAreaPictureBox.Size = new System.Drawing.Size(35, 36);
            this.PinToNotificationAreaPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PinToNotificationAreaPictureBox.TabIndex = 27;
            this.PinToNotificationAreaPictureBox.TabStop = false;
            // 
            // PinToNotificationAreaLabel
            // 
            this.PinToNotificationAreaLabel.AutoSize = true;
            this.PinToNotificationAreaLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.PinToNotificationAreaLabel.Location = new System.Drawing.Point(37, 7);
            this.PinToNotificationAreaLabel.Name = "PinToNotificationAreaLabel";
            this.PinToNotificationAreaLabel.Size = new System.Drawing.Size(170, 20);
            this.PinToNotificationAreaLabel.TabIndex = 16;
            this.PinToNotificationAreaLabel.Text = "Pin to Notification Area";
            this.PinToNotificationAreaLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // PinToNotificationArea
            // 
            this.PinToNotificationArea.AutoSize = true;
            this.PinToNotificationArea.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PinToNotificationArea.Location = new System.Drawing.Point(351, 12);
            this.PinToNotificationArea.Name = "PinToNotificationArea";
            this.PinToNotificationArea.Size = new System.Drawing.Size(15, 14);
            this.PinToNotificationArea.TabIndex = 17;
            this.PinToNotificationArea.UseVisualStyleBackColor = true;
            // 
            // LaunchAtStartupPanel
            // 
            this.LaunchAtStartupPanel.BackColor = System.Drawing.SystemColors.Menu;
            this.LaunchAtStartupPanel.Controls.Add(this.LaunchAtStartUpPictureBox);
            this.LaunchAtStartupPanel.Controls.Add(this.LaunchAtStartUpLabel);
            this.LaunchAtStartupPanel.Controls.Add(this.launchAtStartup);
            this.LaunchAtStartupPanel.Location = new System.Drawing.Point(4, 48);
            this.LaunchAtStartupPanel.Margin = new System.Windows.Forms.Padding(4);
            this.LaunchAtStartupPanel.Name = "LaunchAtStartupPanel";
            this.LaunchAtStartupPanel.Size = new System.Drawing.Size(376, 36);
            this.LaunchAtStartupPanel.TabIndex = 33;
            // 
            // LaunchAtStartUpPictureBox
            // 
            this.LaunchAtStartUpPictureBox.BackColor = System.Drawing.Color.Transparent;
            this.LaunchAtStartUpPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.LaunchAtStartUpPictureBox.Image = global::BatteryNotifier.Properties.Resources.launchatstartup;
            this.LaunchAtStartUpPictureBox.Location = new System.Drawing.Point(1, -2);
            this.LaunchAtStartUpPictureBox.Name = "LaunchAtStartUpPictureBox";
            this.LaunchAtStartUpPictureBox.Size = new System.Drawing.Size(34, 38);
            this.LaunchAtStartUpPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.LaunchAtStartUpPictureBox.TabIndex = 28;
            this.LaunchAtStartUpPictureBox.TabStop = false;
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
            this.launchAtStartup.Cursor = System.Windows.Forms.Cursors.Hand;
            this.launchAtStartup.Location = new System.Drawing.Point(351, 11);
            this.launchAtStartup.Name = "launchAtStartup";
            this.launchAtStartup.Size = new System.Drawing.Size(15, 14);
            this.launchAtStartup.TabIndex = 17;
            this.launchAtStartup.UseVisualStyleBackColor = true;
            // 
            // ThemeConfigurationPanel
            // 
            this.ThemeConfigurationPanel.BackColor = System.Drawing.SystemColors.Menu;
            this.ThemeConfigurationPanel.ColumnCount = 4;
            this.ThemeConfigurationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 103F));
            this.ThemeConfigurationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.ThemeConfigurationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.ThemeConfigurationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.ThemeConfigurationPanel.Controls.Add(this.DarkThemeLabel, 2, 0);
            this.ThemeConfigurationPanel.Controls.Add(this.ThemePanel, 0, 0);
            this.ThemeConfigurationPanel.Controls.Add(this.SystemThemeLabel, 1, 0);
            this.ThemeConfigurationPanel.Controls.Add(this.LightThemeLabel, 1, 0);
            this.ThemeConfigurationPanel.Location = new System.Drawing.Point(4, 92);
            this.ThemeConfigurationPanel.Margin = new System.Windows.Forms.Padding(4);
            this.ThemeConfigurationPanel.Name = "ThemeConfigurationPanel";
            this.ThemeConfigurationPanel.RowCount = 1;
            this.ThemeConfigurationPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ThemeConfigurationPanel.Size = new System.Drawing.Size(376, 36);
            this.ThemeConfigurationPanel.TabIndex = 35;
            // 
            // DarkThemeLabel
            // 
            this.DarkThemeLabel.BackColor = System.Drawing.Color.Transparent;
            this.DarkThemeLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DarkThemeLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.DarkThemeLabel.Location = new System.Drawing.Point(301, 3);
            this.DarkThemeLabel.Name = "DarkThemeLabel";
            this.DarkThemeLabel.Size = new System.Drawing.Size(72, 30);
            this.DarkThemeLabel.TabIndex = 38;
            this.DarkThemeLabel.TabStop = true;
            this.DarkThemeLabel.Text = "Dark";
            this.DarkThemeLabel.UseVisualStyleBackColor = false;
            // 
            // ThemePanel
            // 
            this.ThemePanel.BackColor = System.Drawing.SystemColors.Menu;
            this.ThemePanel.Controls.Add(this.ThemePictureBox);
            this.ThemePanel.Controls.Add(this.ThemeLabel);
            this.ThemePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ThemePanel.Location = new System.Drawing.Point(0, 0);
            this.ThemePanel.Margin = new System.Windows.Forms.Padding(0);
            this.ThemePanel.Name = "ThemePanel";
            this.ThemePanel.Size = new System.Drawing.Size(103, 36);
            this.ThemePanel.TabIndex = 33;
            // 
            // ThemePictureBox
            // 
            this.ThemePictureBox.BackColor = System.Drawing.Color.Transparent;
            this.ThemePictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ThemePictureBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.ThemePictureBox.Image = global::BatteryNotifier.Properties.Resources.DarkMode;
            this.ThemePictureBox.Location = new System.Drawing.Point(0, 0);
            this.ThemePictureBox.Name = "ThemePictureBox";
            this.ThemePictureBox.Size = new System.Drawing.Size(37, 36);
            this.ThemePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ThemePictureBox.TabIndex = 28;
            this.ThemePictureBox.TabStop = false;
            // 
            // ThemeLabel
            // 
            this.ThemeLabel.BackColor = System.Drawing.Color.Transparent;
            this.ThemeLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.ThemeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ThemeLabel.Location = new System.Drawing.Point(24, 0);
            this.ThemeLabel.Margin = new System.Windows.Forms.Padding(0);
            this.ThemeLabel.Name = "ThemeLabel";
            this.ThemeLabel.Size = new System.Drawing.Size(79, 36);
            this.ThemeLabel.TabIndex = 16;
            this.ThemeLabel.Text = "Theme :";
            this.ThemeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // SystemThemeLabel
            // 
            this.SystemThemeLabel.BackColor = System.Drawing.Color.Transparent;
            this.SystemThemeLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SystemThemeLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.SystemThemeLabel.Location = new System.Drawing.Point(110, 3);
            this.SystemThemeLabel.Name = "SystemThemeLabel";
            this.SystemThemeLabel.Size = new System.Drawing.Size(81, 30);
            this.SystemThemeLabel.TabIndex = 37;
            this.SystemThemeLabel.TabStop = true;
            this.SystemThemeLabel.Text = "System";
            this.SystemThemeLabel.UseVisualStyleBackColor = false;
            // 
            // LightThemeLabel
            // 
            this.LightThemeLabel.BackColor = System.Drawing.Color.Transparent;
            this.LightThemeLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.LightThemeLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.LightThemeLabel.Location = new System.Drawing.Point(211, 3);
            this.LightThemeLabel.Name = "LightThemeLabel";
            this.LightThemeLabel.Size = new System.Drawing.Size(71, 30);
            this.LightThemeLabel.TabIndex = 36;
            this.LightThemeLabel.TabStop = true;
            this.LightThemeLabel.Text = "Light";
            this.LightThemeLabel.UseVisualStyleBackColor = false;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.BatteryDetail, 0, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tableLayoutPanel2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 20);
            this.panel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.BatteryIcon, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(200, 20);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // BatteryIcon
            // 
            this.BatteryIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.BatteryIcon.Location = new System.Drawing.Point(0, 0);
            this.BatteryIcon.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryIcon.Name = "BatteryIcon";
            this.BatteryIcon.Size = new System.Drawing.Size(120, 20);
            this.BatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.BatteryIcon.TabIndex = 1;
            this.BatteryIcon.TabStop = false;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.BatteryPercentageLabel, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.BatteryStatusLabel, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(123, 3);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 59.57447F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40.42553F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(74, 14);
            this.tableLayoutPanel3.TabIndex = 2;
            // 
            // BatteryPercentageLabel
            // 
            this.BatteryPercentageLabel.AutoSize = true;
            this.BatteryPercentageLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BatteryPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.BatteryPercentageLabel.Location = new System.Drawing.Point(3, 0);
            this.BatteryPercentageLabel.Name = "BatteryPercentageLabel";
            this.BatteryPercentageLabel.Size = new System.Drawing.Size(68, 8);
            this.BatteryPercentageLabel.TabIndex = 0;
            this.BatteryPercentageLabel.Text = "0%";
            // 
            // BatteryStatusLabel
            // 
            this.BatteryStatusLabel.Location = new System.Drawing.Point(3, 8);
            this.BatteryStatusLabel.Name = "BatteryStatusLabel";
            this.BatteryStatusLabel.Size = new System.Drawing.Size(68, 6);
            this.BatteryStatusLabel.TabIndex = 1;
            // 
            // BatteryDetail
            // 
            this.BatteryDetail.Location = new System.Drawing.Point(3, 23);
            this.BatteryDetail.Name = "BatteryDetail";
            this.BatteryDetail.Size = new System.Drawing.Size(194, 74);
            this.BatteryDetail.TabIndex = 1;
            this.BatteryDetail.TabStop = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label7.Location = new System.Drawing.Point(148, 93);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(161, 20);
            this.label7.TabIndex = 19;
            this.label7.Text = "2 Hour 15  minutes";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(0, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(100, 23);
            this.label8.TabIndex = 0;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(0, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(100, 23);
            this.label5.TabIndex = 0;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(0, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(100, 23);
            this.label6.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(0, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 23);
            this.label4.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(0, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 23);
            this.label3.TabIndex = 0;
            // 
            // Notification
            // 
            this.Notification.Controls.Add(this.tableLayoutPanel7);
            this.Notification.Location = new System.Drawing.Point(0, 0);
            this.Notification.Name = "Notification";
            this.Notification.Size = new System.Drawing.Size(200, 100);
            this.Notification.TabIndex = 0;
            this.Notification.TabStop = false;
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 4;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 71F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel7.Controls.Add(this.pictureBox5, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.checkBox4, 2, 0);
            this.tableLayoutPanel7.Controls.Add(this.pictureBox6, 3, 0);
            this.tableLayoutPanel7.Controls.Add(this.label2, 1, 0);
            this.tableLayoutPanel7.Location = new System.Drawing.Point(6, 100);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 1;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(428, 42);
            this.tableLayoutPanel7.TabIndex = 8;
            // 
            // pictureBox5
            // 
            this.pictureBox5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox5.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.pictureBox5.Location = new System.Drawing.Point(3, 3);
            this.pictureBox5.Name = "pictureBox5";
            this.pictureBox5.Size = new System.Drawing.Size(65, 36);
            this.pictureBox5.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox5.TabIndex = 5;
            this.pictureBox5.TabStop = false;
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBox4.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.checkBox4.Location = new System.Drawing.Point(325, 3);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(60, 36);
            this.checkBox4.TabIndex = 6;
            this.checkBox4.Text = "Off";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // pictureBox6
            // 
            this.pictureBox6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox6.Location = new System.Drawing.Point(391, 3);
            this.pictureBox6.Name = "pictureBox6";
            this.pictureBox6.Size = new System.Drawing.Size(34, 36);
            this.pictureBox6.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox6.TabIndex = 12;
            this.pictureBox6.TabStop = false;
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label2.Location = new System.Drawing.Point(74, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(245, 42);
            this.label2.TabIndex = 5;
            this.label2.Text = "Low Battery Notification";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 4;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 71F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel4.Controls.Add(this.pictureBox1, 0, 0);
            this.tableLayoutPanel4.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 1;
            this.tableLayoutPanel4.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.pictureBox1.Location = new System.Drawing.Point(3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(65, 94);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBox1.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.checkBox1.Location = new System.Drawing.Point(97, 3);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(60, 94);
            this.checkBox1.TabIndex = 6;
            this.checkBox1.Text = "Off";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // Dashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.ClientSize = new System.Drawing.Size(410, 510);
            this.Controls.Add(this.AppContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(90, 90);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Dashboard";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Battery Notifier";
            this.Activated += new System.EventHandler(this.Dashboard_Activated);
            this.Load += new System.EventHandler(this.Dashboard_Load);
            this.tableLayoutPanel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.tableLayoutPanel6.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
            this.AppContainer.ResumeLayout(false);
            this.AppFooter.ResumeLayout(false);
            this.AppFooter.PerformLayout();
            this.AppHeader.ResumeLayout(false);
            this.AppHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).EndInit();
            this.AppTabControl.ResumeLayout(false);
            this.DashboardTab.ResumeLayout(false);
            this.tableLayoutPanel9.ResumeLayout(false);
            this.NotificationSettingGroupBox.ResumeLayout(false);
            this.tableLayoutPanel12.ResumeLayout(false);
            this.tableLayoutPanel12.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryIcon)).EndInit();
            this.tableLayoutPanel13.ResumeLayout(false);
            this.tableLayoutPanel13.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BatteryImage)).EndInit();
            this.SettingTab.ResumeLayout(false);
            this.NotificationGroupBox.ResumeLayout(false);
            this.NotificationGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryPercentageValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox8)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullbatteryPercentageValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryTrackbar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox9)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullBatteryTrackbar)).EndInit();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.ShowAsWindowPanel.ResumeLayout(false);
            this.ShowAsWindowPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PinToNotificationAreaPictureBox)).EndInit();
            this.LaunchAtStartupPanel.ResumeLayout(false);
            this.LaunchAtStartupPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LaunchAtStartUpPictureBox)).EndInit();
            this.ThemeConfigurationPanel.ResumeLayout(false);
            this.ThemePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ThemePictureBox)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BatteryIcon)).EndInit();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.Notification.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).EndInit();
            this.tableLayoutPanel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private NotifyIcon BatteryNotifierIcon;
        private TableLayoutPanel tableLayoutPanel5;
        private PictureBox pictureBox2;
        private CheckBox checkBox2;
        private TableLayoutPanel tableLayoutPanel6;
        private PictureBox pictureBox4;
        private CheckBox checkBox3;
        private TableLayoutPanel AppContainer;
        private TableLayoutPanel AppHeader;
        private Label AppHeaderTitle;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
        private TableLayoutPanel tableLayoutPanel2;
        private PictureBox BatteryIcon;
        private TableLayoutPanel tableLayoutPanel3;
        private Label BatteryPercentageLabel;
        private Label BatteryStatusLabel;
        private GroupBox BatteryDetail;
        private Label label7;
        private Label label8;
        private Label label5;
        private Label label6;
        private Label label4;
        private Label label3;
        private GroupBox Notification;
        private TableLayoutPanel tableLayoutPanel7;
        private PictureBox pictureBox5;
        private CheckBox checkBox4;
        private PictureBox pictureBox6;
        private Label label2;
        private TableLayoutPanel tableLayoutPanel4;
        private PictureBox pictureBox1;
        private CheckBox checkBox1;
        private System.Windows.Forms.Timer BatteryStatusTimer;
        private System.Windows.Forms.Timer ShowNotificationTimer;
        private Panel AppFooter;
        private Label VersionLabel;
        private Label NotificationText;
        private PictureBox CloseIcon;
        private TabPage DashboardTab;
        private TableLayoutPanel tableLayoutPanel9;
        private GroupBox NotificationSettingGroupBox;
        private TableLayoutPanel tableLayoutPanel12;
        private PictureBox LowBatteryIcon;
        private CheckBox LowBatteryNotificationCheckbox;
        private Label LowBatteryLabel;
        private TableLayoutPanel tableLayoutPanel13;
        private PictureBox FullBatteryIcon;
        private CheckBox FullBatteryNotificationCheckbox;
        private Label FullBatteryLabel;
        private FlowLayoutPanel flowLayoutPanel1;
        private PictureBox BatteryImage;
        private Label BatteryPercentage;
        private Label BatteryStatus;
        private Label RemainingTime;
        private TabPage SettingTab;
        private FlowLayoutPanel flowLayoutPanel2;
        private Panel ShowAsWindowPanel;
        private PictureBox PinToNotificationAreaPictureBox;
        private Label PinToNotificationAreaLabel;
        private CheckBox PinToNotificationArea;
        private Panel LaunchAtStartupPanel;
        private PictureBox LaunchAtStartUpPictureBox;
        private Label LaunchAtStartUpLabel;
        private CheckBox launchAtStartup;
        private TableLayoutPanel ThemeConfigurationPanel;
        private RadioButton DarkThemeLabel;
        private Panel ThemePanel;
        private PictureBox ThemePictureBox;
        private Label ThemeLabel;
        private RadioButton SystemThemeLabel;
        private RadioButton LightThemeLabel;
        private GroupBox NotificationGroupBox;
        private NumericUpDown lowBatteryPercentageValue;
        private PictureBox pictureBox8;
        private NumericUpDown fullbatteryPercentageValue;
        private TrackBar lowBatteryTrackbar;
        private Label LowBatteryPercentageLabel;
        private PictureBox pictureBox9;
        private Label ShowFullBatteryNotificationLabel;
        private Label LowBatteryNotificationLabel;
        private TrackBar fullBatteryTrackbar;
        private Label FullBatteryPercentageLabel;
        public CustomControls.FlatTabControl.FlatTabControl AppTabControl;
    }
}