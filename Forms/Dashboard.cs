using System;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using BatteryNotifier.Helpers;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Forms
{
    public partial class Dashboard : Form
    {
        private readonly Debouncer.Debouncer _debouncer;
        private const string SourceUrl = "https://github.com/Sandip124/BatteryNotifier/";
        private readonly Timer _soundPlayingTimer = new();
        private readonly SoundPlayer _batteryNotification = new(Properties.Resources.BatteryFull);

        private Point _lastLocation;
        private bool _mouseDown;

        private const int DefaultMusicPlayingDuration = 15;

        public Dashboard()
        {
            InitializeComponent();
            InitializeContextMenu();
            SetDefaultLocation();
            _debouncer = new Debouncer.Debouncer();
        }

        private void CloseIcon_Click(object sender, EventArgs e)
        {
            Hide();
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
            this.SuspendLayout();
            RefreshBatteryStatus();
            LoadNotificationSetting();
            RenderBatteryInfo();
            ApplyTheme();
            BatteryStatusTimer.Enabled = true;
            ShowNotificationTimer.Enabled = true;

            _soundPlayingTimer.Enabled = true;
            _soundPlayingTimer.Interval = 1000;
            _soundPlayingTimer.Tick += SoundPlayingTimer_Tick;

            this.ResumeLayout();
        }

        private void RenderBatteryInfo()
        {
            // the battery detail is hardcoded for now.
            BatteryCapacity.Text = "4760 mWh";
            CurrentCapacityValue.Text = "3680 mWh";
        }

        private readonly CustomTimer _timer = new();

        private void SoundPlayingTimer_Tick(object? sender, EventArgs e)
        {
            if (_timer.TimerCount >= DefaultMusicPlayingDuration)
            {
                _soundPlayingTimer.Stop();
                _batteryNotification.Stop();
                _timer.ResetTimer();
            }
            _timer.Increment();
        }


        public partial class CustomTimer
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

            checkBox.Checked = showNotification;
            checkBox.Text = showNotification ? "On" : "Off";
        }

        private bool _isCharging = false;

        private void CheckNotification()
        {

            var status = SystemInformation.PowerStatus;

            var showFullBatteryNotification = appSetting.Default.fullBatteryNotification;

            if (showFullBatteryNotification)
            {
                if (status.PowerLineStatus == PowerLineStatus.Online && _isCharging && status.BatteryLifePercent >= (float)appSetting.Default.fullBatteryNotificationValue / 100)
                {
                    BatteryNotifierIcon.ShowBalloonTip(30, "Full Battery", "Battery is full please unplug the charger.", ToolTipIcon.Info);

                    PlayFullBatterySound();
                }
            }

            var showLowBatteryNotification = appSetting.Default.lowBatteryNotification;

            if (!showLowBatteryNotification) return;
            if (status.PowerLineStatus != PowerLineStatus.Offline || _isCharging ||
                !(status.BatteryLifePercent <= (float)appSetting.Default.lowBatteryNotificationValue / 100)) return;
            BatteryNotifierIcon.ShowBalloonTip(30, "Low Battery", "Battery is low.Please Connect to Charger.", ToolTipIcon.Info);

            PlayLowBatterySound();
        }


        private void PlaySound(string soundLocation)
        {
            _soundPlayingTimer.Start();

            var notificationSoundAvailable = !string.IsNullOrEmpty(soundLocation);
            
            if (notificationSoundAvailable)
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
            var status = SystemInformation.PowerStatus;

            switch (status.PowerLineStatus)
            {
                case PowerLineStatus.Online when _isCharging == false:
                    _isCharging = true;
                    BatteryStatus.Text = "Charging";
                    BatteryStatus.ForeColor = Color.ForestGreen;
                    UpdateChargingAnimation();
                    break;
                case PowerLineStatus.Offline or PowerLineStatus.Unknown:
                    _isCharging = false;
                    BatteryStatus.Text = "Not Charging";
                    BatteryStatus.ForeColor = Color.Gray;
                    SetBatteryChargeStatus(status);
                    break;
                default:
                {
                    switch (status.BatteryChargeStatus)
                    {
                        case BatteryChargeStatus.NoSystemBattery:
                            _isCharging = false;
                            BatteryStatus.Text = "Looks like you are running on main power !!";
                            BatteryImage.Image = Properties.Resources.Unknown;
                            break;
                        case BatteryChargeStatus.Unknown:
                            _isCharging = false;
                            BatteryStatus.Text = "Only God knows about this battery !!";
                            this.BatteryImage.Image = Properties.Resources.Unknown;
                            break;
                    }

                    break;
                }
            }

            UpdateBatteryPercentage(status);
            UpdateBatteryChargeRemainingStatus(status);
        }

        private void UpdateChargingAnimation()
        {
            BatteryImage.Image = appSetting.Default.darkThemeApplied
                ? Properties.Resources.ChargingBatteryAnimatedDark
                : Properties.Resources.ChargingBatteryAnimated;
        }

        private void UpdateBatteryChargeRemainingStatus(PowerStatus status)
        {
            var secondsRemaining = status.BatteryLifeRemaining;
            if (secondsRemaining >= 0)
            {
                var timeSpan = TimeSpan.FromSeconds(secondsRemaining);
                RemainingTime.Text = $@"{timeSpan.Hours} hr {timeSpan.Minutes} min remaining";
            }
            else
            {
                RemainingTime.Text = $@"{Math.Round(status.BatteryLifePercent * 100,0)} % remaining";
            }
        }

        private void UpdateBatteryPercentage(PowerStatus status)
        {
            var powerPercent = (int)(status.BatteryLifePercent * 100);
            BatteryPercentage.Text = $@"{(powerPercent <= 100 ? powerPercent.ToString() : "0")} %";
        }

        private void SetBatteryChargeStatus(PowerStatus powerStatus)
        {
            if (powerStatus.BatteryLifePercent >= .96)
            {
                BatteryStatus.Text = "Full Battery";
                BatteryImage.Image = Properties.Resources.Full;
            }
            else if (powerStatus.BatteryLifePercent >= .4 && powerStatus.BatteryLifePercent <= .96)
            {
                BatteryStatus.Text = "Sufficient Battery";
                BatteryImage.Image = Properties.Resources.Normal;
            }
            else if (powerStatus.BatteryLifePercent < .4)
            {
                BatteryStatus.Text = "Battery Critical";
                BatteryImage.Image = Properties.Resources.Critical;
            }
            else if (powerStatus.BatteryLifePercent <= .14)
            {
                BatteryStatus.Text = "Battery Low";
                BatteryImage.Image = Properties.Resources.Low;
            }
        }

        private void SetDefaultLocation()
        {
            this.ShowModal(appSetting.Default.showAsModal);
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

            ToolStripMenuItem viewSourceToolStripItem = new("ViewSource")
            {
                Text = "View Source",
                Name = "ViewSource",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 10.2F)
            };
            viewSourceToolStripItem.Click += ViewSource_Click!;

            contextMenu.Items.Add(viewSourceToolStripItem);
            contextMenu.Items.Add(exitAppToolStripItem);

            BatteryNotifierIcon.ContextMenuStrip = contextMenu;
            Update();
        }


        private void ExitApp_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ViewSource_Click(object sender, EventArgs e)
        {
            ViewDeveloperUrl();
        }

        private static void ViewDeveloperUrl()
        {
            ProcessStartInfo sInfo = new(SourceUrl)
            {
                UseShellExecute = true
            };
            Process.Start(sInfo);
        }

        private void BatteryNotifierIcon_Click(object sender, EventArgs e)
        {
            Show();
            Activate();
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
        }

        private void SettingLabel_Click(object sender, EventArgs e)
        {
            OpenSettingPage();
        }

        private void ViewSourceLabel_Click(object sender, EventArgs e)
        {
            ViewDeveloperUrl();
        }

        private void SettingLabel_MouseEnter(object sender, EventArgs e)
        {
            ApplyThemeForFooterMouseEnter(SettingLabel);            
        }

        private void SettingLabel_MouseLeave(object sender, EventArgs e)
        {
            ApplyThemeForFooterMouseLeave(SettingLabel);
        }

        private void ViewSourceLabel_MouseEnter(object sender, EventArgs e)
        {
            ApplyThemeForFooterMouseEnter(ViewSourceLabel);
        }

        private void ViewSourceLabel_MouseLeave(object sender, EventArgs e)
        {
            ApplyThemeForFooterMouseLeave(ViewSourceLabel);
        }

        private static void ApplyThemeForFooterMouseEnter(Control control)
        {
            var label = (control as Label)!;
            label.ForeColor = appSetting.Default.darkThemeApplied ? Color.White : Color.Black;
        }

        private static void ApplyThemeForFooterMouseLeave(Control control)
        {
            var label = (control as Label)!;
            label.ForeColor = appSetting.Default.darkThemeApplied ? Color.FromArgb(160, 160, 160) : Color.FromArgb(50, 50, 50);
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
            BatteryStatusTimer.Start();
            RefreshBatteryStatus();
            LoadNotificationSetting();
            SetDefaultLocation();
            ApplyTheme();
            UpdateChargingAnimation();
        }

        private void ApplyTheme()
        {
            SuspendLayout();
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
            ResumeLayout();
        }

        private void AppHeaderTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (!appSetting.Default.showAsModal) return;
            
            _mouseDown = true;
            _lastLocation = e.Location;
        }

        private void AppHeaderTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!appSetting.Default.showAsModal) return;
            if (!_mouseDown) return;

            var xPosition = Location.X - _lastLocation.X + e.X;
            var yPosition = Location.Y - _lastLocation.Y + e.Y;
            Location = new Point(
                xPosition, yPosition);
            Update();

            _debouncer.Debounce(UpdateLocation, 500);

            void UpdateLocation()
            {
                appSetting.Default.WindowPositionX = xPosition;
                appSetting.Default.WindowPositionY = yPosition;
                appSetting.Default.Save();
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
            Show();
            Activate();
        }

        private void BatteryNotifierIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            _batteryNotification.Stop();
            _soundPlayingTimer.Stop();
        }

        private void Dashboard_Deactivate(object sender, EventArgs e)
        {
            BatteryStatusTimer.Stop();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {

            if (keyData == (Keys.Escape))
            {
                this.Close();
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Dashboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            BatteryStatusTimer.Stop();
            _soundPlayingTimer.Stop();
        }
    }
}
