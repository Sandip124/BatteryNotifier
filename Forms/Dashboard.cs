﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BatteryNotifier.Constants;
using BatteryNotifier.Helpers;
using BatteryNotifier.Properties;
using Squirrel;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Forms
{
    public partial class Dashboard : Form
    {
        private readonly Debouncer.Debouncer _debouncer;
        private readonly System.Windows.Forms.Timer _soundPlayingTimer = new();
        private readonly SoundPlayer _batteryNotification = new(Resources.BatteryFull);

        private Point _lastLocation;
        private bool _mouseDown;

        private const int DefaultMusicPlayingDuration = 15;

        public Dashboard()
        {
            InitializeComponent();
            ApplyTheme();
            RenderFormArea();
            _debouncer = new Debouncer.Debouncer();
        }

        public void SetVersion(string ver)
        {
            VersionLabel.Text = $"v {ver}";
        }

        public void UpdateStatus(string status)
        {
            CheckingForUpdateLabel.Text = status;
        }

        private void CloseIcon_Click(object? sender, EventArgs e)
        {
            Hide();
        }

        private void CloseIcon_MouseEnter(object? sender, EventArgs e)
        {
            CloseIcon.Image = Resources.closeIconHoverState;
            CloseIcon.BackColor = Color.FromArgb(197, 48, 38);
        }

        private void CloseIcon_MouseLeave(object? sender, EventArgs e)
        {
            CloseIcon.BackColor = Color.Transparent;

            if (appSetting.Default.darkThemeApplied)
            {
                CloseIcon.Image = Resources.closeIconDark;
            }
            else
            {
                CloseIcon.Image = Resources.closeIconLight;
            }

        }

        private void Dashboard_Load(object? sender, EventArgs e)
        {
            TryUpdate();            
            this.SuspendLayout();
            RefreshBatteryStatus();
            LoadNotificationSetting();
            BatteryStatusTimer.Enabled = true;
            ShowNotificationTimer.Enabled = true;

            ConfigureTimer();
            AttachEventListeners();

            InitializeContextMenu();
            this.ResumeLayout();
        }

        private void ConfigureTimer()
        {
            _soundPlayingTimer.Enabled = true;
            _soundPlayingTimer.Interval = 1000;

            this.ShowNotificationTimer.Interval = 30000;

        }

        private void AttachEventListeners()
        {

            CloseIcon.Click += CloseIcon_Click;
            CloseIcon.MouseEnter += CloseIcon_MouseEnter;
            CloseIcon.MouseLeave += CloseIcon_MouseLeave;

            ViewSourceLabel.Click += ViewSourceLabel_Click;
            ViewSourceLabel.MouseEnter += ViewSourceLabel_MouseEnter;
            ViewSourceLabel.MouseLeave += ViewSourceLabel_MouseLeave;

            SettingLabel.Click += SettingLabel_Click;
            SettingLabel.MouseEnter += SettingLabel_MouseEnter;
            SettingLabel.MouseLeave += SettingLabel_MouseLeave;

            BatteryStatusTimer.Tick += BatteryStatusTimer_Tick;
            ShowNotificationTimer.Tick += ShowNotificationTimer_Tick;
            _soundPlayingTimer.Tick += SoundPlayingTimer_Tick;

            FullBatteryNotificationCheckbox.CheckedChanged += FullBatteryNotificationCheckbox_CheckStateChanged;
            LowBatteryNotificationCheckbox.CheckedChanged += LowBatteryNotificationCheckbox_CheckStateChanged;

            VersionLabel.Click += VersionLabel_Click;
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
                _batteryNotification.Stream = Resources.BatteryFull;
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

            if (status.PowerLineStatus == PowerLineStatus.Online && status.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery && _isCharging == false)
            {
                _isCharging = true;
                BatteryStatus.Text = "⚡ Charging";
                BatteryStatus.ForeColor = Color.ForestGreen;
                UpdateChargingAnimation();
            }
            else if (status.PowerLineStatus == PowerLineStatus.Offline || status.PowerLineStatus == PowerLineStatus.Unknown)
            {
                _isCharging = false;
                BatteryStatus.Text = "🙄 Not Charging";
                BatteryStatus.ForeColor = Color.Gray;
                SetBatteryChargeStatus(status);

                ResetIsChargingStatus();

            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                _isCharging = false;
                BatteryStatus.Text = "💀 Are you running on main power !!";
                BatteryImage.Image = Resources.Unknown;
                ResetIsChargingStatus();
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.Unknown)
            {
                _isCharging = false;
                BatteryStatus.Text = "😇 Only God knows about this battery !!";
                this.BatteryImage.Image = Resources.Unknown;
                ResetIsChargingStatus();
            }

            UpdateBatteryPercentage(status);
            UpdateBatteryChargeRemainingStatus(status);
        }

        private void ResetIsChargingStatus()
        {
            isRenderingChargingStatusForDarkMode = false;
            isRenderingChargingStatusForLightMode = false;
        }

        bool isRenderingChargingStatusForDarkMode;
        bool isRenderingChargingStatusForLightMode;

        private void UpdateChargingAnimation()
        {
            if (!_isCharging) return;

            if (appSetting.Default.darkThemeApplied)
            {
                if (!isRenderingChargingStatusForDarkMode)
                {
                    BatteryImage.Image = Resources.ChargingBatteryAnimatedDark;
                    isRenderingChargingStatusForDarkMode = true;
                }
            }
            else
            {
                if (!isRenderingChargingStatusForLightMode)
                {
                    BatteryImage.Image = Resources.ChargingBatteryAnimated;
                    isRenderingChargingStatusForLightMode = true;
                }
            }
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
                RemainingTime.Text = $@"{Math.Round(status.BatteryLifePercent * 100, 0)}% remaining";
            }
        }

        private void UpdateBatteryPercentage(PowerStatus status)
        {
            var powerPercent = (int)(status.BatteryLifePercent * 100);
            BatteryPercentage.Text = $@"{(powerPercent <= 100 ? powerPercent.ToString() : "0")}%";
        }

        private void SetBatteryChargeStatus(PowerStatus powerStatus)
        {
            if (_isCharging) return;

            if (powerStatus.BatteryLifePercent >= .96)
            {
                BatteryStatus.Text = "🔋 Full Battery";
                BatteryImage.Image = Resources.Full;
            }
            else if (powerStatus.BatteryLifePercent >= .4 && powerStatus.BatteryLifePercent <= .96)
            {
                BatteryStatus.Text = "🔋 Sufficient Battery";
                BatteryImage.Image = Resources.Normal;
            }
            else if (powerStatus.BatteryLifePercent < .4)
            {
                BatteryStatus.Text = "⚠ Battery Critical";
                BatteryImage.Image = Resources.Critical;
            }
            else if (powerStatus.BatteryLifePercent <= .14)
            {
                BatteryStatus.Text = "🔌 Battery Low";
                BatteryImage.Image = Resources.Low;
            }
        }

        private void RenderFormArea()
        {
            this.RenderFormPosition(appSetting.Default.showAsModal);
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

            ToolStripMenuItem restartAppToolStripItem = new("RestartApplication")
            {
                Text = "Restart Application",
                Name = "RestartApp",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 10.2F)
            };
            restartAppToolStripItem.Click += RestartApp_Click!;

            ToolStripMenuItem viewSourceToolStripItem = new("ViewSource")
            {
                Text = "View Source",
                Name = "ViewSource",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 10.2F)
            };
            viewSourceToolStripItem.Click += ViewSource_Click!;

            contextMenu.Items.Add(viewSourceToolStripItem);
            contextMenu.Items.Add(restartAppToolStripItem);
            contextMenu.Items.Add(exitAppToolStripItem);

            BatteryNotifierIcon.ContextMenuStrip = contextMenu;
            Update();
        }


        private void ExitApp_Click(object sender, EventArgs e)
        {
            Close();
            Dispose();
        }

        private void RestartApp_Click(object sender, EventArgs e)
        {
            Application.Exit();
            Process.Start(Application.ExecutablePath, $"/restart{Process.GetCurrentProcess().Id}");
        }

        private void ViewSource_Click(object sender, EventArgs e)
        {
            ViewDeveloperUrl();
        }

        private static void ViewDeveloperUrl()
        {
            ProcessStartInfo sInfo = new(Constant.SourceUrl)
            {
                UseShellExecute = true
            };
            Process.Start(sInfo);
        }

        private void BatteryNotifierIcon_Click(object? sender, EventArgs e)
        {
            if (this.Visible)
            {
                Hide();
            }
            else
            {
                Show();
                Activate();
            }
        }

        private void BatteryStatusTimer_Tick(object? sender, EventArgs e)
        {
            RefreshBatteryStatus();
        }

        private void FullBatteryNotificationCheckbox_CheckStateChanged(object? sender, EventArgs e)
        {
            RenderCheckboxState(FullBatteryNotificationCheckbox, FullBatteryNotificationCheckbox.Checked);
            appSetting.Default.fullBatteryNotification = FullBatteryNotificationCheckbox.Checked;
            appSetting.Default.Save();
        }

        private void LowBatteryNotificationCheckbox_CheckStateChanged(object? sender, EventArgs e)
        {
            RenderCheckboxState(LowBatteryNotificationCheckbox, LowBatteryNotificationCheckbox.Checked);
            appSetting.Default.lowBatteryNotification = LowBatteryNotificationCheckbox.Checked;
            appSetting.Default.Save();
        }

        private void ShowNotificationTimer_Tick(object? sender, EventArgs e)
        {
            CheckNotification();
        }

        private void OpenSettingPage()
        {

            Hide();
            var settingPage = new SettingPage();
            settingPage.ShowDialog();
            ResetRenderingState();
            ApplyTheme();
            Show();
        }

        private void SettingLabel_Click(object? sender, EventArgs e)
        {
            OpenSettingPage();
        }

        private void ViewSourceLabel_Click(object? sender, EventArgs e)
        {
            ViewDeveloperUrl();
        }

        private void SettingLabel_MouseEnter(object? sender, EventArgs e)
        {
            ApplyThemeForFooterMouseEnter(SettingLabel);
        }

        private void SettingLabel_MouseLeave(object? sender, EventArgs e)
        {
            ApplyThemeForFooterMouseLeave(SettingLabel);
        }

        private void ViewSourceLabel_MouseEnter(object? sender, EventArgs e)
        {
            ApplyThemeForFooterMouseEnter(ViewSourceLabel);
        }

        private void ViewSourceLabel_MouseLeave(object? sender, EventArgs e)
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

        private void Dashboard_Activated(object? sender, EventArgs e)
        {
            BatteryStatusTimer.Start();
            RefreshBatteryStatus();
            LoadNotificationSetting();
            RenderFormArea();
            UpdateChargingAnimation();
        }

        bool isLightThemeRendered = false;
        bool isDarkThemeRendered = false;
        private void ApplyTheme()
        {
            SuspendLayout();
            if (appSetting.Default.darkThemeApplied)
            {
                if (isDarkThemeRendered) return;
                AppContainer.BackColor = Color.FromArgb(30, 30, 30);
                AppHeader.BackColor = Color.Black;
                AppHeaderTitle.ForeColor = Color.White;
                RemainingTime.ForeColor = Color.White;
                BatteryPercentage.ForeColor = Color.White;
                NotificationSettingGroupBox.ForeColor = Color.White;
                AppFooter.BackColor = Color.Black;
                SettingLabel.ForeColor = Color.FromArgb(160, 160, 160);
                ViewSourceLabel.ForeColor = Color.FromArgb(160, 160, 160);
                VersionLabel.ForeColor = Color.FromArgb(160, 160, 160);
                CloseIcon.Image = Resources.closeIconDark;
                CheckingForUpdateLabel.ForeColor = Color.White;
                isDarkThemeRendered = true;
                isLightThemeRendered = false;
            }
            else
            {
                if (isLightThemeRendered) return;
                AppContainer.BackColor = Color.White;
                AppHeader.BackColor = Color.AliceBlue;
                AppHeaderTitle.ForeColor = Color.Black;
                RemainingTime.ForeColor = Color.Black;
                BatteryPercentage.ForeColor = Color.Black;
                NotificationSettingGroupBox.ForeColor = Color.Black;
                AppFooter.BackColor = Color.AliceBlue;
                SettingLabel.ForeColor = Color.Black;
                ViewSourceLabel.ForeColor = Color.Black;
                VersionLabel.ForeColor = Color.Black;
                CloseIcon.Image = Resources.closeIconLight;
                CheckingForUpdateLabel.ForeColor = Color.Black;
                isDarkThemeRendered = false;
                isLightThemeRendered = true;
            }
            ResumeLayout();
        }

        public void ResetRenderingState()
        {
            isDarkThemeRendered = false;
            isLightThemeRendered = false;
        }

        private void AppHeaderTitle_MouseDown(object? sender, MouseEventArgs e)
        {
            if (!appSetting.Default.showAsModal) return;

            _mouseDown = true;
            _lastLocation = e.Location;
        }

        private void AppHeaderTitle_MouseMove(object? sender, MouseEventArgs e)
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

        private void AppHeaderTitle_MouseUp(object? sender, MouseEventArgs e)
        {
            if (appSetting.Default.showAsModal)
            {
                _mouseDown = false;
            }
        }

        private void BatteryNotifierIcon_BalloonTipClicked(object? sender, EventArgs e)
        {
            Show();
            Activate();
        }

        private void BatteryNotifierIcon_BalloonTipClosed(object? sender, EventArgs e)
        {
            _batteryNotification.Stop();
            _soundPlayingTimer.Stop();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {

            if (keyData == (Keys.Escape))
            {
                Close();
                UpdateManager?.Dispose();
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static string version = UtilityHelper.AssemblyVersion;

        public void VersionLabel_Click(object? sender, EventArgs e)
        {
            TryUpdate();
        }

        private void TryUpdate()
        {
#if RELEASE
            Task.Run(() => InitUpdateManager(this)).Wait();
            Task.Run(() => CheckForUpdates(this)).Start();
            version = UpdateManager.CurrentlyInstalledVersion().ToString();
            UpdateStatus("Checking for update ...");
            IsUpdateInProgress = true;
#endif
        }
        

        private static UpdateManager UpdateManager;

        private static bool IsUpdateInProgress = false;


        private async Task InitUpdateManager(Dashboard dashboard)
        {
            try
            {
                UpdateManager = await UpdateManager.GitHubUpdateManager($@"{Constants.Constant.SourceUrl}");
            }
            catch (Exception)
            {
                dashboard?.UpdateStatus("Could not initialize update manager!");
            }
        }

        private async void CheckForUpdates(Dashboard dashboard)
        {
            try
            {
                var updateInfo = await UpdateManager.CheckForUpdate();

                if (!IsUpdateInProgress) return;

                if (updateInfo.ReleasesToApply.Count > 0)
                {
                    var releaseEntry = await UpdateManager.UpdateApp();

                    if (releaseEntry != null)
                    {
                        IsUpdateInProgress = false;
                        dashboard.UpdateStatus($"🔔 Battery Notifier {releaseEntry.Version} downloaded. Restart to apply.");
                    }
                }
                else
                {
                    IsUpdateInProgress = false;
                    dashboard.UpdateStatus("✅ No Update Available");
                }
            }
            catch (Exception)
            {
                dashboard?.UpdateStatus("💀 Could not update app!");
            }
            finally
            {
                Thread.Sleep(5000);
                dashboard.UpdateStatus(string.Empty);
            }
        }
    }
}
