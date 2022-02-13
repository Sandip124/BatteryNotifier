using BrumCustomAlerts;
using System.Diagnostics;
using System.Media;

namespace BatteryNotifier
{
    public partial class Dashboard : Form
    {
        const string DeveloperUrl = "https://github.com/Sandip124/BatteryNotifier/";

        System.Windows.Forms.Timer soundPlayingTimer = new System.Windows.Forms.Timer();
        SoundPlayer batteryNotification = new SoundPlayer(Properties.Resources.BatteryFull);


        public Dashboard()
        {
            InitializeComponent();
            InitializeContextMenu();
            SetDefaultLocation();

        }

        private void CloseIcon_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void CloseIcon_MouseEnter(object sender, EventArgs e)
        {
            this.CloseIcon.Image = Properties.Resources.CloseIconHover;
        }

        private void CloseIcon_MouseLeave(object sender, EventArgs e)
        {
            this.CloseIcon.Image = Properties.Resources.CloseIcon;
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {
            RefreshBatteryStatus();
            LoadNotificationSetting();
            BatteryStatusTimer.Enabled = true;
            ShowNotificationTimer.Enabled = true;

            soundPlayingTimer.Enabled = true;
            soundPlayingTimer.Interval = 1000;
            soundPlayingTimer.Tick += SoundPlayingTimer_Tick;
        }

        int timerCount = 0;

        private void SoundPlayingTimer_Tick(object? sender, EventArgs e)
        {
            if(timerCount >= 15)
            {
                soundPlayingTimer.Stop();
                batteryNotification.Stop();
                timerCount = 0;
            }
            timerCount++;
        }

        private void LoadNotificationSetting()
        {
             var showLowBatteryNotification = appSetting.Default.lowBatteryNotification;
            if (showLowBatteryNotification)
            {
                LowBatteryNotificationCheckbox.Checked = true;
                LowBatteryNotificationCheckbox.Text = "On";
            }else
            {
                LowBatteryNotificationCheckbox.Checked = false;
                LowBatteryNotificationCheckbox.Text = "Off";
            }


            var showFullBatteryNotification = appSetting.Default.fullBatteryNotification;
            if (showFullBatteryNotification)
            {
                FullBatteryNotificationCheckbox.Checked = true;
                FullBatteryNotificationCheckbox.Text = "On";
            }
            else
            {
                FullBatteryNotificationCheckbox.Checked = false;
                FullBatteryNotificationCheckbox.Text = "Off";
            }
        }

        private bool IsCharging = false;

        private void CheckNotification()
        {
            PowerStatus status = SystemInformation.PowerStatus;

            if (status.PowerLineStatus == PowerLineStatus.Online && IsCharging == true && status.BatteryLifePercent >= .96)
            {
                BrumAlertFactory.OpenAlert("Battery is full please unplug the charger.", Color.Black, Color.Gray, AlertType.Info, 15000, AlertLocation.TopMiddle);
                PlaySound();
            }

            if (status.PowerLineStatus == PowerLineStatus.Offline && IsCharging == false && status.BatteryLifePercent <= .14)
            {
                BrumAlertFactory.OpenAlert("Please Connect to Charger.", Color.Black, Color.Gray, AlertType.Info, 15000, AlertLocation.TopMiddle);
                PlaySound();
            }
        }



        private void PlaySound()
        {
            soundPlayingTimer.Start();
            batteryNotification.PlayLooping();
        }

        private void RefreshBatteryStatus()
        {
            PowerStatus status = SystemInformation.PowerStatus;

            if (status.PowerLineStatus == PowerLineStatus.Online && IsCharging == false)
            {
                IsCharging = true;
                BatteryStatus.Text = "Charging";
                BatteryStatus.ForeColor = Color.ForestGreen;
                this.BatteryImage.Image = Properties.Resources.ChargingBatteryAnimated;
            }
            else if (status.PowerLineStatus is PowerLineStatus.Offline or PowerLineStatus.Unknown)
            {
                IsCharging = false;
                BatteryStatus.Text = "Not Charging";
                BatteryStatus.ForeColor = Color.Gray;
                SetBatteryChargeStatus(status);
            }else if (status.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                IsCharging=false;
                BatteryStatus.Text = "Looks like you are running on main power !!";
                this.BatteryImage.Image = Properties.Resources.Unknown;
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.Unknown)
            {
                IsCharging=false;
                BatteryStatus.Text = "Only God knows about this battery !!";
                this.BatteryImage.Image = Properties.Resources.Unknown;
            }

            UpdateBatteryPercentage(status);

            UpdateBatteryChargeRemainingStatus(status);
        }

        private void UpdateBatteryChargeRemainingStatus(PowerStatus status)
        {
            int secondsRemaining = status.BatteryLifeRemaining;
            if (secondsRemaining >= 0)
            {
                var timeSpan = TimeSpan.FromSeconds(secondsRemaining);
                RemainingTime.Text = string.Format("{0} hr {1} min remaining", timeSpan.Hours, timeSpan.Minutes);
            }
            else
            {
                RemainingTime.Text = "0 min remaining";
            }
        }

        private void UpdateBatteryPercentage(PowerStatus status)
        {
            int powerPercent = (int)(status.BatteryLifePercent * 100);
            if (powerPercent <= 100)
                BatteryPercentage.Text = powerPercent + " %";
            else
                BatteryPercentage.Text = "0 %";
        }

        private void SetBatteryChargeStatus(PowerStatus powerStatus)
        {
            if (powerStatus.BatteryLifePercent >= .96)
            {
                BatteryStatus.Text = "Full Battery";
                this.BatteryImage.Image = Properties.Resources.Full;
            }
            else if (powerStatus.BatteryLifePercent >= .4 && powerStatus.BatteryLifePercent <= .96)
            {
                BatteryStatus.Text = "Sufficient Battery";
                this.BatteryImage.Image = Properties.Resources.Normal;
            }
            else if (powerStatus.BatteryLifePercent < .4)
            {
                BatteryStatus.Text = "Battery Critical";
                this.BatteryImage.Image = Properties.Resources.Critical;
            }
            else if (powerStatus.BatteryLifePercent <= .14)
            {
                BatteryStatus.Text = "Battery Low";
                this.BatteryImage.Image = Properties.Resources.Low;
            }
        }

        private void SetDefaultLocation()
        {
            Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(workingArea.Right - Size.Width,
                                      workingArea.Bottom - Size.Height);
        }

        private void InitializeContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Clear();

            ToolStripMenuItem exitAppToolStripItem = new("ExitApplication")
            {
                Text = "Exit Application",
                Name = "ExitApp",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 10.2F)
            };
            exitAppToolStripItem.Click += ExitApp_Click!;

