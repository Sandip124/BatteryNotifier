using System;
using System.Windows.Forms;
using BatteryNotifier.Constants;
using BatteryNotifier.Lib.Manager;
using BatteryNotifier.Properties;
using BatteryNotifier.Utils;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Forms
{
    public partial class Dashboard : Form
    {
        private BatteryManager _batteryManager;
        private NotificationManager _notificationManager;
        private ThemeManager _themeManager;
        private SettingsManager _settingsManager;
        private SoundManager _soundManager;
        private WindowManager _windowManager;
        private ContextMenuManager _contextMenuManager;

        private const int BATTERY_STATUS_TIMER_INTERVAL = 5000; // 5 seconds instead of frequent updates
        private const int NOTIFICATION_CHECK_INTERVAL = 30000; // 30 seconds
        
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= 0x20000; // WS_MINIMIZEBOX
                //cp.ClassStyle |= 0x8; // CS_DBLCLKS
                return cp;
            }
        }

        public Dashboard()
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            InitializeManagers();
        }

        private void InitializeManagers()
        {
            _batteryManager = new BatteryManager(this);
            _soundManager = new SoundManager();
            _notificationManager = new NotificationManager(_soundManager);

            var backAccentControls = new Control[]
            {
                AppContainer,AppTabControl, DashboardTab, SettingTab,
                AppFooter, NotificationSettingPanel,
                fullBatteryTrackbar, lowBatteryTrackbar,
                FullBatteryPictureBox, LowBatteryPictureBox
            };

            var backAccent2Controls = new Control[]
            {
                ShowAsWindowPanel, LaunchAtStartupPanel, ThemeConfigurationPanel,
                ThemePanel, FullBatteryNotificationPanel, LowBatteryNotificationPanel,
                SettingHeader, NotificationSettingLabel,
                FullBatterySound, LowBatterySound
            };

            var backAccent3Controls = new Control[]
            {
                PinToNotificationAreaPictureBox, ThemePictureBox, LaunchAtStartUpPictureBox
            };

            var foreControls = new Control[]
            {
                DashboardTab, SettingTab, RemainingTime, BatteryPercentage,
                FullBatteryLabel, LowBatteryLabel, VersionLabel,
                NotificationText, ThemePanel, SystemThemeLabel,
                LightThemeLabel, DarkThemeLabel, NotificationPanel,
                FullBatteryNotificationPercentageLabel, FullBatterySound,
                LowBatterySound, PinToNotificationAreaLabel, LaunchAtStartUpLabel,
                BatteryPercentageLabel, LowBatteryNotificationPercentageLabel
            };
            
            _themeManager = new ThemeManager(this)
                .RegisterAccentControls(backAccentControls)
                .RegisterAccent2Controls(backAccent2Controls)
                .RegisterAccent3Controls(backAccent3Controls)
                .RegisterForegroundControls(foreControls);

            NotificationPanel.BorderStyle = BorderStyle.FixedSingle;

            _settingsManager = new SettingsManager();
            _windowManager = new WindowManager(this);
            _contextMenuManager = new ContextMenuManager(_notificationManager, this);
        }

        public void SetVersion(string? ver)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string?>(SetVersion), ver);
                return;
            }
            
            VersionLabel.Text = ver is null ? UtilityHelper.AssemblyVersion : $"v {ver}";
        }

        public void Notify(string status, int timeout = 3000)
        {
            _notificationManager.ShowInAppNotification(NotificationText, status, timeout);
        }

        private void Dashboard_Load(object? sender, EventArgs e)
        {
            if (appSetting.Default.startMinimized) Hide();

            SuspendLayout();
            try
            {
                _windowManager.RenderFormPosition(BatteryNotifierIcon);
                _themeManager.ApplyTheme(ThemePictureBox, CloseIcon);
                _windowManager.RenderTitleBarCursor(AppHeaderTitle);
                ApplyFontStyle();
                _settingsManager.LoadCheckboxSettings(PinToNotificationArea, launchAtStartup)
                    .LoadTrackbarSettings(fullBatteryTrackbar, lowBatteryTrackbar, FullBatteryNotificationPercentageLabel, LowBatteryNotificationPercentageLabel)
                    .LoadSoundSettings(FullBatterySound, LowBatterySound)
                    .LoadThemeSettings(SystemThemeLabel, DarkThemeLabel, LightThemeLabel)
                    .LoadNotificationSettings(FullBatteryNotificationCheckbox, LowBatteryNotificationCheckbox)
                    .HandleStartupLaunchSetting(launchAtStartup);
                _batteryManager.RefreshBatteryStatus(BatteryStatus, BatteryPercentage, RemainingTime, BatteryImage);

                ConfigureTimers();
                AttachEventListeners();

                _contextMenuManager.AttachContextMenu(BatteryNotifierIcon, NotificationSettingLabel);
            }
            catch (Exception ex)
            {
                Notify(ex.Message);
            }
            finally
            {
                ResumeLayout(false);
            }
        }

        private void ConfigureTimers()
        {
            BatteryStatusTimer.Interval = BATTERY_STATUS_TIMER_INTERVAL;
            BatteryStatusTimer.Enabled = true;
            
            ShowNotificationTimer.Interval = NOTIFICATION_CHECK_INTERVAL;
            ShowNotificationTimer.Enabled = true;
        }

        private void AttachEventListeners()
        {
            // Form events
            Activated += Dashboard_Activated;
            Shown += Dashboard_Shown;

            // Close icon events
            CloseIcon.Click += CloseIcon_Click;
            CloseIcon.MouseEnter += CloseIcon_MouseEnter;
            CloseIcon.MouseLeave += CloseIcon_MouseLeave;

            // Timer events
            BatteryStatusTimer.Tick += BatteryStatusTimer_Tick;
            ShowNotificationTimer.Tick += ShowNotificationTimer_Tick;

            // Notification checkbox events
            FullBatteryNotificationCheckbox.CheckedChanged += FullBatteryNotificationCheckbox_CheckStateChanged;
            LowBatteryNotificationCheckbox.CheckedChanged += LowBatteryNotificationCheckbox_CheckStateChanged;

            // Window dragging events
            AppHeaderTitle.MouseDown += AppHeaderTitle_MouseDown;
            AppHeaderTitle.MouseMove += AppHeaderTitle_MouseMove;
            AppHeaderTitle.MouseUp += AppHeaderTitle_MouseUp;

            // Trackbar events
            lowBatteryTrackbar.Scroll += LowBatteryTrackbar_Scroll;
            lowBatteryTrackbar.ValueChanged += LowBatteryTrackbar_ValueChanged;
            fullBatteryTrackbar.Scroll += FullBatteryTrackbar_Scroll;
            fullBatteryTrackbar.ValueChanged += FullBatteryTrackbar_ValueChanged;

            // Settings events
            PinToNotificationArea.CheckedChanged += PinToNotificationArea_CheckedChanged;
            launchAtStartup.CheckedChanged += LaunchAtStartup_CheckedChanged;

            // Theme events
            SystemThemeLabel.CheckedChanged += SystemThemeLabel_CheckedChanged;
            DarkThemeLabel.CheckedChanged += DarkThemeLabel_CheckedChanged;
            LightThemeLabel.CheckedChanged += LightThemeLabel_CheckedChanged;

            // Sound browser events
            BrowserFullBatterySound.Click += BrowseFullBatterySound_Click;
            BrowseLowBatterySound.Click += BrowseLowBatterySound_Click;

            // Other events
            VersionLabel.Click += VersionLabel_Click;
            BatteryNotifierIcon.BalloonTipClicked += BatteryNotifierIcon_BalloonTipClicked;
            BatteryNotifierIcon.BalloonTipClosed += BatteryNotifierIcon_BalloonTipClosed;
        }


        private void CloseIcon_Click(object? sender, EventArgs e)
        {
            _windowManager.HandleCloseClick();
        }

        private void CloseIcon_MouseEnter(object? sender, EventArgs e)
        {
            CloseIcon.Image = Resources.closeIconHoverState;
        }

        private void CloseIcon_MouseLeave(object? sender, EventArgs e)
        {
            CloseIcon.Image = Resources.closeIconDark;
        }

        private void BatteryStatusTimer_Tick(object? sender, EventArgs e)
        {
            _batteryManager.RefreshBatteryStatus(BatteryStatus, BatteryPercentage, RemainingTime, BatteryImage);
        }

        private void ShowNotificationTimer_Tick(object? sender, EventArgs e)
        {
            _notificationManager.CheckAndShowNotifications(_batteryManager.PowerStatus, BatteryNotifierIcon);
        }

        private void Dashboard_Activated(object? sender, EventArgs e)
        {
            BatteryStatusTimer.Start();
            _batteryManager.RefreshBatteryStatus(BatteryStatus, BatteryPercentage, RemainingTime, BatteryImage);
            _settingsManager.LoadNotificationSettings(FullBatteryNotificationCheckbox, LowBatteryNotificationCheckbox);
            _windowManager.RenderFormPosition(BatteryNotifierIcon);
            _batteryManager.UpdateChargingAnimation(BatteryImage);
        }

        private void FullBatteryNotificationCheckbox_CheckStateChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleFullBatteryNotificationChange(FullBatteryNotificationCheckbox);
            _notificationManager.ShowInAppNotification(NotificationText,
                "🔔 Full Battery Notification " + (appSetting.Default.fullBatteryNotification ? "Enabled" : "Disabled"));
            BatteryNotifierIcon.ContextMenuStrip = _contextMenuManager.InitializeContextMenu(BatteryNotifierIcon, NotificationSettingLabel);
        }

        private void LowBatteryNotificationCheckbox_CheckStateChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleLowBatteryNotificationChange(LowBatteryNotificationCheckbox);
            _notificationManager.ShowInAppNotification(NotificationText,
                "🔔 Low Battery Notification " + (appSetting.Default.lowBatteryNotification ? "Enabled" : "Disabled"));
            BatteryNotifierIcon.ContextMenuStrip = _contextMenuManager.InitializeContextMenu(BatteryNotifierIcon, NotificationSettingLabel);
        }

        private void PinToNotificationArea_CheckedChanged(object? sender, EventArgs e)
        {
            _settingsManager.UpdatePinToNotificationArea(PinToNotificationArea.Checked);
            _windowManager.RenderFormPosition(BatteryNotifierIcon);
            Show();
            _windowManager.RenderTitleBarCursor(AppHeaderTitle);
        }

        private void LaunchAtStartup_CheckedChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleStartupLaunchSetting(launchAtStartup);
        }

        private void FullBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            FullBatteryNotificationPercentageLabel.Text = $"({fullBatteryTrackbar.Value}%)";
        }

        private void LowBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            LowBatteryNotificationPercentageLabel.Text = $"({lowBatteryTrackbar.Value}%)";
        }

        private void FullBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleFullBatteryTrackbarChange(fullBatteryTrackbar.Value);
        }

        private void LowBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleLowBatteryTrackbarChange(lowBatteryTrackbar.Value);
        }

        private void SystemThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            SuspendLayout();
            _themeManager.SetSystemTheme().ApplyTheme(ThemePictureBox, CloseIcon);
            _batteryManager.UpdateChargingAnimation(BatteryImage);
            Notify("Battery Notifier theme is synced with system theme.");
            ResumeLayout(true);
        }

        private void DarkThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            SuspendLayout();
            _themeManager.SetDarkTheme().ApplyTheme(ThemePictureBox, CloseIcon);
            _batteryManager.UpdateChargingAnimation(BatteryImage);
            Notify("Battery Notifier is on dark mode 🌙.");
            ResumeLayout(true);
        }

        private void LightThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            SuspendLayout();
            _themeManager.SetLightTheme().ApplyTheme(ThemePictureBox, CloseIcon);
            _batteryManager.UpdateChargingAnimation(BatteryImage);
            Notify("Battery Notifier is on light mode 🔆.");
            ResumeLayout(true);
        }

        private void BrowseFullBatterySound_Click(object? sender, EventArgs e)
        {
            var soundPath = _soundManager.BrowseForSoundFile(NotificationText);
            if (!string.IsNullOrEmpty(soundPath))
            {
                FullBatterySound.Text = soundPath;
                _settingsManager.SaveFullBatterySoundPath(soundPath);
            }
        }

        private void BrowseLowBatterySound_Click(object? sender, EventArgs e)
        {
            var soundPath = _soundManager.BrowseForSoundFile(NotificationText);
            if (!string.IsNullOrEmpty(soundPath))
            {
                LowBatterySound.Text = soundPath;
                _settingsManager.SaveLowBatterySoundPath(soundPath);
            }
        }

        private void AppHeaderTitle_MouseDown(object? sender, MouseEventArgs e)
        {
            _windowManager.HandleMouseDown(e);
        }

        private void AppHeaderTitle_MouseMove(object? sender, MouseEventArgs e)
        {
            _windowManager.HandleMouseMove(e);
        }

        private void AppHeaderTitle_MouseUp(object? sender, MouseEventArgs e)
        {
            _windowManager.HandleMouseUp(e);
        }

        private void VersionLabel_Click(object? sender, EventArgs e)
        {
            UtilityHelper.StartExternalUrlProcess(Constant.ReleaseUrl);
        }

        private void BatteryNotifierIcon_BalloonTipClicked(object? sender, EventArgs e)
        {
            Show();
            Activate();
        }

        private void BatteryNotifierIcon_BalloonTipClosed(object? sender, EventArgs e)
        {
            _soundManager.StopSound();
        }


        protected override void OnDeactivate(EventArgs e)
        {
            if (appSetting.Default.PinToNotificationArea) Hide();
            base.OnDeactivate(e);
        }

        private void BatteryNotifierIcon_Click(object? sender, EventArgs e)
        {
            if (!Visible)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
            else
            {
                Hide();
            }
        }

        private void Dashboard_Shown(object? sender, EventArgs e)
        {
            Invalidate();
        }

        private void ApplyFontStyle()
        {
            var boldSize12 = new Control[] { AppHeaderTitle };
            foreach (var ctrl in boldSize12)
                ctrl.ApplyBoldFont(size: 12);

            var boldControls = new Control[] { BatteryPercentage, RemainingTime };
            foreach (var ctrl in boldControls)
                ctrl.ApplyBoldFont();

            var regularControls = new Control[]
            {
                BatteryStatus, NotificationSettingLabel, FullBatteryLabel, LowBatteryLabel,
                AppTabControl, FullBatteryNotificationCheckbox, LowBatteryNotificationCheckbox,
                VersionLabel, PinToNotificationAreaLabel, LaunchAtStartUpLabel, ThemeLabel,
                SystemThemeLabel, LightThemeLabel, DarkThemeLabel, NotificationPanel,
                SettingHeader, FullBatteryNotificationSettingLabel, LowBatteryNotificationSettingLabel,
                FullBatteryNotificationPercentageLabel, LowBatteryNotificationPercentageLabel,
                FullBatterySound, LowBatterySound, NotificationText
            };
            foreach (var ctrl in regularControls)
                ctrl.ApplyRegularFont();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _batteryManager?.Dispose();
                _notificationManager?.Dispose();
                _themeManager?.Dispose();
                _settingsManager?.Dispose();
                _soundManager?.Dispose();
                _windowManager?.Dispose();
                _contextMenuManager?.Dispose();

                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}