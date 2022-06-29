using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using BatteryNotifier.Constants;
using BatteryNotifier.Helpers;
using BatteryNotifier.Properties;
using BatteryNotifier.Providers;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Forms
{
    public partial class Dashboard : Form
    {
        private readonly Debouncer.Debouncer _debouncer;
        private readonly Timer _soundPlayingTimer = new();
        private readonly SoundPlayer _batteryNotification = new(Resources.BatteryFull);

        private Point _lastLocation;
        private bool _mouseDown;

        private const int DefaultMusicPlayingDuration = 15;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
                return cp;
            }
        }
        public Dashboard()
        {
            InitializeComponent();
            ApplyTheme();
            ApplyFontStyle();
            this.RenderFormPosition();
            _debouncer = new Debouncer.Debouncer();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        public void SetVersion(string? ver)
        {
            VersionLabel.Text = ver is null ? UtilityHelper.AssemblyVersion : $"v {ver}";
        }

        public void UpdateStatus(string status, int timeout = 3000)
        {
            NotificationText.Text = status;
            _debouncer.Debounce(() =>
            {
                NotificationText.Text = string.Empty;
            }, timeout);
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
            CloseIcon.Image = Resources.closeIconDark;
        }

        private void Dashboard_Load(object? sender, EventArgs e)
        {
            SuspendLayout();
            isFormActivated = true;
            LoadSettings();
            HandleLaunchAtStartup();
            RefreshBatteryStatus();
            LoadNotificationSetting();
            BatteryStatusTimer.Enabled = true;
            ShowNotificationTimer.Enabled = true;

            ConfigureTimer();
            AttachEventListeners();

            InitializeContextMenu();
            ResumeLayout();
        }


        private void LoadSettings()
        {
            PinToNotificationArea.Checked = appSetting.Default.PinToNotificationArea;
            launchAtStartup.Checked = appSetting.Default.LaunchAtStartup;

            fullBatteryTrackbar.Value = appSetting.Default.fullBatteryNotificationValue;
            fullbatteryPercentageValue.Value = appSetting.Default.fullBatteryNotificationValue;

            lowBatteryTrackbar.Value = appSetting.Default.lowBatteryNotificationValue;
            lowBatteryPercentageValue.Value = appSetting.Default.lowBatteryNotificationValue;

            SystemThemeLabel.Checked = appSetting.Default.SystemThemeApplied;
            DarkThemeLabel.Checked = appSetting.Default.darkThemeApplied;
            LightThemeLabel.Checked = !appSetting.Default.darkThemeApplied && (appSetting.Default.SystemThemeApplied && UtilityHelper.IsLightTheme());
        }

        private void HandleLaunchAtStartup()
        {
            var windowsStartupAppsKey = UtilityHelper.GetWindowsStartupAppsKey();
            var startupValue = windowsStartupAppsKey.GetValue(UtilityHelper.AppName);

            if (launchAtStartup.Checked)
            {
                if (startupValue == null)
                {
                    windowsStartupAppsKey.SetValue(UtilityHelper.AppName, Application.ExecutablePath);
                }
            }
            else
            {
                if (startupValue != null)
                {
                    windowsStartupAppsKey.DeleteValue(UtilityHelper.AppName);
                }
            }
            appSetting.Default.LaunchAtStartup = launchAtStartup.Checked;
            appSetting.Default.Save();
        }


        private void LaunchAtStartup_CheckedChanged(object? sender, EventArgs e)
        {
            HandleLaunchAtStartup();
        }

        private void ConfigureTimer()
        {
            _soundPlayingTimer.Enabled = true;
            _soundPlayingTimer.Interval = 1000;

            ShowNotificationTimer.Interval = 50000;

        }

        private void AttachEventListeners()
        {

            CloseIcon.Click += CloseIcon_Click;
            CloseIcon.MouseEnter += CloseIcon_MouseEnter;
            CloseIcon.MouseLeave += CloseIcon_MouseLeave;

            VersionLabel.Click += VersionLabel_Click;

            BatteryStatusTimer.Tick += BatteryStatusTimer_Tick;
            ShowNotificationTimer.Tick += ShowNotificationTimer_Tick;
            _soundPlayingTimer.Tick += SoundPlayingTimer_Tick;

            FullBatteryNotificationCheckbox.CheckedChanged += FullBatteryNotificationCheckbox_CheckStateChanged;
            LowBatteryNotificationCheckbox.CheckedChanged += LowBatteryNotificationCheckbox_CheckStateChanged;

            CloseIcon.Click += new EventHandler(CloseIcon_Click);
            CloseIcon.MouseEnter += new EventHandler(CloseIcon_MouseEnter);
            CloseIcon.MouseLeave += new EventHandler(CloseIcon_MouseLeave);

            AppHeaderTitle.MouseDown += new MouseEventHandler(AppHeaderTitle_MouseDown);
            AppHeaderTitle.MouseMove += new MouseEventHandler(AppHeaderTitle_MouseMove);
            AppHeaderTitle.MouseUp += new MouseEventHandler(AppHeaderTitle_MouseUp);


            lowBatteryTrackbar.Scroll += new EventHandler(LowBatteryTrackbar_Scroll);
            lowBatteryTrackbar.ValueChanged += new EventHandler(LowBatteryTrackbar_ValueChanged);

            fullBatteryTrackbar.Scroll += new EventHandler(FullBatteryTrackbar_Scroll);
            fullBatteryTrackbar.ValueChanged += new EventHandler(FullBatteryTrackbar_ValueChanged);

            PinToNotificationArea.CheckedChanged += new EventHandler(PinToNotificationArea_CheckedChanged);

            launchAtStartup.CheckedChanged += new System.EventHandler(LaunchAtStartup_CheckedChanged);

            SystemThemeLabel.CheckedChanged += new EventHandler(SystemThemeLabel_CheckedChanged);
            DarkThemeLabel.CheckedChanged += new EventHandler(DarkThemeLabel_CheckedChanged);
            LightThemeLabel.CheckedChanged += new EventHandler(LightThemeLabel_CheckedChanged);
        }

        private void LightThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.darkThemeApplied = false;
            appSetting.Default.SystemThemeApplied = false;
            appSetting.Default.Save();
            UpdateChargingAnimation();
            ApplyTheme();
        }

        private void DarkThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.darkThemeApplied = true;
            appSetting.Default.SystemThemeApplied = false;
            appSetting.Default.Save();
            UpdateChargingAnimation();
            ApplyTheme();
        }

        private void SystemThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.darkThemeApplied = false;
            appSetting.Default.SystemThemeApplied = true;
            appSetting.Default.Save();
            UpdateChargingAnimation();
            ApplyTheme();
        }

        private void GithubIcon_Click(object? sender, EventArgs e)
        {
            UtilityHelper.StartExternalUrlProcess(Constant.ReleaseUrl);
        }

        private void FullBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            fullbatteryPercentageValue.Value = fullBatteryTrackbar.Value;
        }

        private void LowBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            lowBatteryPercentageValue.Value = lowBatteryTrackbar.Value;
        }

        private void PinToNotificationArea_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.PinToNotificationArea = PinToNotificationArea.Checked;
            appSetting.Default.Save();
            this.RenderFormPosition();
        }

        private void FullBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _debouncer.Debounce(SaveSetting, 500);

            void SaveSetting()
            {
                appSetting.Default.fullBatteryNotificationValue = fullBatteryTrackbar.Value;
                appSetting.Default.Save();
            }
        }

        private void LowBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _debouncer.Debounce(SaveSetting, 500);
            void SaveSetting()
            {
                appSetting.Default.lowBatteryNotificationValue = lowBatteryTrackbar.Value;
                appSetting.Default.Save();
            }

        }

        bool isFormActivated = false;

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Hide();
            isFormActivated = false;
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

            if (status.PowerLineStatus == PowerLineStatus.Online && _isCharging && status.BatteryLifePercent >= (float)appSetting.Default.fullBatteryNotificationValue / 100)
            {
                var fullBatteryNotificationMessage = "Battery is full please unplug the charger.";

                UpdateStatus(fullBatteryNotificationMessage);

                if (showFullBatteryNotification)
                {

                    BatteryNotifierIcon.ShowBalloonTip(50, "Full Battery", fullBatteryNotificationMessage, ToolTipIcon.Info);

                    PlayFullBatterySound();
                }

                if (isFormActivated) return;
                Show();

            }


            var showLowBatteryNotification = appSetting.Default.lowBatteryNotification;

            if (status.PowerLineStatus != PowerLineStatus.Offline || _isCharging ||
                !(status.BatteryLifePercent <= (float)appSetting.Default.lowBatteryNotificationValue / 100)) return;

            var LowBatteryNotificationMessage = "Battery is low.Please Connect to Charger.";

            UpdateStatus(LowBatteryNotificationMessage);

            if (showLowBatteryNotification)
            {
                BatteryNotifierIcon.ShowBalloonTip(50, "Low Battery", LowBatteryNotificationMessage, ToolTipIcon.Info);

                PlayLowBatterySound();
            }

            if (isFormActivated) return;
            Show();

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
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                _isCharging = false;
                BatteryStatus.Text = "💀 Are you running on main power !!";
                BatteryImage.Image = Resources.Unknown;
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.Unknown)
            {
                _isCharging = false;
                BatteryStatus.Text = "😇 Only God knows about this battery !!";
                BatteryImage.Image = Resources.Unknown;
            }

            UpdateBatteryPercentage(status);
            UpdateBatteryChargeRemainingStatus(status);
        }




        private void UpdateChargingAnimation()
        {
            if (!_isCharging) return;

            if (ThemeProvider.IsDarkTheme)
            {
                BatteryImage.Image = Resources.ChargingBatteryAnimatedDark;
            }
            else
            {
                BatteryImage.Image = Resources.ChargingBatteryAnimated;
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
                BatteryStatus.Text = "🔌 Battery Low";
                BatteryImage.Image = Resources.Critical;
            }
            else if (powerStatus.BatteryLifePercent <= .14)
            {
                BatteryStatus.Text = "⚠ Battery Critical";
                BatteryImage.Image = Resources.Low;
            }
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


        private void ExitApp_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void ViewSource_Click(object? sender, EventArgs e)
        {
            UtilityHelper.StartExternalUrlProcess(Constant.SourceRepositoryUrl);
        }


        private void BatteryNotifierIcon_Click(object? sender, EventArgs e)
        {
            if (Visible)
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

            UpdateStatus("🔔 Full Battery Notification " + (FullBatteryNotificationCheckbox.Checked ? "Enabled" : "Disabled"));
        }

        private void LowBatteryNotificationCheckbox_CheckStateChanged(object? sender, EventArgs e)
        {
            RenderCheckboxState(LowBatteryNotificationCheckbox, LowBatteryNotificationCheckbox.Checked);
            appSetting.Default.lowBatteryNotification = LowBatteryNotificationCheckbox.Checked;
            appSetting.Default.Save();

            UpdateStatus("🔔 Low Battery Notification " + (LowBatteryNotificationCheckbox.Checked ? "Enabled" : "Disabled"));
        }

        private void ShowNotificationTimer_Tick(object? sender, EventArgs e)
        {
            CheckNotification();
        }

        private void VersionLabel_Click(object? sender, EventArgs e)
        {
            UtilityHelper.StartExternalUrlProcess(Constant.ReleaseUrl);
        }

        private void Dashboard_Activated(object? sender, EventArgs e)
        {
            isFormActivated = true;
            BatteryStatusTimer.Start();
            RefreshBatteryStatus();
            LoadNotificationSetting();
            this.RenderFormPosition();
            UpdateChargingAnimation();
        }

        private void ApplyFontStyle()
        {
            AppHeaderTitle.Font = FontProvider.GetBoldFont(12);
            BatteryPercentage.Font = FontProvider.GetBoldFont(BatteryPercentage.Font.Size);
            BatteryStatus.Font = FontProvider.GetRegularFont(BatteryStatus.Font.Size);
            RemainingTime.Font = FontProvider.GetRegularFont(RemainingTime.Font.Size);
            AppTabControl.Font = FontProvider.GetRegularFont(AppTabControl.Font.Size);
            NotificationSettingGroupBox.Font = FontProvider.GetRegularFont(NotificationSettingGroupBox.Font.Size);
            FullBatteryNotificationCheckbox.Font = FontProvider.GetRegularFont(FullBatteryNotificationCheckbox.Font.Size);
            LowBatteryNotificationCheckbox.Font = FontProvider.GetRegularFont(LowBatteryNotificationCheckbox.Font.Size);
            VersionLabel.Font = FontProvider.GetRegularFont(VersionLabel.Font.Size);

            PinToNotificationAreaLabel.Font = FontProvider.GetRegularFont(PinToNotificationAreaLabel.Font.Size);
            LaunchAtStartUpLabel.Font = FontProvider.GetRegularFont(LaunchAtStartUpLabel.Font.Size);
            ThemeLabel.Font = FontProvider.GetRegularFont(ThemeLabel.Font.Size);
            SystemThemeLabel.Font = FontProvider.GetRegularFont(SystemThemeLabel.Font.Size);
            LightThemeLabel.Font = FontProvider.GetRegularFont(LightThemeLabel.Font.Size);
            DarkThemeLabel.Font = FontProvider.GetRegularFont(DarkThemeLabel.Font.Size);
            NotificationGroupBox.Font = FontProvider.GetRegularFont(NotificationGroupBox.Font.Size);
        }

        private void ApplyTheme()
        {

            SuspendLayout();

            var theme = ThemeProvider.GetTheme();

            AppContainer.BackColor = theme.AccentColor;
            AppTabControl.MyBackColor = theme.AccentColor;
            AppTabControl.MyBorderColor = theme.Accent2Color;
            DashboardTab.BackColor = theme.AccentColor;
            SettingTab.BackColor = theme.AccentColor;
            DashboardTab.ForeColor = theme.ForegroundColor;
            SettingTab.ForeColor = theme.ForegroundColor;
            NotificationGroupBox.BackColor = theme.AccentColor;

            RemainingTime.ForeColor = theme.ForegroundColor;
            BatteryPercentage.ForeColor = theme.ForegroundColor;
            NotificationSettingGroupBox.ForeColor = theme.ForegroundColor;
            FullBatteryLabel.ForeColor = theme.ForegroundColor;
            LowBatteryLabel.ForeColor = theme.ForegroundColor;

            AppFooter.BackColor = theme.AccentColor;
            VersionLabel.ForeColor = theme.ForegroundColor;

            NotificationText.ForeColor = theme.ForegroundColor;

            ShowAsWindowPanel.BackColor = theme.Accent2Color;
            LaunchAtStartupPanel.BackColor = theme.Accent2Color;
            ThemeConfigurationPanel.BackColor = theme.Accent2Color;

            ThemePanel.BackColor = theme.Accent2Color;
            ThemePanel.ForeColor = theme.ForegroundColor;

            SystemThemeLabel.ForeColor = theme.ForegroundColor;
            LightThemeLabel.ForeColor = theme.ForegroundColor;
            DarkThemeLabel.ForeColor = theme.ForegroundColor;

            NotificationGroupBox.ForeColor = theme.ForegroundColor;
            ShowFullBatteryNotificationLabel.ForeColor = theme.ForegroundColor;

            PinToNotificationAreaLabel.ForeColor = theme.ForegroundColor;
            LaunchAtStartUpLabel.ForeColor = theme.ForegroundColor;

            fullBatteryTrackbar.BackColor = theme.AccentColor;
            lowBatteryTrackbar.BackColor = theme.AccentColor;

            BatteryPercentageLabel.ForeColor = theme.ForegroundColor;

            LowBatteryNotificationLabel.ForeColor = theme.ForegroundColor;
            LowBatteryPercentageLabel.ForeColor = theme.ForegroundColor;

            lowBatteryPercentageValue.BackColor = theme.AccentColor;
            lowBatteryPercentageValue.ForeColor = theme.ForegroundColor;
            fullbatteryPercentageValue.BackColor = theme.AccentColor;
            fullbatteryPercentageValue.ForeColor = theme.ForegroundColor;

            CloseIcon.Image = Resources.closeIconDark;


            ResumeLayout();
        }

        private void AppHeaderTitle_MouseDown(object? sender, MouseEventArgs e)
        {
            if (appSetting.Default.PinToNotificationArea) return;

            _mouseDown = true;
            _lastLocation = e.Location;
        }

        private void AppHeaderTitle_MouseMove(object? sender, MouseEventArgs e)
        {
            if (appSetting.Default.PinToNotificationArea) return;
            if (!_mouseDown) return;

            var xPosition = Location.X - _lastLocation.X + e.X;
            var yPosition = Location.Y - _lastLocation.Y + e.Y;
            Location = new Point(xPosition, yPosition);
            Update();

            _debouncer.Debounce(UpdateLocation, 1000);

            void UpdateLocation()
            {
                appSetting.Default.WindowPositionX = xPosition;
                appSetting.Default.WindowPositionY = yPosition;
                appSetting.Default.Save();
            }
        }

        private void AppHeaderTitle_MouseUp(object? sender, MouseEventArgs e)
        {
            if (appSetting.Default.PinToNotificationArea) return;
            _mouseDown = false;
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
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
