using System;
using System.Windows.Forms;
using BatteryNotifier.Constants;
using BatteryNotifier.Lib.CustomControls.FlatTabControl;
using BatteryNotifier.Lib.Logger;
using BatteryNotifier.Lib.Manager;
using BatteryNotifier.Lib.Providers;
using BatteryNotifier.Lib.Services;
using BatteryNotifier.Properties;
using BatteryNotifier.Utils;
using Serilog;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Forms
{
    public partial class Dashboard : Form
    {
        private readonly ILogger _logger;

        private BatteryManager _batteryManager;
        private NotificationManager _notificationManager;
        private ThemeManager _themeManager;
        private SettingsManager _settingsManager;
        private SoundManager _soundManager;
        private WindowManager _windowManager;
        private ContextMenuManager _contextMenuManager;
        private readonly Debouncer _debouncer;
        private ThemeChangeService _themeService;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= 0x20000;
                return cp;
            }
        }

        public Dashboard()
        {
            InitializeComponent();

            _logger = BatteryNotifierAppLogger.ForContext<Dashboard>();

            UtilityHelper.EnableDoubleBuffering(this);
            UtilityHelper.EnableDoubleBufferingRecursively(this);
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
            UpdateStyles();
            _debouncer = new Debouncer();
            InitializeManagers();
            InitializeServices();
        }

        private void InitializeManagers()
        {
            _batteryManager = new BatteryManager(BatteryStatus, BatteryPercentage, RemainingTime, BatteryImage);
            _soundManager = new SoundManager();
            _notificationManager = new NotificationManager(_soundManager, BatteryNotifierIcon);

            var backAccentControls = new Control[]
            {
                AppContainer, AppTabControl, DashboardTab, SettingTab,
                AppFooter, LowBatterySound, FullBatterySound
            };

            var backAccent2Controls = new Control[]
            {
                ShowAsWindowPanel, LaunchAtStartupPanel, ThemeConfigurationPanel,
                ThemePanel, NotificationSettingPanel,
                NotificationPanel, FullBatteryNotificationPanel, LowBatteryNotificationPanel,
            };

            var backAccent3Controls = new Control[]
            {
                PinToWindowPictureBox, ThemePictureBox, LaunchAtStartUpPictureBox, NotificationSettingLabel,
                SettingHeader
            };

            var foreControls = new Control[]
            {
                DashboardTab, SettingTab, RemainingTime, BatteryPercentage,
                FullBatteryLabel, LowBatteryLabel, VersionLabel,
                NotificationText, ThemePanel, SystemThemeLabel,
                LightThemeLabel, DarkThemeLabel, NotificationPanel,
                FullBatteryNotificationPercentageLabel, PinToWindowLabel, LaunchAtStartUpLabel,
                BatteryPercentageLabel, LowBatteryNotificationPercentageLabel, LowBatterySound, FullBatterySound
            };

            var borderedCustomControls = new FlatTabControl[]
            {
                AppTabControl
            };

            _themeManager = new ThemeManager(this)
                .RegisterAccentControls(backAccentControls)
                .RegisterAccent2Controls(backAccent2Controls)
                .RegisterAccent3Controls(backAccent3Controls)
                .RegisterForegroundControls(foreControls)
                .RegisterBorderedCustomControls(borderedCustomControls);

            _settingsManager = new SettingsManager();
            _windowManager = new WindowManager(this);
            _contextMenuManager = new ContextMenuManager(_soundManager, this);
        }

        private void InitializeServices()
        {
            // Subscribe to battery monitor events
            BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged += OnPowerLineStatusChanged;

            // Subscribe to notifications
            NotificationService.Instance.NotificationReceived += OnNotificationReceived;

            _themeService = new ThemeChangeService();
            _themeService.ThemeChanged += OnThemeChanged;
        }

        private void UpdateNotificationMusicBrowseState()
        {
            ResetFullBatterySound.Visible = !string.IsNullOrEmpty(appSetting.Default.fullBatteryNotificationMusic);
            ResetLowBatterySound.Visible = !string.IsNullOrEmpty(appSetting.Default.lowBatteryNotificationMusic);

            FullBatterySound.Text = appSetting.Default.fullBatteryNotificationMusic;
            LowBatterySound.Text = appSetting.Default.lowBatteryNotificationMusic;
        }

        public void SetVersion(string? ver)
        {
            UtilityHelper.SafeInvoke(VersionLabel,
                () => { VersionLabel.Text = ver is null ? UtilityHelper.AssemblyVersion : $"v {ver}"; });
        }

        private void OnNotificationReceived(object sender, NotificationMessage notification)
        {
            UtilityHelper.SafeInvoke(NotificationText, () =>
            {
                NotificationText.Text = notification.Message;
                _ = _notificationManager.EmitGlobalNotification(notification);
                _debouncer.Debounce(() =>
                {
                    if (!NotificationText.IsDisposed)
                    {
                        NotificationText.Text = string.Empty;
                    }
                });
            });
        }

        private bool requirePendingBatteryUiUpdate;

        private void OnBatteryStatusChanged(object sender, BatteryStatusEventArgs e)
        {
            RefreshBatteryStatusIfTabSelected();

            (string message, NotificationType notificationType, string Tag) notificationInfo;
            if (e is { IsCharging: false, IsLowBattery: true })
                notificationInfo = (message: "🔋 Low Battery, please connect to charger.", NotificationType.Global,
                    Tag: Constant.LowBatteryTag);
            else if (e is { IsCharging: true, IsFullBattery: true })
                notificationInfo = (message: "🔋 Full Battery, please unplug the charger.", NotificationType.Global,
                    Tag: Constant.FullBatteryTag);
            else
                throw new ArgumentOutOfRangeException(nameof(e));

            NotificationService.Instance.PublishNotification(new NotificationMessage()
            {
                Message = notificationInfo.message,
                Type = notificationInfo.notificationType,
                Tag = notificationInfo.Tag
            });
        }

        private void RefreshBatteryStatusIfTabSelected()
        {
            UtilityHelper.SafeInvoke(AppTabControl, () =>
            {
                if (AppTabControl.SelectedTab == DashboardTab)
                {
                    _batteryManager.RefreshBatteryStatus();
                    requirePendingBatteryUiUpdate = false;
                }
                else
                {
                    requirePendingBatteryUiUpdate = true;
                }
            });
        }

        private void OnPowerLineStatusChanged(object sender, BatteryStatusEventArgs e)
        {
            RefreshBatteryStatusIfTabSelected();
        }

        private void Dashboard_Load(object? sender, EventArgs e)
        {
            WindowState = appSetting.Default.startMinimized ? FormWindowState.Minimized : FormWindowState.Normal;

            UpdateTaskbarAndIconVisibility();

            SuspendLayout();
            try
            {
                _themeManager.ApplyTheme(ThemePictureBox, CloseIcon);
                _windowManager.RenderTitleBarCursor(AppHeaderTitle);
                ApplyFontStyle();
                _settingsManager.LoadCheckboxSettings(PinToWindow, launchAtStartup)
                    .LoadTrackbarSettings(fullBatteryTrackbar, lowBatteryTrackbar,
                        FullBatteryNotificationPercentageLabel, LowBatteryNotificationPercentageLabel)
                    .LoadThemeSettings(SystemThemeLabel, DarkThemeLabel, LightThemeLabel)
                    .LoadSoundSettings(FullBatterySound, LowBatterySound)
                    .LoadNotificationSettings(FullBatteryNotificationCheckbox, LowBatteryNotificationCheckbox)
                    .HandleStartupLaunchSetting(launchAtStartup.Checked);

                UpdateNotificationMusicBrowseState();

                AttachEventListeners();

                _contextMenuManager.AttachContextMenu(BatteryNotifierIcon);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error on loading dashboard");
            }
            finally
            {
                ForceGarbageCollection();
                ResumeLayout(false);
            }
        }

        private void AttachEventListeners()
        {
            // Form events
            Activated += Dashboard_Activated;
            FormClosed += Dashboard_FormClosed;

            // Close icon events
            CloseIcon.Click += CloseIcon_Click;
            CloseIcon.MouseEnter += CloseIcon_MouseEnter;
            CloseIcon.MouseLeave += CloseIcon_MouseLeave;

            // Tab change
            AppTabControl.SelectedIndexChanged += AppTabControl_SelectedIndexChanged;

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
            PinToWindow.CheckedChanged += PinToWindow_CheckedChanged;
            launchAtStartup.CheckedChanged += LaunchAtStartup_CheckedChanged;

            // Theme events
            SystemThemeLabel.CheckedChanged += SystemThemeLabel_CheckedChanged;
            DarkThemeLabel.CheckedChanged += DarkThemeLabel_CheckedChanged;
            LightThemeLabel.CheckedChanged += LightThemeLabel_CheckedChanged;

            // Sound browser events
            BrowseFullBatterySound.Click += BrowseFullBatterySound_Click;
            BrowseLowBatterySound.Click += BrowseLowBatterySound_Click;

            // Reset Music Selection events
            ResetFullBatterySound.Click += ResetFullBatterySound_Click;
            ResetLowBatterySound.Click += ResetLowBatterySound_Click;

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

        private void AppTabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (AppTabControl.SelectedTab != DashboardTab) return;
            if (!requirePendingBatteryUiUpdate) return;

            _batteryManager.RefreshBatteryStatus();
            requirePendingBatteryUiUpdate = false;
        }

        private void Dashboard_Activated(object? sender, EventArgs e)
        {
            _settingsManager.LoadNotificationSettings(FullBatteryNotificationCheckbox, LowBatteryNotificationCheckbox);
            this.RenderFormPosition(BatteryNotifierIcon);

            WindowState = FormWindowState.Normal;
            Show();

            UpdateTaskbarAndIconVisibility();
            ForceGarbageCollection();
        }

        private void UpdateTaskbarAndIconVisibility()
        {
            if (appSetting.Default.PinToWindow)
            {
                ShowInTaskbar = false;
                ShowIcon = false;
            }
            else
            {
                ShowInTaskbar = true;
                ShowIcon = true;
            }
        }

        private void FullBatteryNotificationCheckbox_CheckStateChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleFullBatteryNotificationChange(FullBatteryNotificationCheckbox);
            NotificationService.Instance.PublishNotification("🔔 Full Battery Notification " +
                                                             (appSetting.Default.fullBatteryNotification
                                                                 ? "Enabled"
                                                                 : "Disabled"), NotificationType.Inline);
            BatteryNotifierIcon.ContextMenuStrip = _contextMenuManager.InitializeContextMenu();
        }

        private void LowBatteryNotificationCheckbox_CheckStateChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleLowBatteryNotificationChange(LowBatteryNotificationCheckbox);
            NotificationService.Instance.PublishNotification("🔔 Low Battery Notification " +
                                                             (appSetting.Default.lowBatteryNotification
                                                                 ? "Enabled"
                                                                 : "Disabled"), NotificationType.Inline);
            BatteryNotifierIcon.ContextMenuStrip = _contextMenuManager.InitializeContextMenu();
        }

        private void PinToWindow_CheckedChanged(object? sender, EventArgs e)
        {
            _settingsManager.UpdatePinToWindow(PinToWindow.Checked);
            this.RenderFormPosition(BatteryNotifierIcon);
            _windowManager.RenderTitleBarCursor(AppHeaderTitle);
            UpdateTaskbarAndIconVisibility();
        }

        private void LaunchAtStartup_CheckedChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleStartupLaunchSetting(launchAtStartup.Checked);
        }

        private void FullBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            FullBatteryNotificationPercentageLabel.Text = $@"({fullBatteryTrackbar.Value}%)";
        }

        private void LowBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            LowBatteryNotificationPercentageLabel.Text = $@"({lowBatteryTrackbar.Value}%)";
        }

        private void FullBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleFullBatteryTrackbarChange(fullBatteryTrackbar.Value);
        }

        private void LowBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _settingsManager.HandleLowBatteryTrackbarChange(lowBatteryTrackbar.Value);
        }

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            if (!appSetting.Default.SystemThemeApplied) return;
            
            UtilityHelper.SafeInvoke(ThemePictureBox, () =>
            {
                _themeManager.ApplyTheme(ThemePictureBox, CloseIcon);
                _batteryManager.UpdateChargingAnimation();
            });
        }

        private void SystemThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            SuspendLayout();
            _themeManager.SetSystemTheme().ApplyTheme(ThemePictureBox, CloseIcon);
            _batteryManager.UpdateChargingAnimation();
            NotificationService.Instance.PublishNotification("Battery Notifier theme is synced with system theme.",
                NotificationType.Inline);
            ForceGarbageCollection();
            ResumeLayout(true);
        }

        private void DarkThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            SuspendLayout();
            _themeManager.SetDarkTheme().ApplyTheme(ThemePictureBox, CloseIcon);
            _batteryManager.UpdateChargingAnimation();
            NotificationService.Instance.PublishNotification("Battery Notifier is on dark mode 🌙.",
                NotificationType.Inline);
            ForceGarbageCollection();
            ResumeLayout(true);
        }

        private void LightThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            SuspendLayout();
            _themeManager.SetLightTheme().ApplyTheme(ThemePictureBox, CloseIcon);
            _batteryManager.UpdateChargingAnimation();
            NotificationService.Instance.PublishNotification("Battery Notifier is on light mode 🔆.",
                NotificationType.Inline);
            ForceGarbageCollection();
            ResumeLayout(true);
        }

        private void BrowseFullBatterySound_Click(object? sender, EventArgs e)
        {
            var soundPath = _soundManager.BrowseForSoundFile();
            if (!string.IsNullOrEmpty(soundPath))
            {
                FullBatterySound.Text = soundPath;
            }

            _settingsManager.SaveFullBatterySoundPath(soundPath);
            UpdateNotificationMusicBrowseState();

            ForceGarbageCollection();
        }

        private void ResetFullBatterySound_Click(object? sender, EventArgs e)
        {
            var soundPath = string.Empty;
            FullBatterySound.Text = soundPath;
            _settingsManager.SaveFullBatterySoundPath(soundPath);
            UpdateNotificationMusicBrowseState();
        }

        private void ResetLowBatterySound_Click(object? sender, EventArgs e)
        {
            var soundPath = string.Empty;
            LowBatterySound.Text = soundPath;
            _settingsManager.SaveLowBatterySoundPath(soundPath);
            UpdateNotificationMusicBrowseState();
        }

        private void BrowseLowBatterySound_Click(object? sender, EventArgs e)
        {
            var soundPath = _soundManager.BrowseForSoundFile();
            if (!string.IsNullOrEmpty(soundPath))
            {
                LowBatterySound.Text = soundPath;
            }

            _settingsManager.SaveLowBatterySoundPath(soundPath);
            UpdateNotificationMusicBrowseState();

            ForceGarbageCollection();
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
            ForceGarbageCollection();
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


        private void BatteryNotifierIcon_Click(object? sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            Activate();
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
                VersionLabel, PinToWindowLabel, LaunchAtStartUpLabel, ThemeLabel,
                SystemThemeLabel, LightThemeLabel, DarkThemeLabel, NotificationPanel,
                SettingHeader, FullBatteryNotificationSettingLabel, LowBatteryNotificationSettingLabel,
                FullBatteryNotificationPercentageLabel, LowBatteryNotificationPercentageLabel, NotificationText
            };
            foreach (var ctrl in regularControls)
                ctrl.ApplyRegularFont();
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);

            if (!value)
            {
                NotificationService.Instance.ClearNotifications();
                NotificationService.Instance.ClearDeduplicationCache();

                ForceGarbageCollection();
            }
        }

        private void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void DetachEventHandlers()
        {
            // Form events
            Activated -= Dashboard_Activated;
            FormClosed -= Dashboard_FormClosed;

            // Close icon events
            CloseIcon.Click -= CloseIcon_Click;
            CloseIcon.MouseEnter -= CloseIcon_MouseEnter;
            CloseIcon.MouseLeave -= CloseIcon_MouseLeave;

            // Notification checkbox events
            FullBatteryNotificationCheckbox.CheckedChanged -= FullBatteryNotificationCheckbox_CheckStateChanged;
            LowBatteryNotificationCheckbox.CheckedChanged -= LowBatteryNotificationCheckbox_CheckStateChanged;

            // Window dragging events
            AppHeaderTitle.MouseDown -= AppHeaderTitle_MouseDown;
            AppHeaderTitle.MouseMove -= AppHeaderTitle_MouseMove;
            AppHeaderTitle.MouseUp -= AppHeaderTitle_MouseUp;

            // Trackbar events
            lowBatteryTrackbar.Scroll -= LowBatteryTrackbar_Scroll;
            lowBatteryTrackbar.ValueChanged -= LowBatteryTrackbar_ValueChanged;
            fullBatteryTrackbar.Scroll -= FullBatteryTrackbar_Scroll;
            fullBatteryTrackbar.ValueChanged -= FullBatteryTrackbar_ValueChanged;

            // Settings events
            PinToWindow.CheckedChanged -= PinToWindow_CheckedChanged;
            launchAtStartup.CheckedChanged -= LaunchAtStartup_CheckedChanged;

            // Theme events
            SystemThemeLabel.CheckedChanged -= SystemThemeLabel_CheckedChanged;
            DarkThemeLabel.CheckedChanged -= DarkThemeLabel_CheckedChanged;
            LightThemeLabel.CheckedChanged -= LightThemeLabel_CheckedChanged;

            // Sound browser events
            BrowseFullBatterySound.Click -= BrowseFullBatterySound_Click;
            BrowseLowBatterySound.Click -= BrowseLowBatterySound_Click;

            // Reset Music Selection events
            ResetFullBatterySound.Click -= ResetFullBatterySound_Click;
            ResetLowBatterySound.Click -= ResetLowBatterySound_Click;

            // Other events
            VersionLabel.Click -= VersionLabel_Click;
            BatteryNotifierIcon.BalloonTipClicked -= BatteryNotifierIcon_BalloonTipClicked;
            BatteryNotifierIcon.BalloonTipClosed -= BatteryNotifierIcon_BalloonTipClosed;
            BatteryNotifierIcon.Click -= BatteryNotifierIcon_Click;
        }

        private void Dashboard_FormClosed(object? sender, FormClosedEventArgs e)
        {
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from events first to prevent callbacks during disposal
                BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
                BatteryMonitorService.Instance.PowerLineStatusChanged -= OnPowerLineStatusChanged;
                NotificationService.Instance.NotificationReceived -= OnNotificationReceived;

                // Detach all event handlers explicitly
                DetachEventHandlers();

                // Dispose managers in reverse order of creation
                _contextMenuManager?.Dispose();
                _windowManager?.Dispose();
                _soundManager?.Dispose();
                _settingsManager?.Dispose();
                _themeManager?.Dispose();
                _notificationManager?.Dispose();
                _batteryManager?.Dispose();
                _debouncer?.Dispose();

                // Clean up services
                BatteryMonitorService.Instance?.Dispose();

                // Clear notification service state
                NotificationService.Instance.ClearNotifications();
                NotificationService.Instance.ClearDeduplicationCache();

                _themeService?.Dispose();

                // Clean up font resources
                FontProvider.Cleanup();

                // Dispose designer-generated resources
                components?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}