            ToolStripMenuItem viewDeveloperToolStripItem = new("ViewDevelopers")
            {
                Text = "View Developer",
                Name = "ViewDeveloper",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 10.2F)
            };
            viewDeveloperToolStripItem.Click += ViewDeveloper_Click!;

            contextMenu.Items.Add(viewDeveloperToolStripItem);
            contextMenu.Items.Add(exitAppToolStripItem);

            BatteryNotifierIcon.ContextMenuStrip = contextMenu;
        }


        private void ExitApp_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ViewDeveloper_Click(object sender, EventArgs e)
        {
            ViewDeveloperUrl();
        }

        private static void ViewDeveloperUrl()
        {
            ProcessStartInfo sInfo = new(DeveloperUrl);
            sInfo.UseShellExecute = true;
            Process.Start(sInfo);
        }

        private void BatteryNotifierIcon_Click(object sender, EventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private void BatteryStatusTimer_Tick(object sender, EventArgs e)
        {
            RefreshBatteryStatus();
        }

        private void FullBatteryNotificationCheckbox_CheckStateChanged(object sender, EventArgs e)
        {
            if (FullBatteryNotificationCheckbox.Checked)
            {
                FullBatteryNotificationCheckbox.Text = "On";
            }
            else
            {
                FullBatteryNotificationCheckbox.Text = "Off";
            }
        }

        private void LowBatteryNotificationCheckbox_CheckStateChanged(object sender, EventArgs e)
        {
            if (LowBatteryNotificationCheckbox.Checked)
            {
                LowBatteryNotificationCheckbox.Text = "On";
            }
            else
            {
                LowBatteryNotificationCheckbox.Text = "Off";
            }
        }

        private void Dashboard_FormClosing(object sender, FormClosingEventArgs e)
        {

            appSetting.Default.fullBatteryNotification = FullBatteryNotificationCheckbox.Checked;
            appSetting.Default.lowBatteryNotification = LowBatteryNotificationCheckbox.Checked;
            appSetting.Default.Save();
        }

        private void ShowNotificationTimer_Tick(object sender, EventArgs e)
        {
            CheckNotification();
        }

        private void OpenSettingPage()
        {
            var settingPage = new SettingPage();
            settingPage.ShowDialog();
        }

        private void label9_Click(object sender, EventArgs e)
        {
            OpenSettingPage();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new(DeveloperUrl);
            sInfo.UseShellExecute = true;
            Process.Start(sInfo);
        }

        private void label9_MouseEnter(object sender, EventArgs e)
        {
            label9.ForeColor = Color.Black;
        }

        private void label9_MouseLeave(object sender, EventArgs e)
        {
            label9.ForeColor = Color.FromArgb(30, 30, 30);
        }

        private void label1_MouseEnter(object sender, EventArgs e)
        {
            label1.ForeColor = Color.Black;
        }

        private void label1_MouseLeave(object sender, EventArgs e)
        {
            label1.ForeColor = Color.FromArgb(30, 30, 30);
        }
    }
}
