using System;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Helpers;
using BatteryNotifier.Properties;
using Microsoft.Win32;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Forms
{
    public partial class SettingPage
    {
        private readonly Debouncer.Debouncer _debouncer;

        private Point _lastLocation;
        private bool _mouseDown;

        public SettingPage()
        {
            InitializeComponent();
            ApplyTheme();
            RenderFormArea();
            _debouncer = new Debouncer.Debouncer();
        }

        private void RenderFormArea()
        {
            this.RenderFormPosition(appSetting.Default.showAsModal);
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

        private void CloseIcon_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void SettingPage_Load(object? sender, EventArgs e)
        {
            LoadSettings();
            HandleStartup();

            AttachEventListeners();
        }

        private void AttachEventListeners()
        {
            CloseIcon.Click += new EventHandler(this.CloseIcon_Click);
            CloseIcon.MouseEnter += new EventHandler(this.CloseIcon_MouseEnter);
            CloseIcon.MouseLeave += new EventHandler(this.CloseIcon_MouseLeave);

            AppHeaderTitle.MouseDown += new MouseEventHandler(this.AppHeaderTitle_MouseDown);
            AppHeaderTitle.MouseMove += new MouseEventHandler(this.AppHeaderTitle_MouseMove);
            AppHeaderTitle.MouseUp += new MouseEventHandler(this.AppHeaderTitle_MouseUp);

            browseFullBatterySoundButton.Click += new EventHandler(this.browseFullBatterySoundButton_Click);
            lowBatteryTrackbar.Scroll += new EventHandler(this.lowBatteryTrackbar_Scroll);
            lowBatteryTrackbar.ValueChanged += new EventHandler(this.lowBatteryTrackbar_ValueChanged);

            showLowBatteryNotification.CheckedChanged += new EventHandler(this.showLowBatteryNotification_CheckedChanged);
            showFullBatteryNotification.CheckedChanged += new EventHandler(this.showFullBatteryNotification_CheckedChanged);

            fullBatteryTrackbar.Scroll += new EventHandler(this.fullBatteryTrackbar_Scroll);
            fullBatteryTrackbar.ValueChanged += new EventHandler(this.fullBatteryTrackbar_ValueChanged);

            DarkModeCheckbox.CheckedChanged += new EventHandler(this.DarkModeCheckbox_CheckedChanged);

            ShowAsWindow.CheckedChanged += new EventHandler(this.ShowAsWindow_CheckedChanged);
            
        }

        private void LoadSettings()
        {
            ShowAsWindow.Checked = appSetting.Default.showAsModal;
            DarkModeCheckbox.Checked = appSetting.Default.darkThemeApplied;
            launchAtStartup.Checked = appSetting.Default.LaunchAtStartup;

            showFullBatteryNotification.Checked = appSetting.Default.fullBatteryNotification;
            fullBatteryTrackbar.Value = appSetting.Default.fullBatteryNotificationValue;
            fullbatteryPercentageValue.Value = appSetting.Default.fullBatteryNotificationValue;
            fullbatterySoundPath.Text = appSetting.Default.fullBatteryNotificationMusic;

            showLowBatteryNotification.Checked = appSetting.Default.lowBatteryNotification;
            lowBatteryTrackbar.Value = appSetting.Default.lowBatteryNotificationValue;
            lowBatteryPercentageValue.Value = appSetting.Default.lowBatteryNotificationValue;
            lowBatterySoundPath.Text = appSetting.Default.lowBatteryNotificationMusic;
        }

        private void fullBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            fullbatteryPercentageValue.Value = fullBatteryTrackbar.Value;
        }

        private void lowBatteryTrackbar_Scroll(object? sender, EventArgs e)
        {
            lowBatteryPercentageValue.Value = lowBatteryTrackbar.Value;
        }

        private void browseFullBatterySoundButton_Click(object? sender, EventArgs e)
        {
            var fileBrowser = new OpenFileDialog();
            fileBrowser.ShowDialog();

            if (!fileBrowser.CheckFileExists) return;
            
            fullbatterySoundPath.Text = fileBrowser.FileName;
            appSetting.Default.fullBatteryNotificationMusic = fullbatterySoundPath.Text;
            appSetting.Default.Save();
        }

        private void browseLowBatterySoundButton_Click(object? sender, EventArgs e)
        {
            var fileBrowser = new OpenFileDialog();
            fileBrowser.ShowDialog();

            if (!fileBrowser.CheckFileExists) return;
            lowBatterySoundPath.Text = fileBrowser.FileName;
            appSetting.Default.lowBatteryNotificationMusic = lowBatterySoundPath.Text;
            appSetting.Default.Save();

        }

        private void ShowAsWindow_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.showAsModal = ShowAsWindow.Checked;
            appSetting.Default.Save();
        }

        private void showFullBatteryNotification_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.fullBatteryNotification = showFullBatteryNotification.Checked;
            appSetting.Default.Save();
        }

        private void showLowBatteryNotification_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.lowBatteryNotification = showLowBatteryNotification.Checked;
            appSetting.Default.Save();
        }

        private void fullBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _debouncer.Debounce(SaveSetting, 500);

            void SaveSetting()
            {
                appSetting.Default.fullBatteryNotificationValue = fullBatteryTrackbar.Value;
                appSetting.Default.Save();
            }
        }

        private void lowBatteryTrackbar_ValueChanged(object? sender, EventArgs e)
        {
            _debouncer.Debounce(SaveSetting, 500);
            void SaveSetting()
            {
                appSetting.Default.lowBatteryNotificationValue = lowBatteryTrackbar.Value;
                appSetting.Default.Save();
            }
           
        }

        private void SettingPage_Activated(object? sender, EventArgs e)
        {
            ApplyTheme();
            LoadSettings();
            RenderFormArea();
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

        private void DarkModeCheckbox_CheckedChanged(object? sender, EventArgs e)
        {
            appSetting.Default.darkThemeApplied = DarkModeCheckbox.Checked;
            appSetting.Default.Save();
            ApplyTheme();
        }

        bool isLightThemeRendered = false;
        bool isDarkThemeRendered = false;
        private void ApplyTheme()
        {
            if (appSetting.Default.darkThemeApplied)
            {
                if (isDarkThemeRendered) return;
                ShowAsWindowPanel.BackColor = Color.FromArgb(20, 20, 20);
                DarkModelPanel.BackColor = Color.FromArgb(20, 20, 20);
                LaunchAtStartupPanel.BackColor = Color.FromArgb(20, 20, 20);
                SettingHeader.BackColor = Color.Black;
                AppHeaderTitle.ForeColor = Color.White;
                SettingContainer.BackColor = Color.FromArgb(30, 30, 30);
                FullBatteryNotificationGroupBox.ForeColor = Color.White;
                ShowFullBatteryNotificationLabel.ForeColor = Color.White;

                ShowAsWindowLabel.ForeColor = Color.White;
                DarkModeLabel.ForeColor = Color.White;
                LaunchAtStartUpLabel.ForeColor = Color.White;

                fullBatteryTrackbar.BackColor = Color.FromArgb(30, 30, 30);
                lowBatteryTrackbar.BackColor = Color.FromArgb(30, 30, 30);

                BatteryPercentageLabel.ForeColor = Color.White;
                FullBatterySoundLabel.ForeColor = Color.White;
                fullbatterySoundPath.BackColor = Color.FromArgb(20, 20, 20);
                fullbatterySoundPath.ForeColor = Color.White;
                browseFullBatterySoundButton.BackColor = Color.FromArgb(20, 20, 20);
                browseFullBatterySoundButton.ForeColor = Color.White;
                LowBatteryNotificationLabel.ForeColor = Color.White;
                LowBatteryPercentageLabel.ForeColor = Color.White;
                LowBatterySoundLabel.ForeColor = Color.White;
                lowBatterySoundPath.BackColor = Color.FromArgb(20, 20, 20);
                lowBatterySoundPath.ForeColor = Color.White;
                browseLowBatterySoundButton.BackColor = Color.FromArgb(20, 20, 20);
                browseLowBatterySoundButton.ForeColor = Color.White;
                lowBatteryPercentageValue.BackColor = Color.FromArgb(20, 20, 20);
                lowBatteryPercentageValue.ForeColor = Color.White;
                fullbatteryPercentageValue.BackColor = Color.FromArgb(20, 20, 20);
                fullbatteryPercentageValue.ForeColor = Color.White;

                CloseIcon.Image = Resources.closeIconDark;

                isDarkThemeRendered = true;
                isLightThemeRendered = false;
            }
            else
            {
                if (isLightThemeRendered) return;
                ShowAsWindowPanel.BackColor = Color.WhiteSmoke;
                DarkModelPanel.BackColor = Color.WhiteSmoke;
                LaunchAtStartupPanel.BackColor = Color.WhiteSmoke;
                SettingHeader.BackColor = Color.AliceBlue;
                AppHeaderTitle.ForeColor = Color.Black;
                SettingContainer.BackColor = Color.White;

                FullBatteryNotificationGroupBox.ForeColor = Color.Black;
                ShowFullBatteryNotificationLabel.ForeColor = Color.Black;

                fullBatteryTrackbar.BackColor = Color.White;
                lowBatteryTrackbar.BackColor = Color.White;

                ShowAsWindowLabel.ForeColor = Color.Black;
                DarkModeLabel.ForeColor = Color.Black;
                LaunchAtStartUpLabel.ForeColor = Color.Black;

                BatteryPercentageLabel.ForeColor = Color.Black;
                FullBatterySoundLabel.ForeColor = Color.Black;
                fullbatterySoundPath.BackColor = Color.White;
                fullbatterySoundPath.ForeColor = Color.Black;
                browseFullBatterySoundButton.BackColor = Color.LightGray;
                browseFullBatterySoundButton.ForeColor = Color.Black;
                LowBatteryNotificationLabel.ForeColor = Color.Black;
                LowBatteryPercentageLabel.ForeColor = Color.Black;
                LowBatterySoundLabel.ForeColor = Color.Black;
                lowBatterySoundPath.BackColor = Color.White;
                lowBatterySoundPath.ForeColor = Color.Black;
                browseLowBatterySoundButton.BackColor = Color.LightGray;
                browseLowBatterySoundButton.ForeColor = Color.Black;
                lowBatteryPercentageValue.BackColor = Color.LightGray;
                lowBatteryPercentageValue.ForeColor = Color.Black;
                fullbatteryPercentageValue.BackColor = Color.LightGray;
                fullbatteryPercentageValue.ForeColor = Color.Black;

                CloseIcon.Image = Resources.closeIconLight;

                isDarkThemeRendered = false;
                isLightThemeRendered = true;

            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {

            if (keyData == (Keys.Escape))
            {

                this.Close();
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void launchAtStartup_CheckedChanged(object? sender, EventArgs e)
        {
            HandleStartup();
        }

        private void HandleStartup()
        {
            var windowsStartupAppsKey = OpenWindowsStartupAppsKey();
            var screenCropperStartupValue = windowsStartupAppsKey.GetValue("BatteryNotifier");

            if (launchAtStartup.Checked)
            {
                if (screenCropperStartupValue == null)
                {
                    windowsStartupAppsKey.SetValue("BatteryNotifier", Application.ExecutablePath);
                }
            }
            else
            {
                if (screenCropperStartupValue != null)
                {
                    windowsStartupAppsKey.DeleteValue("BatteryNotifier");
                }
            }
            appSetting.Default.LaunchAtStartup = launchAtStartup.Checked;
            appSetting.Default.Save();
        }

        /// <summary>
        /// Opens and returns a key where Windows stores paths to executables that load on startup
        /// </summary>
        private RegistryKey OpenWindowsStartupAppsKey()
        {
            var currentUserRegKey = Registry.CurrentUser;

            return currentUserRegKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)!;
        }

    }
}
