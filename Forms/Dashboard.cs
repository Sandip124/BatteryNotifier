﻿using BatteryNotifier.Helpers;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier
{
    public partial class Dashboard : Form
    {
        const string _developerUrl = "https://github.com/Sandip124/BatteryNotifier/";
        readonly Timer _soundPlayingTimer = new();
        readonly SoundPlayer _batteryNotification = new(Properties.Resources.BatteryFull);

        private Point _lastLocation;
        private bool _mouseDown;

        private const int _defaultMusicPlayingDuration = 15;


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
            ApplyTheme();
            BatteryStatusTimer.Enabled = true;
            ShowNotificationTimer.Enabled = true;

            _soundPlayingTimer.Enabled = true;
            _soundPlayingTimer.Interval = 1000;
            _soundPlayingTimer.Tick += SoundPlayingTimer_Tick;

        }

        private void RenderBatteryInfo()
        {
            // the battery detail is hardcoded for now.
            BatteryCapacity.Text = "4760 mWh";
            CurrentCapacityValue.Text = "3680 mWh";
        }

        readonly CustomTimer _timer = new();

        private void SoundPlayingTimer_Tick(object? sender, EventArgs e)
        {
            if (_timer.TimerCount >= _defaultMusicPlayingDuration)
            {
                _soundPlayingTimer.Stop();
                _batteryNotification.Stop();
                _timer.ResetTimer();
            }
            _timer.Increment();
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

        private static void RenderCheckboxState(Control control, bool showNotification)
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

        private bool _isCharging = false;

        private void CheckNotification()
        {

            PowerStatus status = SystemInformation.PowerStatus;

            var showFullBatteryNotification = appSetting.Default.fullBatteryNotification;

            if (showFullBatteryNotification)
            {
                if (status.PowerLineStatus == PowerLineStatus.Online && _isCharging == true && status.BatteryLifePercent >= (float)appSetting.Default.fullBatteryNotificationValue / 100)
                {
                    BatteryNotifierIcon.ShowBalloonTip(30, "Full Battery", "Battery is full please unplug the charger.", ToolTipIcon.Info);

                    PlayFullBatterySound();
                }
            }

            var showLowBatteryNotification = appSetting.Default.lowBatteryNotification;

            if (showLowBatteryNotification)
            {
                if (status.PowerLineStatus == PowerLineStatus.Offline && _isCharging == false && status.BatteryLifePercent <= (float)appSetting.Default.lowBatteryNotificationValue / 100)
                {

                    BatteryNotifierIcon.ShowBalloonTip(30, "Low Battery", "Battery is low.Please Connect to Charger.", ToolTipIcon.Info);

                    PlayLowBatterySound();
                }
            }
        }


        private void PlaySound(string soundLocation)
        {
            _soundPlayingTimer.Start();

            if (!string.IsNullOrEmpty(soundLocation))
            {
                _batteryNotification.SoundLocation = soundLocation;
            }
            else
            {
                _batteryNotification.Stream = Properties.Resources.BatteryFull;
            }
            _batteryNotification.PlayLooping();
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

            if (status.PowerLineStatus == PowerLineStatus.Online && _isCharging == false)
            {
                _isCharging = true;
                BatteryStatus.Text = "Charging";
                BatteryStatus.ForeColor = Color.ForestGreen;
                UpdateChargingAnimation();

            }
            else if (status.PowerLineStatus is PowerLineStatus.Offline or PowerLineStatus.Unknown)
            {
                _isCharging = false;
                BatteryStatus.Text = "Not Charging";
                BatteryStatus.ForeColor = Color.Gray;
                SetBatteryChargeStatus(status);
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                _isCharging = false;
                BatteryStatus.Text = "Looks like you are running on main power !!";
                this.BatteryImage.Image = Properties.Resources.Unknown;
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.Unknown)
            {
                _isCharging = false;
                BatteryStatus.Text = "Only God knows about this battery !!";
                this.BatteryImage.Image = Properties.Resources.Unknown;
            }

            UpdateBatteryPercentage(status);

            UpdateBatteryChargeRemainingStatus(status);
        }

        private void UpdateChargingAnimation()
        {
            if (appSetting.Default.darkThemeApplied)
            {
                this.BatteryImage.Image = Properties.Resources.ChargingBatteryAnimatedDark;
            }
            else
            {
                this.BatteryImage.Image = Properties.Resources.ChargingBatteryAnimated;
            }
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
                RemainingTime.Text = status.BatteryLifePercent * 100 + " % remaining";
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
            ProcessStartInfo sInfo = new(_developerUrl);
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

        private void OpenSettingPage()
        {
            Hide();
            var settingPage = new SettingPage();
            settingPage.ShowDialog();
            Show();
            UpdateChargingAnimation();
        }

        private void label9_Click(object sender, EventArgs e)
        {
            OpenSettingPage();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new(_developerUrl);
            sInfo.UseShellExecute = true;
            Process.Start(sInfo);
        }

        private void label9_MouseEnter(object sender, EventArgs e)
        {
            ApplyThemeForFooterMouseEnter(SettingLabel);            
        }

        private void label9_MouseLeave(object sender, EventArgs e)
        {
            ApplyThemeForFooterMouseLeave(SettingLabel);
        }

        private void label1_MouseEnter(object sender, EventArgs e)
        {
            ApplyThemeForFooterMouseEnter(ViewSourceLabel);
        }

        private void label1_MouseLeave(object sender, EventArgs e)
        {
            ApplyThemeForFooterMouseLeave(ViewSourceLabel);
        }

        private void ApplyThemeForFooterMouseEnter(Control control)
        {
            var label = (control as Label)!;
            if (appSetting.Default.darkThemeApplied)
            {
                label.ForeColor = Color.White;
            }
            else
            {
                label.ForeColor = Color.Black;
            }
        }

        private void ApplyThemeForFooterMouseLeave(Control control)
        {
            var label = (control as Label)!;
            if (appSetting.Default.darkThemeApplied)
            {
                label.ForeColor = Color.FromArgb(160, 160, 160);
            }
            else
            {
                label.ForeColor = Color.FromArgb(50, 50, 50);
            }
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
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (appSetting.Default.darkThemeApplied)
            {
                AppContainer.BackColor = Color.FromArgb(30, 30, 30);
                AppHeader.BackColor = Color.Black;
                AppHeaderTitle.ForeColor = Color.White;
                BatteryPercentage.ForeColor = Color.White;
                BatteryDetailGroupBox.ForeColor = Color.White;
                NotificationSettingGroupBox.ForeColor = Color.White;
                AppFooter.BackColor = Color.Black;
                SettingLabel.ForeColor = Color.FromArgb(160,160,160);
                ViewSourceLabel.ForeColor = Color.FromArgb(160, 160, 160);
            }
            else
            {
                AppContainer.BackColor = Color.White;
                AppHeader.BackColor = Color.AliceBlue;
                AppHeaderTitle.ForeColor = Color.Black;
                BatteryPercentage.ForeColor = Color.Black;
                BatteryDetailGroupBox.ForeColor = Color.Black;
                NotificationSettingGroupBox.ForeColor = Color.Black;
                AppFooter.BackColor = Color.AliceBlue;
                SettingLabel.ForeColor = Color.Black;
                ViewSourceLabel.ForeColor = Color.Black;
            }
        }

        private void AppHeaderTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (appSetting.Default.showAsModal)
            {
                _mouseDown = true;
                _lastLocation = e.Location;
            }
        }

        private void AppHeaderTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (appSetting.Default.showAsModal)
            {
                if (_mouseDown)
                {
                    var xPosition = Location.X - _lastLocation.X + e.X;
                    var yPosition = Location.Y - _lastLocation.Y + e.Y;
                    Location = new Point(
                        xPosition, yPosition);

                    appSetting.Default.WindowPositionX = xPosition;
                    appSetting.Default.WindowPositionY= yPosition;
                    appSetting.Default.Save();
                    Update();
                }
            }
        }

        private void AppHeaderTitle_MouseUp(object sender, MouseEventArgs e)
        {
            if (appSetting.Default.showAsModal)
            {
                _mouseDown = false;
            }
        }

        private void BatteryNotifierIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private void BatteryNotifierIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            _batteryNotification.Stop();
            _soundPlayingTimer.Stop();
        }
    }
}