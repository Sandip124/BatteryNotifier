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
            this.tableLayoutPanel1.SuspendLayout();
            this.SettingHeader.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.White;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.SettingHeader, 0, 0);
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
            this.AppHeaderTitle.Text = "Setting";
            this.AppHeaderTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            // SettingPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.ClientSize = new System.Drawing.Size(410, 490);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SettingPage";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.Text = "SettingPage";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.SettingHeader.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.CloseIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private Panel panel1;
        private PictureBox CloseIcon;
        private Label AppHeaderTitle;
        private TableLayoutPanel SettingHeader;
    }
}