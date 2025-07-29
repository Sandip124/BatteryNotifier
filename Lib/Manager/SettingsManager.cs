using System;
using System.Text;
using System.Windows.Forms;
using BatteryNotifier.Lib.Services;
using BatteryNotifier.Utils;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Lib.Manager
{
    public class SettingsManager : IDisposable
    {
        private readonly Debouncer _debouncer;
        private bool _disposed;

        public SettingsManager()
        {
            _debouncer = new Debouncer();
        }

        public SettingsManager LoadCheckboxSettings(CheckBox pinToNotificationArea, CheckBox launchAtStartup)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SettingsManager));

            UtilityHelper.SafeInvoke(pinToNotificationArea, () =>
            {
                pinToNotificationArea.Checked = appSetting.Default.PinToNotificationArea;
                launchAtStartup.Checked = appSetting.Default.LaunchAtStartup;
            });
            return this;
        }

        public SettingsManager LoadTrackbarSettings(TrackBar fullBatteryTrackbar, TrackBar lowBatteryTrackbar,
            Label fullBatteryPercentageLabel, Label lowBatteryPercentageLabel)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SettingsManager));

            UtilityHelper.SafeInvoke(fullBatteryTrackbar, () =>
            {
                fullBatteryTrackbar.Value = appSetting.Default.fullBatteryNotificationValue;
                fullBatteryPercentageLabel.Text = new StringBuilder().Append("(")
                    .Append(appSetting.Default.fullBatteryNotificationValue)
                    .Append("%)")
                    .ToString();
                lowBatteryTrackbar.Value = appSetting.Default.lowBatteryNotificationValue;
                lowBatteryPercentageLabel.Text = new StringBuilder().Append("(")
                    .Append(appSetting.Default.lowBatteryNotificationValue)
                    .Append("%)")
                    .ToString();
            });
            
            BatteryMonitorService.Instance.SetThresholds(
                appSetting.Default.lowBatteryNotificationValue,
                appSetting.Default.fullBatteryNotificationValue);

            return this;
        }

        public SettingsManager LoadSoundSettings(TextBox fullBatterySound, TextBox lowBatterySound)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SettingsManager));

            UtilityHelper.SafeInvoke(fullBatterySound, () =>
            {
                fullBatterySound.Text = appSetting.Default.fullBatteryNotificationMusic;
                lowBatterySound.Text = appSetting.Default.lowBatteryNotificationMusic;
            });
            return this;
        }

        public SettingsManager LoadThemeSettings(RadioButton systemThemeLabel, RadioButton darkThemeLabel,
            RadioButton lightThemeLabel)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SettingsManager));

            UtilityHelper.SafeInvoke(systemThemeLabel, () =>
            {
                if (appSetting.Default.SystemThemeApplied)
                {
                    systemThemeLabel.Checked = true;
                }
                else if (appSetting.Default.darkThemeApplied)
                {
                    darkThemeLabel.Checked = true;
                }
                else
                {
                    lightThemeLabel.Checked = true;
                }
            });

            return this;
        }

        public SettingsManager LoadNotificationSettings(CheckBox fullBatteryCheckbox, CheckBox lowBatteryCheckbox)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SettingsManager));

            UtilityHelper.RenderCheckboxState(fullBatteryCheckbox, appSetting.Default.fullBatteryNotification);
            UtilityHelper.RenderCheckboxState(lowBatteryCheckbox, appSetting.Default.lowBatteryNotification);
            return this;
        }

        public SettingsManager HandleStartupLaunchSetting(bool shouldLaunchAtStartUp)
        {
            try
            {
                var windowsStartupAppsKey = UtilityHelper.GetWindowsStartupAppsKey();
                var startupValue = windowsStartupAppsKey?.GetValue(UtilityHelper.AppName);

                if (shouldLaunchAtStartUp)
                {
                    if (startupValue == null)
                    {
                        windowsStartupAppsKey?.SetValue(UtilityHelper.AppName, Application.ExecutablePath);
                    }
                }
                else
                {
                    if (startupValue != null)
                    {
                        windowsStartupAppsKey?.DeleteValue(UtilityHelper.AppName);
                    }
                }

                appSetting.Default.LaunchAtStartup = shouldLaunchAtStartUp;
                appSetting.Default.Save();
            }
            catch (Exception ex)
            {
                // TODO implement logger  and unified notification Service for internal errors
                Console.WriteLine($"Error handling launch at startup: {ex.Message}");
            }

            return this;
        }

        public void HandleFullBatteryNotificationChange(CheckBox checkbox)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SettingsManager));

            UtilityHelper.RenderCheckboxState(checkbox, checkbox.Checked);
            appSetting.Default.fullBatteryNotification = checkbox.Checked;
            appSetting.Default.Save();
        }

        public void HandleLowBatteryNotificationChange(CheckBox checkbox)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SettingsManager));

            UtilityHelper.RenderCheckboxState(checkbox, checkbox.Checked);
            appSetting.Default.lowBatteryNotification = checkbox.Checked;
            appSetting.Default.Save();
        }

        public void HandleFullBatteryTrackbarChange(int value)
        {
            _debouncer.Debounce(() =>
            {
                appSetting.Default.fullBatteryNotificationValue = value;
                appSetting.Default.Save();
                
                BatteryMonitorService.Instance.SetThresholds(
                    appSetting.Default.lowBatteryNotificationValue,
                    appSetting.Default.fullBatteryNotificationValue);
            }, 500);
        }

        public void HandleLowBatteryTrackbarChange(int value)
        {
            _debouncer.Debounce(() =>
            {
                appSetting.Default.lowBatteryNotificationValue = value;
                appSetting.Default.Save();
                BatteryMonitorService.Instance.SetThresholds(
                    appSetting.Default.lowBatteryNotificationValue,
                    appSetting.Default.fullBatteryNotificationValue);
            }, 500);
        }

        public void SaveFullBatterySoundPath(string soundPath)
        {
            appSetting.Default.fullBatteryNotificationMusic = soundPath.Trim();
            appSetting.Default.Save();
        }

        public void SaveLowBatterySoundPath(string soundPath)
        {
            appSetting.Default.lowBatteryNotificationMusic = soundPath.Trim();
            appSetting.Default.Save();
        }

        public void UpdatePinToNotificationArea(bool isChecked)
        {
            appSetting.Default.PinToNotificationArea = isChecked;
            appSetting.Default.Save();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _debouncer?.Dispose();
                _disposed = true;
            }
        }
    }
}