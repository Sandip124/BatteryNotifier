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
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.BatteryImage = new System.Windows.Forms.PictureBox();
            this.BatteryPercentage = new System.Windows.Forms.Label();
            this.BatteryStatus = new System.Windows.Forms.Label();
            this.RemainingTime = new System.Windows.Forms.Label();
            this.AppHeader = new System.Windows.Forms.TableLayoutPanel();
            this.AppHeaderTitle = new System.Windows.Forms.Label();
            this.CloseIcon = new System.Windows.Forms.PictureBox();
            this.CheckingForUpdateLabel = new System.Windows.Forms.Label();
            this.AppFooter = new System.Windows.Forms.Panel();
            this.VersionLabel = new System.Windows.Forms.Label();
            this.ViewSourceLabel = new System.Windows.Forms.Label();
            this.SettingLabel = new System.Windows.Forms.Label();
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
            this.tableLayoutPanel13 = new System.Windows.Forms.TableLayoutPanel();
            this.FullBatteryLabel = new System.Windows.Forms.Label();
            this.FullBatteryNotificationCheckbox = new System.Windows.Forms.CheckBox();
            this.FullBatteryIcon = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel12 = new System.Windows.Forms.TableLayoutPanel();
            this.LowBatteryLabel = new System.Windows.Forms.Label();
            this.LowBatteryNotificationCheckbox = new System.Windows.Forms.CheckBox();
            this.LowBatteryIcon = new System.Windows.Forms.PictureBox();
            this.NotificationSettingGroupBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.tableLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
            this.AppContainer.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BatteryImage)).BeginInit();
            this.AppHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).BeginInit();
            this.AppFooter.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BatteryIcon)).BeginInit();
            this.tableLayoutPanel3.SuspendLayout();
            this.BatteryDetail.SuspendLayout();
            this.Notification.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).BeginInit();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tableLayoutPanel13.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).BeginInit();
            this.tableLayoutPanel12.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryIcon)).BeginInit();
            this.NotificationSettingGroupBox.SuspendLayout();
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
            this.AppContainer.BackColor = System.Drawing.Color.White;
            this.AppContainer.ColumnCount = 1;
            this.AppContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AppContainer.Controls.Add(this.tableLayoutPanel9, 0, 1);
            this.AppContainer.Controls.Add(this.AppHeader, 0, 0);
            this.AppContainer.Controls.Add(this.AppFooter, 0, 2);
            this.AppContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppContainer.Location = new System.Drawing.Point(1, 1);
            this.AppContainer.Name = "AppContainer";
            this.AppContainer.RowCount = 3;
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.932461F));
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 91.06754F));
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.AppContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.AppContainer.Size = new System.Drawing.Size(408, 508);
            this.AppContainer.TabIndex = 6;
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.ColumnCount = 1;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel9.Controls.Add(this.NotificationSettingGroupBox, 0, 1);
            this.tableLayoutPanel9.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel9.Location = new System.Drawing.Point(3, 44);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 2;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 268F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.tableLayoutPanel9.Size = new System.Drawing.Size(402, 421);
            this.tableLayoutPanel9.TabIndex = 6;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.BatteryImage);
            this.flowLayoutPanel1.Controls.Add(this.BatteryPercentage);
            this.flowLayoutPanel1.Controls.Add(this.BatteryStatus);
            this.flowLayoutPanel1.Controls.Add(this.RemainingTime);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(396, 262);
            this.flowLayoutPanel1.TabIndex = 6;
            // 
            // BatteryImage
            // 
            this.BatteryImage.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.BatteryImage.Location = new System.Drawing.Point(0, 0);
            this.BatteryImage.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryImage.Name = "BatteryImage";
            this.BatteryImage.Size = new System.Drawing.Size(391, 122);
            this.BatteryImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.BatteryImage.TabIndex = 23;
            this.BatteryImage.TabStop = false;
            // 
            // BatteryPercentage
            // 
            this.BatteryPercentage.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.BatteryPercentage.Location = new System.Drawing.Point(0, 122);
            this.BatteryPercentage.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryPercentage.Name = "BatteryPercentage";
            this.BatteryPercentage.Size = new System.Drawing.Size(391, 63);
            this.BatteryPercentage.TabIndex = 22;
            this.BatteryPercentage.Text = "0%";
            this.BatteryPercentage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BatteryStatus
            // 
            this.BatteryStatus.AutoEllipsis = true;
            this.BatteryStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.BatteryStatus.ForeColor = System.Drawing.Color.Gray;
            this.BatteryStatus.Location = new System.Drawing.Point(0, 185);
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
            this.RemainingTime.Location = new System.Drawing.Point(3, 213);
            this.RemainingTime.Name = "RemainingTime";
            this.RemainingTime.Size = new System.Drawing.Size(388, 36);
            this.RemainingTime.TabIndex = 25;
            this.RemainingTime.Text = "2 Hour 15  minutes";
            this.RemainingTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // AppHeader
            // 
            this.AppHeader.BackColor = System.Drawing.Color.AliceBlue;
            this.AppHeader.ColumnCount = 3;
            this.AppHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 39.08356F));
            this.AppHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60.91644F));
            this.AppHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.AppHeader.Controls.Add(this.AppHeaderTitle, 0, 0);
            this.AppHeader.Controls.Add(this.CloseIcon, 2, 0);
            this.AppHeader.Controls.Add(this.CheckingForUpdateLabel, 1, 0);
            this.AppHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppHeader.Location = new System.Drawing.Point(0, 0);
            this.AppHeader.Margin = new System.Windows.Forms.Padding(0);
            this.AppHeader.Name = "AppHeader";
            this.AppHeader.RowCount = 1;
            this.AppHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AppHeader.Size = new System.Drawing.Size(408, 41);
            this.AppHeader.TabIndex = 7;
            // 
            // AppHeaderTitle
            // 
            this.AppHeaderTitle.AutoSize = true;
            this.AppHeaderTitle.BackColor = System.Drawing.Color.Transparent;
            this.AppHeaderTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AppHeaderTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.AppHeaderTitle.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.AppHeaderTitle.Location = new System.Drawing.Point(3, 0);
            this.AppHeaderTitle.Name = "AppHeaderTitle";
            this.AppHeaderTitle.Size = new System.Drawing.Size(139, 41);
            this.AppHeaderTitle.TabIndex = 16;
            this.AppHeaderTitle.Text = "Battery Notifier";
            this.AppHeaderTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.AppHeaderTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AppHeaderTitle_MouseDown);
            this.AppHeaderTitle.MouseMove += new System.Windows.Forms.MouseEventHandler(this.AppHeaderTitle_MouseMove);
            this.AppHeaderTitle.MouseUp += new System.Windows.Forms.MouseEventHandler(this.AppHeaderTitle_MouseUp);
            // 
            // CloseIcon
            // 
            this.CloseIcon.BackColor = System.Drawing.Color.Transparent;
            this.CloseIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.CloseIcon.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CloseIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CloseIcon.Image = global::BatteryNotifier.Properties.Resources.closeIconLight;
            this.CloseIcon.Location = new System.Drawing.Point(371, 0);
            this.CloseIcon.Margin = new System.Windows.Forms.Padding(0);
            this.CloseIcon.Name = "CloseIcon";
            this.CloseIcon.Size = new System.Drawing.Size(37, 41);
            this.CloseIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.CloseIcon.TabIndex = 0;
            this.CloseIcon.TabStop = false;
            // 
            // CheckingForUpdateLabel
            // 
            this.CheckingForUpdateLabel.AutoSize = true;
            this.CheckingForUpdateLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CheckingForUpdateLabel.Location = new System.Drawing.Point(148, 0);
            this.CheckingForUpdateLabel.Name = "CheckingForUpdateLabel";
            this.CheckingForUpdateLabel.Size = new System.Drawing.Size(220, 41);
            this.CheckingForUpdateLabel.TabIndex = 17;
            this.CheckingForUpdateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // AppFooter
            // 
            this.AppFooter.BackColor = System.Drawing.Color.AliceBlue;
            this.AppFooter.Controls.Add(this.VersionLabel);
            this.AppFooter.Controls.Add(this.ViewSourceLabel);
            this.AppFooter.Controls.Add(this.SettingLabel);
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
            this.VersionLabel.Location = new System.Drawing.Point(333, 15);
            this.VersionLabel.Name = "VersionLabel";
            this.VersionLabel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.VersionLabel.Size = new System.Drawing.Size(51, 16);
            this.VersionLabel.TabIndex = 23;
            this.VersionLabel.Text = "v 1.0.0";
            // 
            // ViewSourceLabel
            // 
            this.ViewSourceLabel.AutoSize = true;
            this.ViewSourceLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ViewSourceLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.ViewSourceLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ViewSourceLabel.Location = new System.Drawing.Point(79, 10);
            this.ViewSourceLabel.Name = "ViewSourceLabel";
            this.ViewSourceLabel.Size = new System.Drawing.Size(109, 20);
            this.ViewSourceLabel.TabIndex = 22;
            this.ViewSourceLabel.Text = "View Source";
            // 
            // SettingLabel
            // 
            this.SettingLabel.AutoSize = true;
            this.SettingLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SettingLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.SettingLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.SettingLabel.Location = new System.Drawing.Point(9, 10);
            this.SettingLabel.Name = "SettingLabel";
            this.SettingLabel.Size = new System.Drawing.Size(67, 20);
            this.SettingLabel.TabIndex = 21;
            this.SettingLabel.Text = "Setting";

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
            // tableLayoutPanel13
            // 
            this.tableLayoutPanel13.ColumnCount = 3;
            this.tableLayoutPanel13.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 71F));
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
            // FullBatteryLabel
            // 
            this.FullBatteryLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FullBatteryLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FullBatteryLabel.Location = new System.Drawing.Point(74, 0);
            this.FullBatteryLabel.Name = "FullBatteryLabel";
            this.FullBatteryLabel.Size = new System.Drawing.Size(233, 42);
            this.FullBatteryLabel.TabIndex = 5;
            this.FullBatteryLabel.Text = "Full Battery Notification";
            this.FullBatteryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FullBatteryNotificationCheckbox
            // 
            this.FullBatteryNotificationCheckbox.AutoSize = true;
            this.FullBatteryNotificationCheckbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FullBatteryNotificationCheckbox.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FullBatteryNotificationCheckbox.Location = new System.Drawing.Point(313, 3);
            this.FullBatteryNotificationCheckbox.Name = "FullBatteryNotificationCheckbox";
            this.FullBatteryNotificationCheckbox.Size = new System.Drawing.Size(60, 36);
            this.FullBatteryNotificationCheckbox.TabIndex = 6;
            this.FullBatteryNotificationCheckbox.Text = "Off";
            this.FullBatteryNotificationCheckbox.UseVisualStyleBackColor = true;
            // 
            // FullBatteryIcon
            // 
            this.FullBatteryIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.FullBatteryIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FullBatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.FullBatteryIcon.Location = new System.Drawing.Point(3, 3);
            this.FullBatteryIcon.Name = "FullBatteryIcon";
            this.FullBatteryIcon.Size = new System.Drawing.Size(65, 36);
            this.FullBatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.FullBatteryIcon.TabIndex = 5;
            this.FullBatteryIcon.TabStop = false;
            // 
            // tableLayoutPanel12
            // 
            this.tableLayoutPanel12.ColumnCount = 3;
            this.tableLayoutPanel12.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 71F));
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
            // LowBatteryLabel
            // 
            this.LowBatteryLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.LowBatteryLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LowBatteryLabel.Location = new System.Drawing.Point(74, 0);
            this.LowBatteryLabel.Name = "LowBatteryLabel";
            this.LowBatteryLabel.Size = new System.Drawing.Size(233, 42);
            this.LowBatteryLabel.TabIndex = 5;
            this.LowBatteryLabel.Text = "Low Battery Notification";
            this.LowBatteryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LowBatteryNotificationCheckbox
            // 
            this.LowBatteryNotificationCheckbox.AutoSize = true;
            this.LowBatteryNotificationCheckbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LowBatteryNotificationCheckbox.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LowBatteryNotificationCheckbox.Location = new System.Drawing.Point(313, 3);
            this.LowBatteryNotificationCheckbox.Name = "LowBatteryNotificationCheckbox";
            this.LowBatteryNotificationCheckbox.Size = new System.Drawing.Size(60, 36);
            this.LowBatteryNotificationCheckbox.TabIndex = 6;
            this.LowBatteryNotificationCheckbox.Text = "Off";
            this.LowBatteryNotificationCheckbox.UseVisualStyleBackColor = true;
            // 
            // LowBatteryIcon
            // 
            this.LowBatteryIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.LowBatteryIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LowBatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Low;
            this.LowBatteryIcon.Location = new System.Drawing.Point(3, 3);
            this.LowBatteryIcon.Name = "LowBatteryIcon";
            this.LowBatteryIcon.Size = new System.Drawing.Size(65, 36);
            this.LowBatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.LowBatteryIcon.TabIndex = 5;
            this.LowBatteryIcon.TabStop = false;
            // 
            // NotificationSettingGroupBox
            // 
            this.NotificationSettingGroupBox.Controls.Add(this.tableLayoutPanel12);
            this.NotificationSettingGroupBox.Controls.Add(this.tableLayoutPanel13);
            this.NotificationSettingGroupBox.Font = new System.Drawing.Font("Roboto", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.NotificationSettingGroupBox.Location = new System.Drawing.Point(8, 276);
            this.NotificationSettingGroupBox.Margin = new System.Windows.Forms.Padding(8);
            this.NotificationSettingGroupBox.Name = "NotificationSettingGroupBox";
            this.NotificationSettingGroupBox.Size = new System.Drawing.Size(386, 137);
            this.NotificationSettingGroupBox.TabIndex = 5;
            this.NotificationSettingGroupBox.TabStop = false;
            this.NotificationSettingGroupBox.Text = "Notification Setting";
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
            this.tableLayoutPanel9.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BatteryImage)).EndInit();
            this.AppHeader.ResumeLayout(false);
            this.AppHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).EndInit();
            this.AppFooter.ResumeLayout(false);
            this.AppFooter.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BatteryIcon)).EndInit();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.BatteryDetail.ResumeLayout(false);
            this.BatteryDetail.PerformLayout();
            this.Notification.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).EndInit();
            this.tableLayoutPanel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tableLayoutPanel13.ResumeLayout(false);
            this.tableLayoutPanel13.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).EndInit();
            this.tableLayoutPanel12.ResumeLayout(false);
            this.tableLayoutPanel12.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LowBatteryIcon)).EndInit();
            this.NotificationSettingGroupBox.ResumeLayout(false);
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
        private TableLayoutPanel tableLayoutPanel9;
        private TableLayoutPanel AppHeader;
        private Label AppHeaderTitle;
        private PictureBox CloseIcon;
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
        private Label ViewSourceLabel;
        private System.Windows.Forms.Timer BatteryStatusTimer;
        private System.Windows.Forms.Timer ShowNotificationTimer;
        private Panel AppFooter;
        private Label SettingLabel;
        private Label VersionLabel;
        private Label CheckingForUpdateLabel;
        private FlowLayoutPanel flowLayoutPanel1;
        private PictureBox BatteryImage;
        private Label BatteryPercentage;
        private Label BatteryStatus;
        private Label RemainingTime;
        private GroupBox NotificationSettingGroupBox;
        private TableLayoutPanel tableLayoutPanel12;
        private PictureBox LowBatteryIcon;
        private CheckBox LowBatteryNotificationCheckbox;
        private Label LowBatteryLabel;
        private TableLayoutPanel tableLayoutPanel13;
        private PictureBox FullBatteryIcon;
        private CheckBox FullBatteryNotificationCheckbox;
        private Label FullBatteryLabel;
    }
}