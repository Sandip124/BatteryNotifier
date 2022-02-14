using BatteryNotifier.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace BatteryNotifier
{
    public partial class Dashboard : Form
    {
        const string DeveloperUrl = "https://github.com/Sandip124/BatteryNotifier/";
        readonly System.Windows.Forms.Timer soundPlayingTimer = new();
        readonly SoundPlayer batteryNotification = new(Properties.Resources.BatteryFull);

        private Point lastLocation;
        private bool mouseDown;

        private const int DefaultMusicPlayingDuration = 15;


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
            RenderBatteryInfo();
            BatteryStatusTimer.Enabled = true;
            ShowNotificationTimer.Enabled = true;

            soundPlayingTimer.Enabled = true;
            soundPlayingTimer.Interval = 1000;
            soundPlayingTimer.Tick += SoundPlayingTimer_Tick;
        }

        private void RenderBatteryInfo()
        {
            BatteryCapacity.Text = "4760 mWh";
            CurrentCapacityValue.Text = "3680 mWh";

        }

        readonly CustomTimer timer = new();

        private void SoundPlayingTimer_Tick(object? sender, EventArgs e)
        { 
            if(timer.TimerCount >= DefaultMusicPlayingDuration)
            {
                soundPlayingTimer.Stop();
                batteryNotification.Stop();
                timer.ResetTimer();
            }
            timer.Increment();
        }


        public class CustomTimer
        {
            public int TimerCount { get; private set; } = 0;
            public void ResetTimer() => TimerCount = 0;
            public void Increment() => TimerCount++;
        }

        private void LoadNotificationSetting()
        {
            var showLowBatteryNotification = appSetting.Default.lowBatteryNotification;
            RenderCheckboxState(LowBatteryNotificationCheckbox, showLowBatteryNotification);

            var showFullBatteryNotification = appSetting.Default.fullBatteryNotification;
            RenderCheckboxState(FullBatteryNotificationCheckbox, showFullBatteryNotification);
        }

        private static void RenderCheckboxState(Control control,bool showNotification)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));

            var checkBox = (control as CheckBox)!;

            if (showNotification)
            {
                checkBox.Checked = true;
                checkBox.Text = "On";
            }
            else
            {
                checkBox.Checked = false;
                checkBox.Text = "Off";
            }
        }

        private bool IsCharging = false;

        private void CheckNotification()
        {

            PowerStatus status = SystemInformation.PowerStatus;

            var showFullBatteryNotification = appSetting.Default.fullBatteryNotification;

            if (showFullBatteryNotification)
            {
                if (status.PowerLineStatus == PowerLineStatus.Online && IsCharging == true && status.BatteryLifePercent >= (float)appSetting.Default.fullBatteryNotificationValue/100)
                {
                    //BrumAlertFactory.OpenAlert("Battery is full please unplug the charger.", Color.Black, Color.Gray, AlertType.Info, 15000, AlertLocation.TopMiddle);
    //                new ToastContentBuilder()
    //.AddArgument("action", "viewConversation")
    //.AddArgument("conversationId", 9813)
    //.AddText("Andrew sent you a picture")
    //.AddText("Check this out, The Enchantments in Washington!")
    //.show();
                    PlayFullBatterySound();
                }
            }


            var showLowBatteryNotification = appSetting.Default.lowBatteryNotification;

            if (showLowBatteryNotification)
            {
                if (status.PowerLineStatus == PowerLineStatus.Offline && IsCharging == false && status.BatteryLifePercent <= (float)appSetting.Default.lowBatteryNotificationValue/100)
                {
                    //BrumAlertFactory.OpenAlert("Please Connect to Charger.", Color.Black, Color.Gray, AlertType.Info, 15000, AlertLocation.TopMiddle);
                    PlayLowBatterySound();
                }
            }
        }


        private void PlaySound(string soundLocation)
        {
            soundPlayingTimer.Start();

            if (!string.IsNullOrEmpty(soundLocation))
            {
                batteryNotification.SoundLocation = soundLocation;
            }
            else
            {
                batteryNotification.Stream = Properties.Resources.BatteryFull;
            }
            batteryNotification.PlayLooping();
        }

        private void PlayFullBatterySound()
        {
            var soundLocation = appSetting.Default.fullBatteryNotificationMusic;
            PlaySound(soundLocation);
        }

        private void PlayLowBatterySound()
        {
            var soundLocation = appSetting.Default.lowBatteryNotificationMusic;
            PlaySound(soundLocation);
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
                RemainingTime.Text = status.BatteryLifePercent*100+ " % remaining";
            }
        }

        private void UpdateBatteryPercentage(PowerStatus status)
        {
            int powerPercent = (int)(status.BatteryLifePercent * 100);
            BatteryPercentage.Text = (powerPercent <= 100 ? powerPercent.ToString() : "0") + " %";
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
            UIHelper.ShowModal(this, appSetting.Default.showAsModal);
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
            RenderCheckboxState(FullBatteryNotificationCheckbox, FullBatteryNotificationCheckbox.Checked);
        }

        private void LowBatteryNotificationCheckbox_CheckStateChanged(object sender, EventArgs e)
        {
            RenderCheckboxState(LowBatteryNotificationCheckbox, LowBatteryNotificationCheckbox.Checked);
        }

        private void ShowNotificationTimer_Tick(object sender, EventArgs e)
        {
            CheckNotification();
        }

        private static void OpenSettingPage()
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

        private void FullBatteryNotificationCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            appSetting.Default.fullBatteryNotification = FullBatteryNotificationCheckbox.Checked;
            appSetting.Default.Save();
        }

        private void LowBatteryNotificationCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            appSetting.Default.lowBatteryNotification = LowBatteryNotificationCheckbox.Checked;
            appSetting.Default.Save();
        }

        private void Dashboard_Activated(object sender, EventArgs e)
        {
            RefreshBatteryStatus();
            LoadNotificationSetting();
            SetDefaultLocation();
        }

        private void AppHeaderTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if(appSetting.Default.showAsModal)
            {
                mouseDown = true;
                lastLocation = e.Location;
            }
        }

        private void AppHeaderTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (appSetting.Default.showAsModal)
            {
                if (mouseDown)
                {
                    Location = new Point(
                        Location.X - lastLocation.X + e.X, Location.Y - lastLocation.Y + e.Y);

                    Update();
                }
            }
        }

        private void AppHeaderTitle_MouseUp(object sender, MouseEventArgs e)
        {
            if (appSetting.Default.showAsModal)
            {
                mouseDown = false;
            }
        }
    }
}
