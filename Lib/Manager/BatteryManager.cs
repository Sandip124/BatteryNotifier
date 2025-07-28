using System;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Lib.Services;
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

        public void RefreshBatteryStatus(BatteryStatusEventArgs e)
        {
            UtilityHelper.SafeInvoke(_batteryStatusLabel, () =>
            {
                if (e.PowerLineStatus == PowerLineStatus.Online &&
                    e.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery && !e.IsCharging)
                {
                    _batteryStatusLabel.Text = @"⚡ Charging";
                    _batteryStatusLabel.ForeColor = Color.ForestGreen;
                    UpdateChargingAnimation();
                }
                else if (e.PowerLineStatus == PowerLineStatus.Offline ||
                         e.PowerLineStatus == PowerLineStatus.Unknown)
                {
                    _batteryStatusLabel.Text = @"🙄 Not Charging";
                    _batteryStatusLabel.ForeColor = Color.Gray;
                    SetBatteryChargeStatus(e);
                }
                else if (e.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
                {
                    _batteryStatusLabel.Text = @"💀 Are you running on main power !!";
                    _batteryImage.Image = ImageCache.Unknown;
                }
                else if (e.BatteryChargeStatus == BatteryChargeStatus.Unknown)
                {
                    _batteryStatusLabel.Text = @"😇 Only God knows about this battery !!";
                    _batteryImage.Image = ImageCache.Unknown;
                }

                UpdateBatteryPercentage(e);
                UpdateBatteryChargeRemainingStatus(e);
            });

           
        }

        public void UpdateChargingAnimation()
        {
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


        private void UpdateBatteryChargeRemainingStatus(BatteryStatusEventArgs e)
        {
            UtilityHelper.SafeInvoke(_remainingTimeLabel, () =>
            {
                if (e.BatteryLifeRemaining >= 0)
                {
                    var timeSpan = TimeSpan.FromSeconds(e.BatteryLifeRemaining);
                    _remainingTimeLabel.Text = $@"{timeSpan.Hours} hr {timeSpan.Minutes} min remaining";
                    return;
                }

                _remainingTimeLabel.Text = $@"{e.BatteryLevel}% remaining";
            });
        }

        private void UpdateBatteryPercentage(BatteryStatusEventArgs e)
        {
            UtilityHelper.SafeInvoke(_batteryPercentageLabel, () =>
            {
                var powerPercent = e.BatteryLevel;
                _batteryPercentageLabel.Text = $@"{(powerPercent <= 100 ? powerPercent.ToString() : "0")}%";
            });
        }

        private void SetBatteryChargeStatus(BatteryStatusEventArgs e)
        {
            if (e.IsCharging) return;

            UtilityHelper.SafeInvoke(_batteryStatusLabel, () =>
            {
                switch (e.BatteryLevel)
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