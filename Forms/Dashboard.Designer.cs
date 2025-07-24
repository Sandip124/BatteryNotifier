using System.Windows.Forms;
using BatteryNotifier.Lib.CustomControls.FlatTabControl;

namespace BatteryNotifier.Forms
{
    partial class Dashboard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.AppHeaderTitle = new System.Windows.Forms.Label();
            this.CloseIcon = new System.Windows.Forms.PictureBox();
            this.AppTabControl = new BatteryNotifier.Lib.CustomControls.FlatTabControl.FlatTabControl();
            this.DashboardTab = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.NotificationSettingPanel = new System.Windows.Forms.Panel();
            this.NotificationSettingLabel = new System.Windows.Forms.Label();
            this.LowBatteryNotificationPanel = new System.Windows.Forms.TableLayoutPanel();
            this.LowBatteryIcon = new System.Windows.Forms.PictureBox();
            this.LowBatteryNotificationCheckbox = new System.Windows.Forms.CheckBox();
            this.LowBatteryLabel = new System.Windows.Forms.Label();
            this.FullBatteryNotificationPanel = new System.Windows.Forms.TableLayoutPanel();
            this.FullBatteryIcon = new System.Windows.Forms.PictureBox();
            this.FullBatteryNotificationCheckbox = new System.Windows.Forms.CheckBox();
            this.FullBatteryLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.BatteryImage = new System.Windows.Forms.PictureBox();
            this.BatteryPercentage = new System.Windows.Forms.Label();
            this.BatteryStatus = new System.Windows.Forms.Label();
            this.RemainingTime = new System.Windows.Forms.Label();
            this.SettingTab = new System.Windows.Forms.TabPage();
            this.NotificationPanel = new System.Windows.Forms.Panel();
            this.LowBatterySound = new System.Windows.Forms.TextBox();
            this.LowBatteryNotificationSettingLabel = new System.Windows.Forms.Label();
            this.FullBatterySound = new System.Windows.Forms.TextBox();
            this.FullBatteryNotificationSettingLabel = new System.Windows.Forms.Label();
            this.BrowseLowBatterySound = new System.Windows.Forms.PictureBox();
            this.BrowserFullBatterySound = new System.Windows.Forms.PictureBox();
            this.SettingHeader = new System.Windows.Forms.Label();
            this.LowBatteryPictureBox = new System.Windows.Forms.PictureBox();
            this.lowBatteryTrackbar = new System.Windows.Forms.TrackBar();
            this.FullBatteryPictureBox = new System.Windows.Forms.PictureBox();
            this.FullBatteryNotificationPercentageLabel = new System.Windows.Forms.Label();
            this.LowBatteryNotificationPercentageLabel = new System.Windows.Forms.Label();
            this.fullBatteryTrackbar = new System.Windows.Forms.TrackBar();
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
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).BeginInit();
            this.AppTabControl.SuspendLayout();
            this.DashboardTab.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.NotificationSettingPanel.SuspendLayout();
            this.LowBatteryNotificationPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryIcon)).BeginInit();
            this.FullBatteryNotificationPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BatteryImage)).BeginInit();
            this.SettingTab.SuspendLayout();
            this.NotificationPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BrowseLowBatterySound)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.BrowserFullBatterySound)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryTrackbar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryPictureBox)).BeginInit();
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
            this.BatteryNotifierIcon.BalloonTipText = "Did you get notification ?";
            this.BatteryNotifierIcon.BalloonTipTitle = "Battery Notifier";
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
            this.checkBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
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
            this.checkBox3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
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
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 17F));
            this.AppContainer.Size = new System.Drawing.Size(379, 440);
            this.AppContainer.TabIndex = 6;
            // 
            // AppFooter
            // 
            this.AppFooter.BackColor = System.Drawing.Color.AliceBlue;
            this.AppFooter.Controls.Add(this.VersionLabel);
            this.AppFooter.Controls.Add(this.NotificationText);
            this.AppFooter.Dock = System.Windows.Forms.DockStyle.Right;
            this.AppFooter.ForeColor = System.Drawing.Color.Crimson;
            this.AppFooter.Location = new System.Drawing.Point(0, 405);
            this.AppFooter.Margin = new System.Windows.Forms.Padding(0);
            this.AppFooter.Name = "AppFooter";
            this.AppFooter.Padding = new System.Windows.Forms.Padding(7, 0, 7, 0);
            this.AppFooter.Size = new System.Drawing.Size(379, 35);
            this.AppFooter.TabIndex = 8;
            // 
            // VersionLabel
            // 
            this.VersionLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.VersionLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.VersionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.VersionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.VersionLabel.Location = new System.Drawing.Point(300, 0);
            this.VersionLabel.Name = "VersionLabel";
            this.VersionLabel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.VersionLabel.Size = new System.Drawing.Size(72, 35);
            this.VersionLabel.TabIndex = 23;
            this.VersionLabel.Text = "v 1.0.0.0";
            this.VersionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // NotificationText
            // 
            this.NotificationText.Dock = System.Windows.Forms.DockStyle.Left;
            this.NotificationText.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.NotificationText.Location = new System.Drawing.Point(7, 0);
            this.NotificationText.Name = "NotificationText";
            this.NotificationText.Size = new System.Drawing.Size(267, 35);
            this.NotificationText.TabIndex = 17;
            this.NotificationText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // AppHeader
            // 
            this.AppHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(19)))), ((int)(((byte)(20)))));
            this.AppHeader.ColumnCount = 3;
            this.AppHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.AppHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AppHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.AppHeader.Controls.Add(this.pictureBox3, 0, 0);
            this.AppHeader.Controls.Add(this.AppHeaderTitle, 1, 0);
            this.AppHeader.Controls.Add(this.CloseIcon, 2, 0);
            this.AppHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppHeader.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.AppHeader.Location = new System.Drawing.Point(0, 0);
            this.AppHeader.Margin = new System.Windows.Forms.Padding(0);
            this.AppHeader.Name = "AppHeader";
            this.AppHeader.RowCount = 1;
            this.AppHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AppHeader.Size = new System.Drawing.Size(379, 40);
            this.AppHeader.TabIndex = 7;
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox3.Image = global::BatteryNotifier.Properties.Resources.battery_icon;
            this.pictureBox3.Location = new System.Drawing.Point(3, 0);
            this.pictureBox3.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(27, 40);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox3.TabIndex = 17;
            this.pictureBox3.TabStop = false;
            // 
            // AppHeaderTitle
            // 
            this.AppHeaderTitle.AutoSize = true;
            this.AppHeaderTitle.BackColor = System.Drawing.Color.Transparent;
            this.AppHeaderTitle.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.AppHeaderTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppHeaderTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.AppHeaderTitle.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.AppHeaderTitle.Location = new System.Drawing.Point(33, 0);
            this.AppHeaderTitle.Margin = new System.Windows.Forms.Padding(0);
            this.AppHeaderTitle.Name = "AppHeaderTitle";
            this.AppHeaderTitle.Size = new System.Drawing.Size(307, 40);
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
            this.CloseIcon.Location = new System.Drawing.Point(340, 0);
            this.CloseIcon.Margin = new System.Windows.Forms.Padding(0);
            this.CloseIcon.Name = "CloseIcon";
            this.CloseIcon.Size = new System.Drawing.Size(39, 40);
            this.CloseIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.CloseIcon.TabIndex = 0;
            this.CloseIcon.TabStop = false;
            // 
            // AppTabControl
            // 
            this.AppTabControl.Controls.Add(this.DashboardTab);
            this.AppTabControl.Controls.Add(this.SettingTab);
            this.AppTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppTabControl.Font = new System.Drawing.Font("Segoe UI", 11.25F);
            this.AppTabControl.Location = new System.Drawing.Point(0, 40);
            this.AppTabControl.Margin = new System.Windows.Forms.Padding(0);
            this.AppTabControl.BackColor = System.Drawing.Color.Transparent;
            this.AppTabControl.BorderColor = System.Drawing.SystemColors.ControlText;
            this.AppTabControl.Name = "AppTabControl";
            this.AppTabControl.SelectedIndex = 0;
            this.AppTabControl.ShowToolTips = true;
            this.AppTabControl.Size = new System.Drawing.Size(379, 365);
            this.AppTabControl.TabIndex = 9;
            // 
            // DashboardTab
            // 
            this.DashboardTab.BackColor = System.Drawing.Color.Transparent;
            this.DashboardTab.Controls.Add(this.tableLayoutPanel9);
            this.DashboardTab.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DashboardTab.Location = new System.Drawing.Point(4, 25);
            this.DashboardTab.Margin = new System.Windows.Forms.Padding(0);
            this.DashboardTab.Name = "DashboardTab";
            this.DashboardTab.Size = new System.Drawing.Size(371, 336);
            this.DashboardTab.TabIndex = 0;
            this.DashboardTab.Text = "Dashboard   ";
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.ColumnCount = 1;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel9.Controls.Add(this.NotificationSettingPanel, 0, 1);
            this.tableLayoutPanel9.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel9.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel9.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 2;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 209F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel9.Size = new System.Drawing.Size(371, 336);
            this.tableLayoutPanel9.TabIndex = 7;
            // 
            // NotificationSettingPanel
            // 
            this.NotificationSettingPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.NotificationSettingPanel.Controls.Add(this.NotificationSettingLabel);
            this.NotificationSettingPanel.Controls.Add(this.LowBatteryNotificationPanel);
            this.NotificationSettingPanel.Controls.Add(this.FullBatteryNotificationPanel);
            this.NotificationSettingPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.NotificationSettingPanel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.NotificationSettingPanel.Location = new System.Drawing.Point(7, 212);
            this.NotificationSettingPanel.Margin = new System.Windows.Forms.Padding(7, 3, 7, 7);
            this.NotificationSettingPanel.Name = "NotificationSettingPanel";
            this.NotificationSettingPanel.Size = new System.Drawing.Size(357, 117);
            this.NotificationSettingPanel.TabIndex = 5;
            this.NotificationSettingPanel.Text = "Notification Setting";
            // 
            // NotificationSettingLabel
            // 
            this.NotificationSettingLabel.BackColor = System.Drawing.Color.Gainsboro;
            this.NotificationSettingLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.NotificationSettingLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.NotificationSettingLabel.Location = new System.Drawing.Point(0, 0);
            this.NotificationSettingLabel.Name = "NotificationSettingLabel";
            this.NotificationSettingLabel.Size = new System.Drawing.Size(355, 23);
            this.NotificationSettingLabel.TabIndex = 28;
            this.NotificationSettingLabel.Text = "Notification Setting";
            this.NotificationSettingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LowBatteryNotificationPanel
            // 
            this.LowBatteryNotificationPanel.BackColor = System.Drawing.Color.Gainsboro;
            this.LowBatteryNotificationPanel.ColumnCount = 3;
            this.LowBatteryNotificationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.LowBatteryNotificationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LowBatteryNotificationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 69F));
            this.LowBatteryNotificationPanel.Controls.Add(this.LowBatteryIcon, 0, 0);
            this.LowBatteryNotificationPanel.Controls.Add(this.LowBatteryNotificationCheckbox, 2, 0);
            this.LowBatteryNotificationPanel.Controls.Add(this.LowBatteryLabel, 1, 0);
            this.LowBatteryNotificationPanel.Location = new System.Drawing.Point(8, 73);
            this.LowBatteryNotificationPanel.Name = "LowBatteryNotificationPanel";
            this.LowBatteryNotificationPanel.RowCount = 1;
            this.LowBatteryNotificationPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LowBatteryNotificationPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.LowBatteryNotificationPanel.Size = new System.Drawing.Size(344, 36);
            this.LowBatteryNotificationPanel.TabIndex = 8;
            // 
            // LowBatteryIcon
            // 
            this.LowBatteryIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.LowBatteryIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LowBatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Low;
            this.LowBatteryIcon.Location = new System.Drawing.Point(0, 0);
            this.LowBatteryIcon.Margin = new System.Windows.Forms.Padding(0);
            this.LowBatteryIcon.Name = "LowBatteryIcon";
            this.LowBatteryIcon.Size = new System.Drawing.Size(48, 36);
            this.LowBatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.LowBatteryIcon.TabIndex = 5;
            this.LowBatteryIcon.TabStop = false;
            // 
            // LowBatteryNotificationCheckbox
            // 
            this.LowBatteryNotificationCheckbox.AutoSize = true;
            this.LowBatteryNotificationCheckbox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.LowBatteryNotificationCheckbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LowBatteryNotificationCheckbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.LowBatteryNotificationCheckbox.Location = new System.Drawing.Point(278, 3);
            this.LowBatteryNotificationCheckbox.Name = "LowBatteryNotificationCheckbox";
            this.LowBatteryNotificationCheckbox.Size = new System.Drawing.Size(63, 30);
            this.LowBatteryNotificationCheckbox.TabIndex = 6;
            this.LowBatteryNotificationCheckbox.Text = "Off";
            this.LowBatteryNotificationCheckbox.UseVisualStyleBackColor = true;
            // 
            // LowBatteryLabel
            // 
            this.LowBatteryLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LowBatteryLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.LowBatteryLabel.Location = new System.Drawing.Point(51, 0);
            this.LowBatteryLabel.Name = "LowBatteryLabel";
            this.LowBatteryLabel.Size = new System.Drawing.Size(221, 36);
            this.LowBatteryLabel.TabIndex = 5;
            this.LowBatteryLabel.Text = "Low Battery";
            this.LowBatteryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FullBatteryNotificationPanel
            // 
            this.FullBatteryNotificationPanel.BackColor = System.Drawing.Color.Gainsboro;
            this.FullBatteryNotificationPanel.ColumnCount = 3;
            this.FullBatteryNotificationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.FullBatteryNotificationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.FullBatteryNotificationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.FullBatteryNotificationPanel.Controls.Add(this.FullBatteryIcon, 0, 0);
            this.FullBatteryNotificationPanel.Controls.Add(this.FullBatteryNotificationCheckbox, 2, 0);
            this.FullBatteryNotificationPanel.Controls.Add(this.FullBatteryLabel, 1, 0);
            this.FullBatteryNotificationPanel.Location = new System.Drawing.Point(8, 31);
            this.FullBatteryNotificationPanel.Name = "FullBatteryNotificationPanel";
            this.FullBatteryNotificationPanel.RowCount = 1;
            this.FullBatteryNotificationPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.FullBatteryNotificationPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.FullBatteryNotificationPanel.Size = new System.Drawing.Size(344, 36);
            this.FullBatteryNotificationPanel.TabIndex = 7;
            // 
            // FullBatteryIcon
            // 
            this.FullBatteryIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.FullBatteryIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FullBatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.FullBatteryIcon.Location = new System.Drawing.Point(0, 0);
            this.FullBatteryIcon.Margin = new System.Windows.Forms.Padding(0);
            this.FullBatteryIcon.Name = "FullBatteryIcon";
            this.FullBatteryIcon.Size = new System.Drawing.Size(47, 36);
            this.FullBatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.FullBatteryIcon.TabIndex = 5;
            this.FullBatteryIcon.TabStop = false;
            // 
            // FullBatteryNotificationCheckbox
            // 
            this.FullBatteryNotificationCheckbox.AutoSize = true;
            this.FullBatteryNotificationCheckbox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.FullBatteryNotificationCheckbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FullBatteryNotificationCheckbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.FullBatteryNotificationCheckbox.Location = new System.Drawing.Point(277, 3);
            this.FullBatteryNotificationCheckbox.Name = "FullBatteryNotificationCheckbox";
            this.FullBatteryNotificationCheckbox.Size = new System.Drawing.Size(64, 30);
            this.FullBatteryNotificationCheckbox.TabIndex = 6;
            this.FullBatteryNotificationCheckbox.Text = "Off";
            this.FullBatteryNotificationCheckbox.UseVisualStyleBackColor = true;
            // 
            // FullBatteryLabel
            // 
            this.FullBatteryLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FullBatteryLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.FullBatteryLabel.Location = new System.Drawing.Point(50, 0);
            this.FullBatteryLabel.Name = "FullBatteryLabel";
            this.FullBatteryLabel.Size = new System.Drawing.Size(221, 36);
            this.FullBatteryLabel.TabIndex = 5;
            this.FullBatteryLabel.Text = "Full Battery";
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
            this.flowLayoutPanel1.Size = new System.Drawing.Size(371, 209);
            this.flowLayoutPanel1.TabIndex = 6;
            // 
            // BatteryImage
            // 
            this.BatteryImage.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.BatteryImage.Location = new System.Drawing.Point(0, 0);
            this.BatteryImage.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryImage.Name = "BatteryImage";
            this.BatteryImage.Size = new System.Drawing.Size(368, 97);
            this.BatteryImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.BatteryImage.TabIndex = 23;
            this.BatteryImage.TabStop = false;
            // 
            // BatteryPercentage
            // 
            this.BatteryPercentage.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Bold);
            this.BatteryPercentage.Location = new System.Drawing.Point(0, 97);
            this.BatteryPercentage.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryPercentage.Name = "BatteryPercentage";
            this.BatteryPercentage.Size = new System.Drawing.Size(368, 47);
            this.BatteryPercentage.TabIndex = 22;
            this.BatteryPercentage.Text = "0%";
            this.BatteryPercentage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BatteryStatus
            // 
            this.BatteryStatus.AutoEllipsis = true;
            this.BatteryStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.BatteryStatus.ForeColor = System.Drawing.Color.Gray;
            this.BatteryStatus.Location = new System.Drawing.Point(0, 144);
            this.BatteryStatus.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryStatus.Name = "BatteryStatus";
            this.BatteryStatus.Size = new System.Drawing.Size(368, 24);
            this.BatteryStatus.TabIndex = 24;
            this.BatteryStatus.Text = "Charging status";
            this.BatteryStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // RemainingTime
            // 
            this.RemainingTime.AutoEllipsis = true;
            this.RemainingTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.RemainingTime.Location = new System.Drawing.Point(3, 168);
            this.RemainingTime.Name = "RemainingTime";
            this.RemainingTime.Size = new System.Drawing.Size(365, 29);
            this.RemainingTime.TabIndex = 25;
            this.RemainingTime.Text = "2 Hour 15  minutes";
            this.RemainingTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SettingTab
            // 
            this.SettingTab.BackColor = System.Drawing.Color.Transparent;
            this.SettingTab.Controls.Add(this.NotificationPanel);
            this.SettingTab.Controls.Add(this.flowLayoutPanel2);
            this.SettingTab.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SettingTab.Location = new System.Drawing.Point(4, 25);
            this.SettingTab.Margin = new System.Windows.Forms.Padding(0);
            this.SettingTab.Name = "SettingTab";
            this.SettingTab.Padding = new System.Windows.Forms.Padding(7);
            this.SettingTab.Size = new System.Drawing.Size(371, 336);
            this.SettingTab.TabIndex = 1;
            this.SettingTab.Text = "Setting   ";
            // 
            // NotificationPanel
            // 
            this.NotificationPanel.BackColor = System.Drawing.Color.Transparent;
            this.NotificationPanel.Controls.Add(this.LowBatterySound);
            this.NotificationPanel.Controls.Add(this.LowBatteryNotificationSettingLabel);
            this.NotificationPanel.Controls.Add(this.FullBatterySound);
            this.NotificationPanel.Controls.Add(this.FullBatteryNotificationSettingLabel);
            this.NotificationPanel.Controls.Add(this.BrowseLowBatterySound);
            this.NotificationPanel.Controls.Add(this.BrowserFullBatterySound);
            this.NotificationPanel.Controls.Add(this.SettingHeader);
            this.NotificationPanel.Controls.Add(this.LowBatteryPictureBox);
            this.NotificationPanel.Controls.Add(this.lowBatteryTrackbar);
            this.NotificationPanel.Controls.Add(this.FullBatteryPictureBox);
            this.NotificationPanel.Controls.Add(this.FullBatteryNotificationPercentageLabel);
            this.NotificationPanel.Controls.Add(this.LowBatteryNotificationPercentageLabel);
            this.NotificationPanel.Controls.Add(this.fullBatteryTrackbar);
            this.NotificationPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.NotificationPanel.Location = new System.Drawing.Point(7, 124);
            this.NotificationPanel.Margin = new System.Windows.Forms.Padding(0);
            this.NotificationPanel.Name = "NotificationPanel";
            this.NotificationPanel.Size = new System.Drawing.Size(357, 205);
            this.NotificationPanel.TabIndex = 35;
            this.NotificationPanel.Text = "Notification Setting for";
            // 
            // LowBatterySound
            // 
            this.LowBatterySound.Location = new System.Drawing.Point(7, 176);
            this.LowBatterySound.Name = "LowBatterySound";
            this.LowBatterySound.ReadOnly = true;
            this.LowBatterySound.Size = new System.Drawing.Size(306, 27);
            this.LowBatterySound.TabIndex = 33;
            // 
            // LowBatteryNotificationSettingLabel
            // 
            this.LowBatteryNotificationSettingLabel.AutoSize = true;
            this.LowBatteryNotificationSettingLabel.BackColor = System.Drawing.Color.Transparent;
            this.LowBatteryNotificationSettingLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.LowBatteryNotificationSettingLabel.Location = new System.Drawing.Point(60, 117);
            this.LowBatteryNotificationSettingLabel.Name = "LowBatteryNotificationSettingLabel";
            this.LowBatteryNotificationSettingLabel.Size = new System.Drawing.Size(93, 20);
            this.LowBatteryNotificationSettingLabel.TabIndex = 32;
            this.LowBatteryNotificationSettingLabel.Text = "Low Battery";
            // 
            // FullBatterySound
            // 
            this.FullBatterySound.Location = new System.Drawing.Point(8, 84);
            this.FullBatterySound.Name = "FullBatterySound";
            this.FullBatterySound.ReadOnly = true;
            this.FullBatterySound.Size = new System.Drawing.Size(305, 27);
            this.FullBatterySound.TabIndex = 31;
            // 
            // FullBatteryNotificationSettingLabel
            // 
            this.FullBatteryNotificationSettingLabel.AutoSize = true;
            this.FullBatteryNotificationSettingLabel.BackColor = System.Drawing.Color.Transparent;
            this.FullBatteryNotificationSettingLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.FullBatteryNotificationSettingLabel.Location = new System.Drawing.Point(60, 31);
            this.FullBatteryNotificationSettingLabel.Name = "FullBatteryNotificationSettingLabel";
            this.FullBatteryNotificationSettingLabel.Size = new System.Drawing.Size(89, 20);
            this.FullBatteryNotificationSettingLabel.TabIndex = 30;
            this.FullBatteryNotificationSettingLabel.Text = "Full Battery";
            // 
            // BrowseLowBatterySound
            // 
            this.BrowseLowBatterySound.BackColor = System.Drawing.Color.Transparent;
            this.BrowseLowBatterySound.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.BrowseLowBatterySound.Image = global::BatteryNotifier.Properties.Resources.music_icon;
            this.BrowseLowBatterySound.Location = new System.Drawing.Point(316, 174);
            this.BrowseLowBatterySound.Margin = new System.Windows.Forms.Padding(0);
            this.BrowseLowBatterySound.Name = "BrowseLowBatterySound";
            this.BrowseLowBatterySound.Padding = new System.Windows.Forms.Padding(2);
            this.BrowseLowBatterySound.Size = new System.Drawing.Size(28, 29);
            this.BrowseLowBatterySound.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.BrowseLowBatterySound.TabIndex = 29;
            this.BrowseLowBatterySound.TabStop = false;
            // 
            // BrowserFullBatterySound
            // 
            this.BrowserFullBatterySound.BackColor = System.Drawing.Color.Transparent;
            this.BrowserFullBatterySound.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.BrowserFullBatterySound.Image = global::BatteryNotifier.Properties.Resources.music_icon;
            this.BrowserFullBatterySound.Location = new System.Drawing.Point(316, 84);
            this.BrowserFullBatterySound.Margin = new System.Windows.Forms.Padding(0);
            this.BrowserFullBatterySound.Name = "BrowserFullBatterySound";
            this.BrowserFullBatterySound.Padding = new System.Windows.Forms.Padding(2);
            this.BrowserFullBatterySound.Size = new System.Drawing.Size(28, 27);
            this.BrowserFullBatterySound.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.BrowserFullBatterySound.TabIndex = 28;
            this.BrowserFullBatterySound.TabStop = false;
            // 
            // SettingHeader
            // 
            this.SettingHeader.BackColor = System.Drawing.Color.Transparent;
            this.SettingHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.SettingHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.SettingHeader.Location = new System.Drawing.Point(0, 0);
            this.SettingHeader.Name = "SettingHeader";
            this.SettingHeader.Size = new System.Drawing.Size(357, 23);
            this.SettingHeader.TabIndex = 27;
            this.SettingHeader.Text = "Notification Setting";
            this.SettingHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LowBatteryPictureBox
            // 
            this.LowBatteryPictureBox.BackColor = System.Drawing.Color.Transparent;
            this.LowBatteryPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.LowBatteryPictureBox.Image = global::BatteryNotifier.Properties.Resources.Low;
            this.LowBatteryPictureBox.Location = new System.Drawing.Point(7, 117);
            this.LowBatteryPictureBox.Name = "LowBatteryPictureBox";
            this.LowBatteryPictureBox.Size = new System.Drawing.Size(48, 54);
            this.LowBatteryPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.LowBatteryPictureBox.TabIndex = 26;
            this.LowBatteryPictureBox.TabStop = false;
            // 
            // lowBatteryTrackbar
            // 
            this.lowBatteryTrackbar.Cursor = System.Windows.Forms.Cursors.SizeWE;
            this.lowBatteryTrackbar.LargeChange = 10;
            this.lowBatteryTrackbar.Location = new System.Drawing.Point(59, 137);
            this.lowBatteryTrackbar.Maximum = 100;
            this.lowBatteryTrackbar.Name = "lowBatteryTrackbar";
            this.lowBatteryTrackbar.Size = new System.Drawing.Size(229, 45);
            this.lowBatteryTrackbar.TabIndex = 20;
            this.lowBatteryTrackbar.Value = 40;
            // 
            // FullBatteryPictureBox
            // 
            this.FullBatteryPictureBox.BackColor = System.Drawing.Color.Transparent;
            this.FullBatteryPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.FullBatteryPictureBox.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.FullBatteryPictureBox.Location = new System.Drawing.Point(7, 31);
            this.FullBatteryPictureBox.Name = "FullBatteryPictureBox";
            this.FullBatteryPictureBox.Size = new System.Drawing.Size(47, 48);
            this.FullBatteryPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.FullBatteryPictureBox.TabIndex = 25;
            this.FullBatteryPictureBox.TabStop = false;
            // 
            // FullBatteryNotificationPercentageLabel
            // 
            this.FullBatteryNotificationPercentageLabel.AutoSize = true;
            this.FullBatteryNotificationPercentageLabel.BackColor = System.Drawing.Color.Transparent;
            this.FullBatteryNotificationPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.FullBatteryNotificationPercentageLabel.Location = new System.Drawing.Point(289, 49);
            this.FullBatteryNotificationPercentageLabel.Name = "FullBatteryNotificationPercentageLabel";
            this.FullBatteryNotificationPercentageLabel.Size = new System.Drawing.Size(50, 20);
            this.FullBatteryNotificationPercentageLabel.TabIndex = 18;
            this.FullBatteryNotificationPercentageLabel.Text = "100%";
            this.FullBatteryNotificationPercentageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LowBatteryNotificationPercentageLabel
            // 
            this.LowBatteryNotificationPercentageLabel.AutoSize = true;
            this.LowBatteryNotificationPercentageLabel.BackColor = System.Drawing.Color.Transparent;
            this.LowBatteryNotificationPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.LowBatteryNotificationPercentageLabel.Location = new System.Drawing.Point(289, 137);
            this.LowBatteryNotificationPercentageLabel.Name = "LowBatteryNotificationPercentageLabel";
            this.LowBatteryNotificationPercentageLabel.Size = new System.Drawing.Size(41, 20);
            this.LowBatteryNotificationPercentageLabel.TabIndex = 18;
            this.LowBatteryNotificationPercentageLabel.Text = "40%";
            this.LowBatteryNotificationPercentageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // fullBatteryTrackbar
            // 
            this.fullBatteryTrackbar.Cursor = System.Windows.Forms.Cursors.SizeWE;
            this.fullBatteryTrackbar.LargeChange = 10;
            this.fullBatteryTrackbar.Location = new System.Drawing.Point(57, 49);
            this.fullBatteryTrackbar.Margin = new System.Windows.Forms.Padding(0);
            this.fullBatteryTrackbar.Maximum = 100;
            this.fullBatteryTrackbar.Name = "fullBatteryTrackbar";
            this.fullBatteryTrackbar.Size = new System.Drawing.Size(231, 45);
            this.fullBatteryTrackbar.TabIndex = 20;
            this.fullBatteryTrackbar.Value = 100;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.ShowAsWindowPanel);
            this.flowLayoutPanel2.Controls.Add(this.LaunchAtStartupPanel);
            this.flowLayoutPanel2.Controls.Add(this.ThemeConfigurationPanel);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(7, 7);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(357, 109);
            this.flowLayoutPanel2.TabIndex = 34;
            // 
            // ShowAsWindowPanel
            // 
            this.ShowAsWindowPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ShowAsWindowPanel.Controls.Add(this.PinToNotificationAreaPictureBox);
            this.ShowAsWindowPanel.Controls.Add(this.PinToNotificationAreaLabel);
            this.ShowAsWindowPanel.Controls.Add(this.PinToNotificationArea);
            this.ShowAsWindowPanel.Location = new System.Drawing.Point(0, 2);
            this.ShowAsWindowPanel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.ShowAsWindowPanel.Name = "ShowAsWindowPanel";
            this.ShowAsWindowPanel.Size = new System.Drawing.Size(357, 31);
            this.ShowAsWindowPanel.TabIndex = 30;
            // 
            // PinToNotificationAreaPictureBox
            // 
            this.PinToNotificationAreaPictureBox.BackColor = System.Drawing.Color.Transparent;
            this.PinToNotificationAreaPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.PinToNotificationAreaPictureBox.Image = global::BatteryNotifier.Properties.Resources.Window;
            this.PinToNotificationAreaPictureBox.Location = new System.Drawing.Point(0, 0);
            this.PinToNotificationAreaPictureBox.Name = "PinToNotificationAreaPictureBox";
            this.PinToNotificationAreaPictureBox.Size = new System.Drawing.Size(30, 31);
            this.PinToNotificationAreaPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PinToNotificationAreaPictureBox.TabIndex = 27;
            this.PinToNotificationAreaPictureBox.TabStop = false;
            // 
            // PinToNotificationAreaLabel
            // 
            this.PinToNotificationAreaLabel.AutoSize = true;
            this.PinToNotificationAreaLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.PinToNotificationAreaLabel.Location = new System.Drawing.Point(32, 6);
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
            this.PinToNotificationArea.Location = new System.Drawing.Point(329, 10);
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
            this.LaunchAtStartupPanel.Location = new System.Drawing.Point(0, 37);
            this.LaunchAtStartupPanel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.LaunchAtStartupPanel.Name = "LaunchAtStartupPanel";
            this.LaunchAtStartupPanel.Size = new System.Drawing.Size(357, 31);
            this.LaunchAtStartupPanel.TabIndex = 33;
            // 
            // LaunchAtStartUpPictureBox
            // 
            this.LaunchAtStartUpPictureBox.BackColor = System.Drawing.Color.Transparent;
            this.LaunchAtStartUpPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.LaunchAtStartUpPictureBox.Image = global::BatteryNotifier.Properties.Resources.launchatstartup;
            this.LaunchAtStartUpPictureBox.Location = new System.Drawing.Point(1, -2);
            this.LaunchAtStartUpPictureBox.Name = "LaunchAtStartUpPictureBox";
            this.LaunchAtStartUpPictureBox.Size = new System.Drawing.Size(29, 33);
            this.LaunchAtStartUpPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.LaunchAtStartUpPictureBox.TabIndex = 28;
            this.LaunchAtStartUpPictureBox.TabStop = false;
            // 
            // LaunchAtStartUpLabel
            // 
            this.LaunchAtStartUpLabel.AutoSize = true;
            this.LaunchAtStartUpLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.LaunchAtStartUpLabel.Location = new System.Drawing.Point(32, 6);
            this.LaunchAtStartUpLabel.Name = "LaunchAtStartUpLabel";
            this.LaunchAtStartUpLabel.Size = new System.Drawing.Size(139, 20);
            this.LaunchAtStartUpLabel.TabIndex = 16;
            this.LaunchAtStartUpLabel.Text = "Launch At Startup";
            // 
            // launchAtStartup
            // 
            this.launchAtStartup.AutoSize = true;
            this.launchAtStartup.Cursor = System.Windows.Forms.Cursors.Hand;
            this.launchAtStartup.Location = new System.Drawing.Point(329, 10);
            this.launchAtStartup.Name = "launchAtStartup";
            this.launchAtStartup.Size = new System.Drawing.Size(15, 14);
            this.launchAtStartup.TabIndex = 17;
            this.launchAtStartup.UseVisualStyleBackColor = true;
            // 
            // ThemeConfigurationPanel
            // 
            this.ThemeConfigurationPanel.BackColor = System.Drawing.SystemColors.Menu;
            this.ThemeConfigurationPanel.ColumnCount = 4;
            this.ThemeConfigurationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            this.ThemeConfigurationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 39.14729F));
            this.ThemeConfigurationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 29.45737F));
            this.ThemeConfigurationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 31.00775F));
            this.ThemeConfigurationPanel.Controls.Add(this.DarkThemeLabel, 2, 0);
            this.ThemeConfigurationPanel.Controls.Add(this.ThemePanel, 0, 0);
            this.ThemeConfigurationPanel.Controls.Add(this.SystemThemeLabel, 1, 0);
            this.ThemeConfigurationPanel.Controls.Add(this.LightThemeLabel, 1, 0);
            this.ThemeConfigurationPanel.Location = new System.Drawing.Point(0, 72);
            this.ThemeConfigurationPanel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.ThemeConfigurationPanel.Name = "ThemeConfigurationPanel";
            this.ThemeConfigurationPanel.RowCount = 1;
            this.ThemeConfigurationPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ThemeConfigurationPanel.Size = new System.Drawing.Size(357, 31);
            this.ThemeConfigurationPanel.TabIndex = 35;
            // 
            // DarkThemeLabel
            // 
            this.DarkThemeLabel.BackColor = System.Drawing.Color.Transparent;
            this.DarkThemeLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DarkThemeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DarkThemeLabel.Location = new System.Drawing.Point(283, 3);
            this.DarkThemeLabel.Margin = new System.Windows.Forms.Padding(5, 3, 3, 3);
            this.DarkThemeLabel.Name = "DarkThemeLabel";
            this.DarkThemeLabel.Size = new System.Drawing.Size(71, 25);
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
            this.ThemePanel.Size = new System.Drawing.Size(105, 31);
            this.ThemePanel.TabIndex = 33;
            // 
            // ThemePictureBox
            // 
            this.ThemePictureBox.BackColor = System.Drawing.Color.Transparent;
            this.ThemePictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ThemePictureBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.ThemePictureBox.Image = global::BatteryNotifier.Properties.Resources.DarkMode;
            this.ThemePictureBox.Location = new System.Drawing.Point(0, 0);
            this.ThemePictureBox.Margin = new System.Windows.Forms.Padding(0);
            this.ThemePictureBox.Name = "ThemePictureBox";
            this.ThemePictureBox.Size = new System.Drawing.Size(30, 31);
            this.ThemePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ThemePictureBox.TabIndex = 28;
            this.ThemePictureBox.TabStop = false;
            // 
            // ThemeLabel
            // 
            this.ThemeLabel.BackColor = System.Drawing.Color.Transparent;
            this.ThemeLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.ThemeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.ThemeLabel.Location = new System.Drawing.Point(32, 0);
            this.ThemeLabel.Margin = new System.Windows.Forms.Padding(0);
            this.ThemeLabel.Name = "ThemeLabel";
            this.ThemeLabel.Size = new System.Drawing.Size(73, 31);
            this.ThemeLabel.TabIndex = 16;
            this.ThemeLabel.Text = "Theme :";
            this.ThemeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SystemThemeLabel
            // 
            this.SystemThemeLabel.BackColor = System.Drawing.Color.Transparent;
            this.SystemThemeLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SystemThemeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SystemThemeLabel.Location = new System.Drawing.Point(110, 3);
            this.SystemThemeLabel.Margin = new System.Windows.Forms.Padding(5, 3, 3, 3);
            this.SystemThemeLabel.Name = "SystemThemeLabel";
            this.SystemThemeLabel.Size = new System.Drawing.Size(91, 25);
            this.SystemThemeLabel.TabIndex = 37;
            this.SystemThemeLabel.TabStop = true;
            this.SystemThemeLabel.Text = "System";
            this.SystemThemeLabel.UseVisualStyleBackColor = false;
            // 
            // LightThemeLabel
            // 
            this.LightThemeLabel.BackColor = System.Drawing.Color.Transparent;
            this.LightThemeLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.LightThemeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LightThemeLabel.Location = new System.Drawing.Point(209, 3);
            this.LightThemeLabel.Margin = new System.Windows.Forms.Padding(5, 3, 3, 3);
            this.LightThemeLabel.Name = "LightThemeLabel";
            this.LightThemeLabel.Size = new System.Drawing.Size(66, 25);
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
            this.BatteryPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Bold);
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
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
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
            this.checkBox4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
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
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
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
            this.checkBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.checkBox1.Location = new System.Drawing.Point(97, 3);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(60, 94);
            this.checkBox1.TabIndex = 6;
            this.checkBox1.Text = "Off";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // Dashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(381, 442);
            this.Controls.Add(this.AppContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(90, 90);
            this.MaximizeBox = false;
            this.Name = "Dashboard";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.Text = "Battery Notifier";
            this.Load += new System.EventHandler(this.Dashboard_Load);
            this.tableLayoutPanel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.tableLayoutPanel6.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
            this.AppContainer.ResumeLayout(false);
            this.AppFooter.ResumeLayout(false);
            this.AppHeader.ResumeLayout(false);
            this.AppHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).EndInit();
            this.AppTabControl.ResumeLayout(false);
            this.DashboardTab.ResumeLayout(false);
            this.tableLayoutPanel9.ResumeLayout(false);
            this.NotificationSettingPanel.ResumeLayout(false);
            this.LowBatteryNotificationPanel.ResumeLayout(false);
            this.LowBatteryNotificationPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryIcon)).EndInit();
            this.FullBatteryNotificationPanel.ResumeLayout(false);
            this.FullBatteryNotificationPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BatteryImage)).EndInit();
            this.SettingTab.ResumeLayout(false);
            this.NotificationPanel.ResumeLayout(false);
            this.NotificationPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BrowseLowBatterySound)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.BrowserFullBatterySound)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryTrackbar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryPictureBox)).EndInit();
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

        private System.Windows.Forms.NotifyIcon BatteryNotifierIcon;
        private TableLayoutPanel tableLayoutPanel5;
        private PictureBox pictureBox2;
        private CheckBox checkBox2;
        private TableLayoutPanel tableLayoutPanel6;
        private PictureBox pictureBox4;
        private CheckBox checkBox3;
        private System.Windows.Forms.TableLayoutPanel AppContainer;
        private System.Windows.Forms.TableLayoutPanel AppHeader;
        private System.Windows.Forms.Label AppHeaderTitle;
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
        private System.Windows.Forms.Panel AppFooter;
        private System.Windows.Forms.Label VersionLabel;
        private Label NotificationText;
        private System.Windows.Forms.PictureBox CloseIcon;
        private System.Windows.Forms.TabPage DashboardTab;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel9;
        private System.Windows.Forms.Panel NotificationSettingPanel;
        private System.Windows.Forms.TableLayoutPanel LowBatteryNotificationPanel;
        private PictureBox LowBatteryIcon;
        private System.Windows.Forms.CheckBox LowBatteryNotificationCheckbox;
        private System.Windows.Forms.Label LowBatteryLabel;
        private System.Windows.Forms.TableLayoutPanel FullBatteryNotificationPanel;
        private PictureBox FullBatteryIcon;
        private System.Windows.Forms.CheckBox FullBatteryNotificationCheckbox;
        private System.Windows.Forms.Label FullBatteryLabel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.PictureBox BatteryImage;
        private System.Windows.Forms.Label BatteryPercentage;
        private System.Windows.Forms.Label BatteryStatus;
        private System.Windows.Forms.Label RemainingTime;
        private System.Windows.Forms.TabPage SettingTab;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Panel ShowAsWindowPanel;
        private PictureBox PinToNotificationAreaPictureBox;
        private Label PinToNotificationAreaLabel;
        private System.Windows.Forms.CheckBox PinToNotificationArea;
        private System.Windows.Forms.Panel LaunchAtStartupPanel;
        private PictureBox LaunchAtStartUpPictureBox;
        private Label LaunchAtStartUpLabel;
        private System.Windows.Forms.CheckBox launchAtStartup;
        private System.Windows.Forms.TableLayoutPanel ThemeConfigurationPanel;
        private System.Windows.Forms.RadioButton DarkThemeLabel;
        private System.Windows.Forms.Panel ThemePanel;
        private PictureBox ThemePictureBox;
        private System.Windows.Forms.Label ThemeLabel;
        private System.Windows.Forms.RadioButton SystemThemeLabel;
        private System.Windows.Forms.RadioButton LightThemeLabel;
        private System.Windows.Forms.Panel NotificationPanel;
        private PictureBox LowBatteryPictureBox;
        private System.Windows.Forms.TrackBar lowBatteryTrackbar;
        private PictureBox FullBatteryPictureBox;
        private System.Windows.Forms.Label FullBatteryNotificationPercentageLabel;
        private System.Windows.Forms.Label LowBatteryNotificationPercentageLabel;
        private System.Windows.Forms.TrackBar fullBatteryTrackbar;
        public BatteryNotifier.Lib.CustomControls.FlatTabControl.FlatTabControl AppTabControl;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.Label SettingHeader;
        private System.Windows.Forms.PictureBox BrowserFullBatterySound;
        private System.Windows.Forms.PictureBox BrowseLowBatterySound;
        private System.Windows.Forms.Label NotificationSettingLabel;
        private System.Windows.Forms.TextBox LowBatterySound;
        private Label LowBatteryNotificationSettingLabel;
        private System.Windows.Forms.TextBox FullBatterySound;
        private Label FullBatteryNotificationSettingLabel;
    }
}