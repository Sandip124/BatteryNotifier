using System;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Properties;
using BatteryNotifier.Utils;

namespace BatteryNotifier.Lib.Manager
{
    public class BatteryManager : IDisposable
    {
        private bool _disposed;

        private readonly Label _batteryStatusLabel;
        private readonly Label _batteryPercentageLabel;
        private readonly Label _remainingTimeLabel;
        private readonly PictureBox _batteryImage;

        public BatteryManager(Label batteryStatusLabel, Label batteryPercentageLabel, Label remainingTimeLabel,
            PictureBox batteryImage)
        {
            if (batteryStatusLabel == null || batteryPercentageLabel == null || remainingTimeLabel == null ||
                batteryImage == null)
                throw new ArgumentNullException(@"One or more controls are null");

            _batteryStatusLabel = batteryStatusLabel;
            _batteryPercentageLabel = batteryPercentageLabel;
            _remainingTimeLabel = remainingTimeLabel;
            _batteryImage = batteryImage;
        }

        public void RefreshBatteryStatus()
        {
            var powerStatus = SystemInformation.PowerStatus;
            
            UtilityHelper.SafeInvoke(_batteryStatusLabel, () =>
            {
                if (powerStatus.PowerLineStatus == PowerLineStatus.Online &&
                    powerStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery && powerStatus.BatteryChargeStatus != BatteryChargeStatus.Charging)
                {
                    _batteryStatusLabel.Text = @"⚡ Charging";
                    _batteryStatusLabel.ForeColor = Color.ForestGreen;
                    UpdateChargingAnimation();
                }
                else if (powerStatus.PowerLineStatus == PowerLineStatus.Offline ||
                         powerStatus.PowerLineStatus == PowerLineStatus.Unknown)
                {
                    _batteryStatusLabel.Text = @"🙄 Not Charging";
                    _batteryStatusLabel.ForeColor = Color.Gray;
                    SetBatteryChargeStatus(powerStatus);
                }
                else if (powerStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
                {
                    _batteryStatusLabel.Text = @"💀 Are you running on main power !!";
                    _batteryImage.Image = ImageCache.Unknown;
                }
                else if (powerStatus.BatteryChargeStatus == BatteryChargeStatus.Unknown)
                {
                    _batteryStatusLabel.Text = @"😇 Only God knows about this battery !!";
                    _batteryImage.Image = ImageCache.Unknown;
                }

                UpdateBatteryPercentage(powerStatus);
                UpdateBatteryChargeRemainingStatus(powerStatus);
            });

           
        }

        public void UpdateChargingAnimation()
        {
            if (SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online ||
                SystemInformation.PowerStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery ||
                SystemInformation.PowerStatus.BatteryChargeStatus == BatteryChargeStatus.Charging) return;
            
            var desiredImage = ThemeUtils.IsDarkTheme
                ? ImageCache.ChargingAnimatedDark
                : ImageCache.ChargingAnimated;

            UtilityHelper.SafeInvoke(_batteryImage, () =>
            {
                if (_batteryImage.Image != desiredImage)
                {
                    _batteryImage.Image = desiredImage;
                }
            });
        }


        private void UpdateBatteryChargeRemainingStatus(PowerStatus powerStatus)
        {
            UtilityHelper.SafeInvoke(_remainingTimeLabel, () =>
            {
                if (powerStatus.BatteryLifeRemaining >= 0)
                {
                    var timeSpan = TimeSpan.FromSeconds(powerStatus.BatteryLifeRemaining);
                    _remainingTimeLabel.Text = $@"{timeSpan.Hours} hr {timeSpan.Minutes} min remaining";
                    return;
                }

                _remainingTimeLabel.Text = $@"{Math.Round(powerStatus.BatteryLifePercent *100,0)}% remaining";
            });
        }

        private void UpdateBatteryPercentage(PowerStatus powerStatus)
        {
            UtilityHelper.SafeInvoke(_batteryPercentageLabel, () =>
            {
                var powerPercent = (int)(powerStatus.BatteryLifePercent * 100);
                _batteryPercentageLabel.Text = $@"{(powerPercent <= 100 ? powerPercent.ToString() : "0")}%";
            });
        }

        private void SetBatteryChargeStatus(PowerStatus powerStatus)
        {
            if (powerStatus.BatteryChargeStatus == BatteryChargeStatus.Charging) return;

            UtilityHelper.SafeInvoke(_batteryStatusLabel, () =>
            {
                var powerPercent = (int)(powerStatus.BatteryLifePercent * 100);
                switch (powerPercent)
                {
                    case >= 96:
                        _batteryStatusLabel.Text = @"Full Battery";
                        _batteryImage.Image = ImageCache.Full;
                        break;
                    case >= 60 and <= 96:
                        _batteryStatusLabel.Text = @"Adequate Battery";
                        _batteryImage.Image = ImageCache.Sufficient;
                        break;
                    case >= 40 and <= 60:
                        _batteryStatusLabel.Text = @"Sufficient Battery";
                        _batteryImage.Image = ImageCache.Normal;
                        break;
                    case < 40 and > 14:
                        _batteryStatusLabel.Text = @"Battery Low";
                        _batteryImage.Image = ImageCache.Low;
                        break;
                    case <= 14:
                        _batteryStatusLabel.Text = @"Battery Critical";
                        _batteryImage.Image = ImageCache.Critical;
                        break;
                }
            });
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
                _disposed = true;
            }
        }
    }

    internal static class ImageCache
    {
        public static readonly Image Full = Resources.Full;
        public static readonly Image Sufficient = Resources.Sufficient;
        public static readonly Image Normal = Resources.Normal;
        public static readonly Image Low = Resources.Low;
        public static readonly Image Critical = Resources.Critical;
        public static readonly Image Unknown = Resources.Unknown;
        public static readonly Image ChargingAnimated = Resources.ChargingBatteryAnimated;
        public static readonly Image ChargingAnimatedDark = Resources.ChargingBatteryAnimatedDark;
    }
}