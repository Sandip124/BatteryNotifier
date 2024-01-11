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
        private readonly CustomTimer.CustomTimer _customTimer = new();
        private readonly ContextMenuStrip contextMenu = new();

        private Point _lastLocation;
        private bool _mouseDown;
        private bool _isCharging;

        private const int DefaultMusicPlayingDuration = 30;
        private const int DefaultNotificationInterval = 30000;
        private const int DefaultSoundPlayingInterval = 1000;
        private const int DefaultNotificationTimeout = 3000;

        const int WS_MINIMIZEBOX = 0x20000;
        const int CS_DBLCLKS = 0x8;
        
        readonly PowerStatus powerStatus = SystemInformation.PowerStatus;
        readonly decimal percentage = (int)Math.Round(SystemInformation.PowerStatus.BatteryLifePercent * 100, 0);

        private static bool ShowFullBatteryNotification => appSetting.Default.fullBatteryNotification;
        private static bool ShowLowBatteryNotification => appSetting.Default.lowBatteryNotification;
        
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                cp.Style |= WS_MINIMIZEBOX;
                cp.ClassStyle |= CS_DBLCLKS;
                return cp;
            }
        }
        public Dashboard()
        {
            InitializeComponent();
            _debouncer = new Debouncer.Debouncer();
        }

        private void RenderTitleBarCursor()
        {
            AppHeaderTitle.Cursor = appSetting.Default.PinToNotificationArea ? Cursors.Default : Cursors.SizeAll;
        }

        public void SetVersion(string? ver)
        {
            VersionLabel.Text = ver is null ? UtilityHelper.AssemblyVersion : $"v {ver}";
        }

        public void Notify(string status, int timeout = DefaultNotificationTimeout)
        {
            NotificationText.Text = status;
            _debouncer.Debounce(() =>
            {
                NotificationText.Text = string.Empty;
            }, timeout);

        }

        private void CloseIcon_Click(object? sender, EventArgs e)
        {
            if (appSetting.Default.PinToNotificationArea)
            {
                Hide();
            }
            else
            {
                WindowState = FormWindowState.Minimized;
            }
        }

        private void CloseIcon_MouseEnter(object? sender, EventArgs e)
        {
            CloseIcon.Image = Resources.closeIconHoverState;
        }

        private void CloseIcon_MouseLeave(object? sender, EventArgs e)
        {
            CloseIcon.BackColor = Color.Transparent;
            CloseIcon.Image = Resources.closeIconDark;
        }

        private void Dashboard_Load(object? sender, EventArgs e)
        {
            if (appSetting.Default.startMinimized) Hide();

            SuspendLayout();
            try
            {
                this.RenderFormPosition(BatteryNotifierIcon);
                ApplyTheme();
                RenderTitleBarCursor();
                ApplyFontStyle();
                LoadSettings();
                HandleLaunchAtStartup();
                RefreshBatteryStatus();
                LoadNotificationSetting();
                BatteryStatusTimer.Enabled = true;
                ShowNotificationTimer.Enabled = true;
                ConfigureTimer();
                AttachEventListeners();

                contextMenu.TopLevel = true;
                BatteryNotifierIcon.ContextMenuStrip = InitializeContextMenu();
            }
            catch (Exception ex)
            {
                Notify(ex.Message);
            }
            finally
            {
                ResumeLayout();
            }
        }

        private void LoadSettings()
        {
            PinToNotificationArea.Checked = appSetting.Default.PinToNotificationArea;
            launchAtStartup.Checked = appSetting.Default.LaunchAtStartup;

            fullBatteryTrackbar.Value = appSetting.Default.fullBatteryNotificationValue;
            FullBatteryNotificationPercentageLabel.Text = $"({appSetting.Default.fullBatteryNotificationValue}%)";

            lowBatteryTrackbar.Value = appSetting.Default.lowBatteryNotificationValue;
            LowBatteryNotificationPercentageLabel.Text = $"({appSetting.Default.lowBatteryNotificationValue}%)";

            if (appSetting.Default.SystemThemeApplied)
            {
                SystemThemeLabel.Checked = true;
            }
            else if (IsDarkTheme())
            {
                DarkThemeLabel.Checked = true;
            }
            else if (IsLightTheme())
            {
                LightThemeLabel.Checked = true;
            }

            FullBatterySound.Text = appSetting.Default.fullBatteryNotificationMusic;
            LowBatterySound.Text = appSetting.Default.lowBatteryNotificationMusic;

            Update();
        }

        private bool IsDarkTheme() => appSetting.Default.darkThemeApplied || (appSetting.Default.SystemThemeApplied && !UtilityHelper.IsLightTheme());

        private bool IsLightTheme() => !appSetting.Default.darkThemeApplied || (appSetting.Default.SystemThemeApplied && UtilityHelper.IsLightTheme());

        private void HandleLaunchAtStartup()
        {
            var shouldLaunchAtStartUp = launchAtStartup.Checked;

            var windowsStartupAppsKey = UtilityHelper.GetWindowsStartupAppsKey();
            var startupValue = windowsStartupAppsKey.GetValue(UtilityHelper.AppName);

            if (shouldLaunchAtStartUp)
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
            appSetting.Default.LaunchAtStartup = shouldLaunchAtStartUp;
            appSetting.Default.Save();
        }


        private void LaunchAtStartup_CheckedChanged(object? sender, EventArgs e)
        {
            HandleLaunchAtStartup();
        }

        private void ConfigureTimer()
        {
            _soundPlayingTimer.Enabled = true;
            _soundPlayingTimer.Interval = DefaultSoundPlayingInterval;
            ShowNotificationTimer.Interval = DefaultNotificationInterval;
        }

        private void AttachEventListeners()
        {
            Activated += Dashboard_Activated;

            CloseIcon.Click += CloseIcon_Click;
            CloseIcon.MouseEnter += CloseIcon_MouseEnter;
            CloseIcon.MouseLeave += CloseIcon_MouseLeave;

            VersionLabel.Click += VersionLabel_Click;

            BatteryStatusTimer.Tick += BatteryStatusTimer_Tick;
            ShowNotificationTimer.Tick += ShowNotificationTimer_Tick;
            _soundPlayingTimer.Tick += SoundPlayingTimer_Tick;

            FullBatteryNotificationCheckbox.CheckedChanged += FullBatteryNotificationCheckbox_CheckStateChanged;
            LowBatteryNotificationCheckbox.CheckedChanged += LowBatteryNotificationCheckbox_CheckStateChanged;

            CloseIcon.Click += CloseIcon_Click;
            CloseIcon.MouseEnter += CloseIcon_MouseEnter;
            CloseIcon.MouseLeave += CloseIcon_MouseLeave;

            AppHeaderTitle.MouseDown += AppHeaderTitle_MouseDown;
            AppHeaderTitle.MouseMove += AppHeaderTitle_MouseMove;
            AppHeaderTitle.MouseUp += AppHeaderTitle_MouseUp;

            lowBatteryTrackbar.Scroll += LowBatteryTrackbar_Scroll;
            lowBatteryTrackbar.ValueChanged += LowBatteryTrackbar_ValueChanged;

            fullBatteryTrackbar.Scroll += FullBatteryTrackbar_Scroll;
            fullBatteryTrackbar.ValueChanged += FullBatteryTrackbar_ValueChanged;

            PinToNotificationArea.CheckedChanged += PinToNotificationArea_CheckedChanged;

            launchAtStartup.CheckedChanged += LaunchAtStartup_CheckedChanged;

            SystemThemeLabel.CheckedChanged += SystemThemeLabel_CheckedChanged;
            DarkThemeLabel.CheckedChanged += DarkThemeLabel_CheckedChanged;
            LightThemeLabel.CheckedChanged += LightThemeLabel_CheckedChanged;

            BrowserFullBatterySound.Click += BrowseFullBatterySound_Click;
            BrowseLowBatterySound.Click += BrowseLowBatterySound_Click;
        }

        private void BrowseLowBatterySound_Click(object? sender, EventArgs e)
        {
            var soundPath = HandleSoundBrowse();
            LowBatterySound.Text = soundPath;

            appSetting.Default.lowBatteryNotificationMusic = soundPath;
            appSetting.Default.Save();
        }

        private void BrowseFullBatterySound_Click(object? sender, EventArgs e)
        {
            var soundPath = HandleSoundBrowse();
            FullBatterySound.Text = soundPath;

            appSetting.Default.fullBatteryNotificationMusic = soundPath;
            appSetting.Default.Save();
        }

        private string HandleSoundBrowse()
        {
            var fileBrowser = new OpenFileDialog
            {
                DefaultExt = "wav"
            };
            fileBrowser.ShowDialog();

            var fileName = fileBrowser.FileName;

            if (!UtilityHelper.IsValidWavFile(fileName))
            {
                Notify("Only .wav file is supported.");
                return string.Empty;
            }
            return fileBrowser.CheckFileExists ? fileName : string.Empty;
        }

        private void LightThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.darkThemeApplied = false;
            appSetting.Default.SystemThemeApplied = false;
            appSetting.Default.Save();
            UpdateChargingAnimation();
            ApplyTheme();

            Notify("Battery Notifier is on light mode 🔆.");
        }

        private void DarkThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.darkThemeApplied = true;
            appSetting.Default.SystemThemeApplied = false;
            appSetting.Default.Save();
            UpdateChargingAnimation();
            ApplyTheme();

            Notify("Battery Notifier is on dark mode 🌙.");
        }

        private void SystemThemeLabel_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.darkThemeApplied = false;
            appSetting.Default.SystemThemeApplied = true;
            appSetting.Default.Save();
            UpdateChargingAnimation();
            ApplyTheme();

            Notify("Battery Notifier theme is synced with system theme.");
        }

        private void FullBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            FullBatteryNotificationPercentageLabel.Text = $"({fullBatteryTrackbar.Value}%)";
        }

        private void LowBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            LowBatteryNotificationPercentageLabel.Text = $"({lowBatteryTrackbar.Value}%)";
        }

        private void PinToNotificationArea_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.PinToNotificationArea = PinToNotificationArea.Checked;
            appSetting.Default.Save();
            this.RenderFormPosition(BatteryNotifierIcon);
            Show();
            RenderTitleBarCursor();
        }

        private void FullBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _debouncer.Debounce(() =>
            {
                appSetting.Default.fullBatteryNotificationValue = fullBatteryTrackbar.Value;
                appSetting.Default.Save();
            }, 500);
        }

        private void LowBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _debouncer.Debounce(() =>
            {
                appSetting.Default.lowBatteryNotificationValue = lowBatteryTrackbar.Value;
                appSetting.Default.Save();
            }, 500);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            if (appSetting.Default.PinToNotificationArea) Hide();
            base.OnDeactivate(e);
        }

        private void SoundPlayingTimer_Tick(object? sender, EventArgs e)
        {
            if (_customTimer.TimerCount >= DefaultMusicPlayingDuration)
            {
                _soundPlayingTimer.Stop();
                _batteryNotification.Stop();
                _customTimer.ResetTimer();
            }
            _customTimer.Increment();
        }

        private void LoadNotificationSetting()
        {
            UtilityHelper.RenderCheckboxState(LowBatteryNotificationCheckbox, appSetting.Default.lowBatteryNotification);
            UtilityHelper.RenderCheckboxState(FullBatteryNotificationCheckbox, appSetting.Default.fullBatteryNotification);
        }

        private void CheckNotification()
        {
            if (powerStatus.PowerLineStatus == PowerLineStatus.Online && _isCharging && powerStatus.BatteryLifePercent >= (float)appSetting.Default.fullBatteryNotificationValue / 100)
            {
                const string fullBatteryNotificationMessage = "🔋 Battery is full please unplug the charger.";

                Notify(fullBatteryNotificationMessage);

                if (ShowFullBatteryNotification)
                {
                    BatteryNotifierIcon.ShowBalloonTip(50, "Full Battery", fullBatteryNotificationMessage, ToolTipIcon.Info);
                    PlaySound(appSetting.Default.fullBatteryNotificationMusic, Resources.BatteryFull, true);
                }
            }

            if (powerStatus.PowerLineStatus != PowerLineStatus.Offline || _isCharging ||
                !(powerStatus.BatteryLifePercent <= (float)appSetting.Default.lowBatteryNotificationValue / 100)) return;

            const string lowBatteryNotificationMessage = "🔋 Battery is low, please Connect to Charger.";

            Notify(lowBatteryNotificationMessage);

            if (ShowLowBatteryNotification)
            {
                BatteryNotifierIcon.ShowBalloonTip(50, "Low Battery", lowBatteryNotificationMessage, ToolTipIcon.Info);
                PlaySound(appSetting.Default.lowBatteryNotificationMusic, Resources.LowBatterySound, true);
            }
        }

        private void PlaySound(string source, System.IO.UnmanagedMemoryStream fallbackSoundSource, bool loop = false)
        {
            _soundPlayingTimer.Start();

            if (!string.IsNullOrEmpty(source))
            {
                _batteryNotification.SoundLocation = source;
            }
            else
            {
                _batteryNotification.Stream = fallbackSoundSource;
            }

            if (loop)
            {
                _batteryNotification.PlayLooping();
            }
            else
            {
                _batteryNotification.PlaySync();
            }

        }

        private void RefreshBatteryStatus()
        {
            if (!Visible) return;
            
            if (powerStatus.PowerLineStatus == PowerLineStatus.Online && powerStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery && _isCharging == false)
            {
                _isCharging = true;
                BatteryStatus.Text = "⚡ Charging";
                BatteryStatus.ForeColor = Color.ForestGreen;
                UpdateChargingAnimation();
            }
            else if (powerStatus.PowerLineStatus == PowerLineStatus.Offline || powerStatus.PowerLineStatus == PowerLineStatus.Unknown)
            {
                _isCharging = false;
                BatteryStatus.Text = "🙄 Not Charging";
                BatteryStatus.ForeColor = Color.Gray;
                SetBatteryChargeStatus(powerStatus);
            }
            else if (powerStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                _isCharging = false;
                BatteryStatus.Text = "💀 Are you running on main power !!";
                BatteryImage.Image = Resources.Unknown;
            }
            else if (powerStatus.BatteryChargeStatus == BatteryChargeStatus.Unknown)
            {
                _isCharging = false;
                BatteryStatus.Text = "😇 Only God knows about this battery !!";
                BatteryImage.Image = Resources.Unknown;
            }

            UpdateBatteryPercentage(powerStatus);
            UpdateBatteryChargeRemainingStatus(powerStatus);
        }

        private void UpdateChargingAnimation()
        {
            if (!_isCharging) return;
            BatteryImage.Image = ThemeProvider.IsDarkTheme ? Resources.ChargingBatteryAnimatedDark : Resources.ChargingBatteryAnimated;
        }

        private void UpdateBatteryChargeRemainingStatus(PowerStatus status)
        {
            if (status.BatteryLifeRemaining >= 0)
            {
                var timeSpan = TimeSpan.FromSeconds(status.BatteryLifeRemaining);
                RemainingTime.Text = $@"{timeSpan.Hours} hr {timeSpan.Minutes} min remaining";
                return;
            }
            RemainingTime.Text = $@"{Math.Round(status.BatteryLifePercent * 100, 0)}% remaining";
        }

        private void UpdateBatteryPercentage(PowerStatus status)
        {
            var powerPercent = (int)(status.BatteryLifePercent * 100);
            BatteryPercentage.Text = $@"{(powerPercent <= 100 ? powerPercent.ToString() : "0")}%";
        }

        private void SetBatteryChargeStatus(PowerStatus status)
        {
            if (_isCharging) return;

            if (status.BatteryLifePercent >= .96)
            {
                BatteryStatus.Text = "Full Battery";
                BatteryImage.Image = Resources.Full;
            }
            else if (status.BatteryLifePercent >= .6 && status.BatteryLifePercent <= .96)
            {
                BatteryStatus.Text = "Adequate Battery";
                BatteryImage.Image = Resources.Sufficient;
            }
            else if (status.BatteryLifePercent >= .4 && status.BatteryLifePercent <= .6)
            {
                BatteryStatus.Text = "Sufficient Battery";
                BatteryImage.Image = Resources.Normal;
            }
            else if (status.BatteryLifePercent < .4)
            {
                BatteryStatus.Text = "Battery Low";
                BatteryImage.Image = Resources.Low;
            }
            else if (status.BatteryLifePercent <= .14)
            {
                BatteryStatus.Text = "Battery Critical";
                BatteryImage.Image = Resources.Critical;
            }
        }

        private ContextMenuStrip InitializeContextMenu()
        {
            contextMenu.Items.Clear();

            ToolStripMenuItem fullBatteryNotificationToolStripItem = new("Full Battery Notification")
            {
                Text = "Full Battery Notification" + (appSetting.Default.fullBatteryNotification ? "✔" : ""),
                Name = "FullBatteryNotification",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            fullBatteryNotificationToolStripItem.Click += FullBatteryNotification_Click!;

            ToolStripMenuItem lowBatteryNotificationToolStripItem = new("Low Battery Notification")
            {
                Text = "Low Battery Notification" + (appSetting.Default.lowBatteryNotification ? "✔" : ""),
                Name = "LowBatteryNotification",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            lowBatteryNotificationToolStripItem.Click += LowBatteryNotification_Click!;

            ToolStripMenuItem startMinimizedToolStripItem = new("Start Minimized")
            {
                Text = "Start Minimized" + (appSetting.Default.startMinimized ? "✔" : ""),
                Name = "StartMinimized",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            startMinimizedToolStripItem.Click += StartMinimized_Click!;

            ToolStripMenuItem exitAppToolStripItem = new("ExitApplication")
            {
                Text = "Exit Application",
                Name = "ExitApp",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            exitAppToolStripItem.Click += ExitApp_Click!;

            ToolStripMenuItem viewSourceToolStripItem = new("ViewSource")
            {
                Text = "View Source",
                Name = "ViewSource",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            viewSourceToolStripItem.Click += ViewSource_Click!;

            contextMenu.Items.Add(fullBatteryNotificationToolStripItem);
            contextMenu.Items.Add(lowBatteryNotificationToolStripItem);
            contextMenu.Items.Add(startMinimizedToolStripItem);
            contextMenu.Items.Add(startMinimizedToolStripItem);
            contextMenu.Items.Add(viewSourceToolStripItem);
            contextMenu.Items.Add(exitAppToolStripItem);

            return contextMenu;
        }

        private void StartMinimized_Click(object? sender, EventArgs e)
        {
            appSetting.Default.startMinimized = !appSetting.Default.startMinimized;
            appSetting.Default.Save();

            BatteryNotifierIcon.ContextMenuStrip = InitializeContextMenu();
        }

        private void FullBatteryNotification_Click(object? sender, EventArgs e)
        {
            appSetting.Default.fullBatteryNotification = !appSetting.Default.fullBatteryNotification;
            appSetting.Default.Save();

            ShowFullBatteryNotificationStatus();

            BatteryNotifierIcon.ContextMenuStrip = InitializeContextMenu();
        }

        private void ShowFullBatteryNotificationStatus()
        {
            Notify("🔔 Full Battery Notification " + (appSetting.Default.fullBatteryNotification ? "Enabled" : "Disabled"));
        }

        private void LowBatteryNotification_Click(object? sender, EventArgs e)
        {
            appSetting.Default.lowBatteryNotification = !appSetting.Default.lowBatteryNotification;
            appSetting.Default.Save();

            ShowLowBatteryNotificationStatus();

            BatteryNotifierIcon.ContextMenuStrip = InitializeContextMenu();
        }

        private void ShowLowBatteryNotificationStatus()
        {
            Notify("🔔 Low Battery Notification " + (appSetting.Default.lowBatteryNotification ? "Enabled" : "Disabled"));
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
                WindowState = FormWindowState.Normal;
            }
        }

        private void BatteryStatusTimer_Tick(object? sender, EventArgs e)
        {
            RefreshBatteryStatus();
        }

        private void FullBatteryNotificationCheckbox_CheckStateChanged(object? sender, EventArgs e)
        {
            UtilityHelper.RenderCheckboxState(FullBatteryNotificationCheckbox, FullBatteryNotificationCheckbox.Checked);
            appSetting.Default.fullBatteryNotification = FullBatteryNotificationCheckbox.Checked;
            appSetting.Default.Save();

            ShowFullBatteryNotificationStatus();

            BatteryNotifierIcon.ContextMenuStrip = InitializeContextMenu();
        }
        private void LowBatteryNotificationCheckbox_CheckStateChanged(object? sender, EventArgs e)
        {
            UtilityHelper.RenderCheckboxState(LowBatteryNotificationCheckbox, LowBatteryNotificationCheckbox.Checked);
            appSetting.Default.lowBatteryNotification = LowBatteryNotificationCheckbox.Checked;
            appSetting.Default.Save();

            ShowLowBatteryNotificationStatus();

            BatteryNotifierIcon.ContextMenuStrip = InitializeContextMenu();
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
            SuspendLayout();
            BatteryStatusTimer.Start();
            RefreshBatteryStatus();
            LoadNotificationSetting();
            this.RenderFormPosition(notifyIcon: BatteryNotifierIcon);
            UpdateChargingAnimation();
            ResumeLayout();
        }

        private void ApplyFontStyle()
        {
            AppHeaderTitle.ApplyBoldFont(12);
            BatteryPercentage.ApplyBoldFont();
            BatteryStatus.ApplyRegularFont();
            RemainingTime.ApplyBoldFont();

            NotificationSettingLabel.ApplyRegularFont();
            FullBatteryLabel.ApplyRegularFont();
            LowBatteryLabel.ApplyRegularFont();

            AppTabControl.ApplyRegularFont();
            FullBatteryNotificationCheckbox.ApplyRegularFont();
            LowBatteryNotificationCheckbox.ApplyRegularFont();
            VersionLabel.ApplyRegularFont();

            PinToNotificationAreaLabel.ApplyRegularFont();
            LaunchAtStartUpLabel.ApplyRegularFont();
            ThemeLabel.ApplyRegularFont();
            SystemThemeLabel.ApplyRegularFont();
            LightThemeLabel.ApplyRegularFont();
            DarkThemeLabel.ApplyRegularFont();
            NotificationPanel.ApplyRegularFont();

            SettingHeader.ApplyRegularFont();
            FullBatteryNotificationSettingLabel.ApplyRegularFont();
            LowBatteryNotificationSettingLabel.ApplyRegularFont();

            FullBatteryNotificationPercentageLabel.ApplyRegularFont();
            LowBatteryNotificationPercentageLabel.ApplyRegularFont();

            FullBatterySound.ApplyRegularFont();
            LowBatterySound.ApplyRegularFont();

            NotificationText.ApplyRegularFont();
        }

        private void ApplyTheme()
        {
            SuspendLayout();

            ThemePictureBox.Image = IsDarkTheme() ? Resources.DarkMode : Resources.LightMode;

            var theme = ThemeProvider.GetTheme();

            AppContainer.BackColor = theme.AccentColor;
            AppTabControl.MyBackColor = theme.AccentColor;
            AppTabControl.MyBorderColor = theme.Accent2Color;
            DashboardTab.BackColor = theme.AccentColor;
            SettingTab.BackColor = theme.AccentColor;
            DashboardTab.ForeColor = theme.ForegroundColor;
            SettingTab.ForeColor = theme.ForegroundColor;

            RemainingTime.ForeColor = theme.ForegroundColor;
            BatteryPercentage.ForeColor = theme.ForegroundColor;
            FullBatteryLabel.ForeColor = theme.ForegroundColor;
            LowBatteryLabel.ForeColor = theme.ForegroundColor;

            AppFooter.BackColor = theme.AccentColor;
            VersionLabel.ForeColor = theme.ForegroundColor;

            NotificationText.ForeColor = theme.ForegroundColor;

            ShowAsWindowPanel.BackColor = theme.Accent2Color;
            LaunchAtStartupPanel.BackColor = theme.Accent2Color;
            ThemeConfigurationPanel.BackColor = theme.Accent2Color;

            PinToNotificationAreaPictureBox.BackColor = theme.Accent3Color;
            ThemePictureBox.BackColor = theme.Accent3Color;
            LaunchAtStartUpPictureBox.BackColor = theme.Accent3Color;

            ThemePanel.BackColor = theme.Accent2Color;
            ThemePanel.ForeColor = theme.ForegroundColor;

            NotificationSettingPanel.BackColor = theme.AccentColor;
            FullBatteryNotificationPanel.BackColor = theme.Accent2Color;
            LowBatteryNotificationPanel.BackColor = theme.Accent2Color;

            SystemThemeLabel.ForeColor = theme.ForegroundColor;
            LightThemeLabel.ForeColor = theme.ForegroundColor;
            DarkThemeLabel.ForeColor = theme.ForegroundColor;

            SettingHeader.BackColor = theme.Accent2Color;
            NotificationSettingLabel.BackColor = theme.Accent2Color;
            NotificationPanel.BackColor = theme.AccentColor;
            NotificationPanel.BorderStyle = BorderStyle.FixedSingle;
            NotificationPanel.ForeColor = theme.ForegroundColor;
            FullBatteryNotificationPercentageLabel.ForeColor = theme.ForegroundColor;

            FullBatterySound.BackColor = theme.Accent2Color;
            FullBatterySound.ForeColor = theme.ForegroundColor;
            LowBatterySound.BackColor = theme.Accent2Color;
            LowBatterySound.ForeColor = theme.ForegroundColor;

            PinToNotificationAreaLabel.ForeColor = theme.ForegroundColor;
            LaunchAtStartUpLabel.ForeColor = theme.ForegroundColor;

            fullBatteryTrackbar.BackColor = theme.AccentColor;
            lowBatteryTrackbar.BackColor = theme.AccentColor;

            BatteryPercentageLabel.ForeColor = theme.ForegroundColor;

            LowBatteryNotificationPercentageLabel.ForeColor = theme.ForegroundColor;

            CloseIcon.Image = Resources.closeIconDark;

            FullBatteryPictureBox.BackColor = theme.AccentColor;
            LowBatteryPictureBox.BackColor = theme.AccentColor;

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

            _debouncer.Debounce(() =>
            {
                appSetting.Default.WindowPositionX = xPosition;
                appSetting.Default.WindowPositionY = yPosition;
                appSetting.Default.Save();
            }, 1000);
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
    }
}
