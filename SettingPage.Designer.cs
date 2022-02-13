namespace BatteryNotifier
{
    partial class SettingPage
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.SettingHeader = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.AppHeaderTitle = new System.Windows.Forms.Label();
            this.CloseIcon = new System.Windows.Forms.PictureBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.FullBatteryIcon = new System.Windows.Forms.PictureBox();
            this.panel5 = new System.Windows.Forms.Panel();
            this.lowBatteryPercentageValue = new System.Windows.Forms.NumericUpDown();
            this.browseLowBatterySoundButton = new System.Windows.Forms.Button();
            this.lowBatterySoundPath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lowBatteryTrackbar = new System.Windows.Forms.TrackBar();
            this.label5 = new System.Windows.Forms.Label();
            this.showLowBatteryNotification = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.fullbatteryPercentageValue = new System.Windows.Forms.NumericUpDown();
            this.browseFullBatterySoundButton = new System.Windows.Forms.Button();
            this.fullbatterySoundPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.fullBatteryTrackbar = new System.Windows.Forms.TrackBar();
            this.label2 = new System.Windows.Forms.Label();
            this.showFullBatteryNotification = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.BatteryCapacityLabel = new System.Windows.Forms.Label();
            this.ShowAsWindow = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.SettingHeader.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).BeginInit();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).BeginInit();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryPercentageValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryTrackbar)).BeginInit();
            this.panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fullbatteryPercentageValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullBatteryTrackbar)).BeginInit();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.White;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.SettingHeader, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(1, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(408, 488);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // SettingHeader
            // 
            this.SettingHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(212)))), ((int)(((byte)(144)))));
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
            this.AppHeaderTitle.Font = new System.Drawing.Font("Oswald SemiBold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
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
            this.CloseIcon.Image = global::BatteryNotifier.Properties.Resources.CloseIcon;
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
            this.panel2.Controls.Add(this.pictureBox1);
            this.panel2.Controls.Add(this.FullBatteryIcon);
            this.panel2.Controls.Add(this.panel5);
            this.panel2.Controls.Add(this.panel4);
            this.panel2.Controls.Add(this.panel3);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 40);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(408, 448);
            this.panel2.TabIndex = 1;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.SystemColors.Menu;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Image = global::BatteryNotifier.Properties.Resources.Low;
            this.pictureBox1.Location = new System.Drawing.Point(16, 246);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(65, 36);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 26;
            this.pictureBox1.TabStop = false;
            // 
            // FullBatteryIcon
            // 
            this.FullBatteryIcon.BackColor = System.Drawing.SystemColors.Menu;
            this.FullBatteryIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.FullBatteryIcon.Image = global::BatteryNotifier.Properties.Resources.Full;
            this.FullBatteryIcon.Location = new System.Drawing.Point(16, 56);
            this.FullBatteryIcon.Name = "FullBatteryIcon";
            this.FullBatteryIcon.Size = new System.Drawing.Size(65, 36);
            this.FullBatteryIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.FullBatteryIcon.TabIndex = 25;
            this.FullBatteryIcon.TabStop = false;
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.SystemColors.Menu;
            this.panel5.Controls.Add(this.lowBatteryPercentageValue);
            this.panel5.Controls.Add(this.browseLowBatterySoundButton);
            this.panel5.Controls.Add(this.lowBatterySoundPath);
            this.panel5.Controls.Add(this.label4);
            this.panel5.Controls.Add(this.lowBatteryTrackbar);
            this.panel5.Controls.Add(this.label5);
            this.panel5.Controls.Add(this.showLowBatteryNotification);
            this.panel5.Controls.Add(this.label6);
            this.panel5.Location = new System.Drawing.Point(12, 266);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(383, 154);
            this.panel5.TabIndex = 24;
            // 
            // lowBatteryPercentageValue
            // 
            this.lowBatteryPercentageValue.Location = new System.Drawing.Point(325, 69);
            this.lowBatteryPercentageValue.Name = "lowBatteryPercentageValue";
            this.lowBatteryPercentageValue.ReadOnly = true;
            this.lowBatteryPercentageValue.Size = new System.Drawing.Size(48, 23);
            this.lowBatteryPercentageValue.TabIndex = 25;
            // 
            // browseLowBatterySoundButton
            // 
            this.browseLowBatterySoundButton.Location = new System.Drawing.Point(298, 112);
            this.browseLowBatterySoundButton.Name = "browseLowBatterySoundButton";
            this.browseLowBatterySoundButton.Size = new System.Drawing.Size(75, 23);
            this.browseLowBatterySoundButton.TabIndex = 23;
            this.browseLowBatterySoundButton.Text = "Browse";
            this.browseLowBatterySoundButton.UseVisualStyleBackColor = true;
            this.browseLowBatterySoundButton.Click += new System.EventHandler(this.browseLowBatterySoundButton_Click);
            // 
            // lowBatterySoundPath
            // 
            this.lowBatterySoundPath.Location = new System.Drawing.Point(136, 112);
            this.lowBatterySoundPath.Name = "lowBatterySoundPath";
            this.lowBatterySoundPath.Size = new System.Drawing.Size(156, 23);
            this.lowBatterySoundPath.TabIndex = 22;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Oswald", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label4.Location = new System.Drawing.Point(4, 112);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(120, 27);
            this.label4.TabIndex = 21;
            this.label4.Text = "Full Battery Sound";
            // 
            // lowBatteryTrackbar
            // 
            this.lowBatteryTrackbar.LargeChange = 10;
            this.lowBatteryTrackbar.Location = new System.Drawing.Point(136, 62);
            this.lowBatteryTrackbar.Maximum = 100;
            this.lowBatteryTrackbar.Name = "lowBatteryTrackbar";
            this.lowBatteryTrackbar.Size = new System.Drawing.Size(175, 45);
            this.lowBatteryTrackbar.TabIndex = 20;
            this.lowBatteryTrackbar.Scroll += new System.EventHandler(this.lowBatteryTrackbar_Scroll);
            this.lowBatteryTrackbar.ValueChanged += new System.EventHandler(this.lowBatteryTrackbar_ValueChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Font = new System.Drawing.Font("Oswald", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label5.Location = new System.Drawing.Point(4, 62);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(126, 27);
            this.label5.TabIndex = 19;
            this.label5.Text = "Battery Percentage";
            // 
            // showLowBatteryNotification
            // 
            this.showLowBatteryNotification.AutoSize = true;
            this.showLowBatteryNotification.Location = new System.Drawing.Point(362, 32);
            this.showLowBatteryNotification.Name = "showLowBatteryNotification";
            this.showLowBatteryNotification.Size = new System.Drawing.Size(15, 14);
            this.showLowBatteryNotification.TabIndex = 18;
            this.showLowBatteryNotification.UseVisualStyleBackColor = true;
            this.showLowBatteryNotification.CheckedChanged += new System.EventHandler(this.showLowBatteryNotification_CheckedChanged);
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Oswald", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label6.Location = new System.Drawing.Point(4, 23);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(193, 27);
            this.label6.TabIndex = 18;
            this.label6.Text = "Show Full Battery Notification";
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.SystemColors.Menu;
            this.panel4.Controls.Add(this.fullbatteryPercentageValue);
            this.panel4.Controls.Add(this.browseFullBatterySoundButton);
            this.panel4.Controls.Add(this.fullbatterySoundPath);
            this.panel4.Controls.Add(this.label3);
            this.panel4.Controls.Add(this.fullBatteryTrackbar);
            this.panel4.Controls.Add(this.label2);
            this.panel4.Controls.Add(this.showFullBatteryNotification);
            this.panel4.Controls.Add(this.label1);
            this.panel4.Location = new System.Drawing.Point(12, 74);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(383, 154);
            this.panel4.TabIndex = 19;
            // 
            // fullbatteryPercentageValue
            // 
            this.fullbatteryPercentageValue.Location = new System.Drawing.Point(325, 65);
            this.fullbatteryPercentageValue.Name = "fullbatteryPercentageValue";
            this.fullbatteryPercentageValue.ReadOnly = true;
            this.fullbatteryPercentageValue.Size = new System.Drawing.Size(48, 23);
            this.fullbatteryPercentageValue.TabIndex = 24;
            // 
            // browseFullBatterySoundButton
            // 
            this.browseFullBatterySoundButton.Location = new System.Drawing.Point(298, 112);
            this.browseFullBatterySoundButton.Name = "browseFullBatterySoundButton";
            this.browseFullBatterySoundButton.Size = new System.Drawing.Size(75, 23);
            this.browseFullBatterySoundButton.TabIndex = 23;
            this.browseFullBatterySoundButton.Text = "Browse";
            this.browseFullBatterySoundButton.UseVisualStyleBackColor = true;
            this.browseFullBatterySoundButton.Click += new System.EventHandler(this.browseFullBatterySoundButton_Click);
            // 
            // fullbatterySoundPath
            // 
            this.fullbatterySoundPath.Location = new System.Drawing.Point(136, 112);
            this.fullbatterySoundPath.Name = "fullbatterySoundPath";
            this.fullbatterySoundPath.Size = new System.Drawing.Size(156, 23);
            this.fullbatterySoundPath.TabIndex = 22;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Oswald", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label3.Location = new System.Drawing.Point(4, 112);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(120, 27);
            this.label3.TabIndex = 21;
            this.label3.Text = "Full Battery Sound";
            // 
            // fullBatteryTrackbar
            // 
            this.fullBatteryTrackbar.LargeChange = 10;
            this.fullBatteryTrackbar.Location = new System.Drawing.Point(136, 62);
            this.fullBatteryTrackbar.Maximum = 100;
            this.fullBatteryTrackbar.Name = "fullBatteryTrackbar";
            this.fullBatteryTrackbar.Size = new System.Drawing.Size(183, 45);
            this.fullBatteryTrackbar.TabIndex = 20;
            this.fullBatteryTrackbar.Scroll += new System.EventHandler(this.fullBatteryTrackbar_Scroll);
            this.fullBatteryTrackbar.ValueChanged += new System.EventHandler(this.fullBatteryTrackbar_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Oswald", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label2.Location = new System.Drawing.Point(2, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(126, 27);
            this.label2.TabIndex = 19;
            this.label2.Text = "Battery Percentage";
            // 
            // showFullBatteryNotification
            // 
            this.showFullBatteryNotification.AutoSize = true;
            this.showFullBatteryNotification.Location = new System.Drawing.Point(358, 31);
            this.showFullBatteryNotification.Name = "showFullBatteryNotification";
            this.showFullBatteryNotification.Size = new System.Drawing.Size(15, 14);
            this.showFullBatteryNotification.TabIndex = 18;
            this.showFullBatteryNotification.UseVisualStyleBackColor = true;
            this.showFullBatteryNotification.CheckedChanged += new System.EventHandler(this.showFullBatteryNotification_CheckedChanged);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Oswald", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(4, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(193, 27);
            this.label1.TabIndex = 18;
            this.label1.Text = "Show Full Battery Notification";
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.SystemColors.Menu;
            this.panel3.Controls.Add(this.BatteryCapacityLabel);
            this.panel3.Controls.Add(this.ShowAsWindow);
            this.panel3.Location = new System.Drawing.Point(12, 12);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(383, 34);
            this.panel3.TabIndex = 18;
            // 
            // BatteryCapacityLabel
            // 
            this.BatteryCapacityLabel.AutoSize = true;
            this.BatteryCapacityLabel.Font = new System.Drawing.Font("Oswald", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.BatteryCapacityLabel.Location = new System.Drawing.Point(3, 3);
            this.BatteryCapacityLabel.Name = "BatteryCapacityLabel";
            this.BatteryCapacityLabel.Size = new System.Drawing.Size(107, 27);
            this.BatteryCapacityLabel.TabIndex = 16;
            this.BatteryCapacityLabel.Text = "Show as window";
            // 
            // ShowAsWindow
            // 
            this.ShowAsWindow.AutoSize = true;
            this.ShowAsWindow.Location = new System.Drawing.Point(360, 10);
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
            this.ClientSize = new System.Drawing.Size(410, 490);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SettingPage";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "SettingPage";
            this.Activated += new System.EventHandler(this.SettingPage_Activated);
            this.Load += new System.EventHandler(this.SettingPage_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.SettingHeader.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).EndInit();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FullBatteryIcon)).EndInit();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryPercentageValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowBatteryTrackbar)).EndInit();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fullbatteryPercentageValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fullBatteryTrackbar)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private Panel panel1;
        private PictureBox CloseIcon;
        private Label AppHeaderTitle;
        private TableLayoutPanel SettingHeader;
        private Panel panel2;
        private Label BatteryCapacityLabel;
        private Panel panel4;
        private TrackBar fullBatteryTrackbar;
        private Label label2;
        private CheckBox showFullBatteryNotification;
        private Label label1;
        private Panel panel3;
        private CheckBox ShowAsWindow;
        private Panel panel5;
        private Button browseLowBatterySoundButton;
        private TextBox lowBatterySoundPath;
        private Label label4;
        private TrackBar lowBatteryTrackbar;
        private Label label5;
        private CheckBox showLowBatteryNotification;
        private Label label6;
        private Button browseFullBatterySoundButton;
        private TextBox fullbatterySoundPath;
        private Label label3;
        private PictureBox pictureBox1;
        private PictureBox FullBatteryIcon;
        private NumericUpDown lowBatteryPercentageValue;
        private NumericUpDown fullbatteryPercentageValue;
    }
}