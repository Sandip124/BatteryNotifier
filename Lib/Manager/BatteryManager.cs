using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using BatteryNotifier.Lib.Store;
using BatteryNotifier.Properties;
using BatteryNotifier.Utils;

namespace BatteryNotifier.Lib.Manager
{
    public sealed class BatteryManager(
        Label batteryStatusLabel,
        Label batteryPercentageLabel,
        Label remainingTimeLabel,
        PictureBox batteryImage)
        : IDisposable
    {
        private bool _disposed;
        private Label batteryStatusLabel = batteryStatusLabel;
        private Label batteryPercentageLabel = batteryPercentageLabel;
        private Label remainingTimeLabel = remainingTimeLabel;
        private PictureBox batteryImage = batteryImage;

        public void RefreshBatteryStatus()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BatteryManager));

            UtilityHelper.SafeInvoke(batteryStatusLabel, () =>
            {
                if (BatteryManagerStore.Instance.IsCharging)
                {
                    batteryStatusLabel.Text = @"⚡ Charging";
                    batteryStatusLabel.ForeColor = Color.ForestGreen;
                    UpdateChargingAnimation();
                }
                else if (!BatteryManagerStore.Instance.IsCharging || BatteryManagerStore.Instance.IsUnknown)
                {
                    batteryStatusLabel.Text = @"🙄 Not Charging";
                    batteryStatusLabel.ForeColor = Color.Gray;
                    SetBatteryChargeStatus();
                }
                else if (BatteryManagerStore.Instance.HasNoBattery)
                {
                    batteryStatusLabel.Text = @"💀 Are you running on main power !!";
                    batteryImage.Image = ImageCache.Unknown;
                }
                else if (BatteryManagerStore.Instance.IsUnknown)
                {
                    batteryStatusLabel.Text = @"😇 Only God knows about this battery !!";
                    batteryImage.Image = ImageCache.Unknown;
                }

                UpdateBatteryPercentage();
                UpdateBatteryChargeRemainingStatus();
            });
        }

        public void UpdateChargingAnimation()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BatteryManager));

            if (!BatteryManagerStore.Instance.IsCharging) return;

            var desiredImage = ThemeUtils.IsDarkTheme
                ? ImageCache.ChargingAnimatedDark
                : ImageCache.ChargingAnimated;

            UtilityHelper.SafeInvoke(batteryImage, () =>
            {
                if (batteryImage.Image != desiredImage)
                {
                    batteryImage.Image = desiredImage;
                }
            });
        }


        private void UpdateBatteryChargeRemainingStatus()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BatteryManager));

            UtilityHelper.SafeInvoke(remainingTimeLabel, () =>
            {
                if (BatteryManagerStore.Instance.BatteryLifeRemaining >= 0)
                {
                    remainingTimeLabel.Text =
                        $@"{BatteryManagerStore.Instance.BatteryLifeRemainingInSeconds.Hours} hr {BatteryManagerStore.Instance.BatteryLifeRemainingInSeconds.Minutes} min remaining";
                    return;
                }

                remainingTimeLabel.Text = $@"{BatteryManagerStore.Instance.BatteryLifePercent}% remaining";
            });
        }

        private void UpdateBatteryPercentage()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BatteryManager));

            UtilityHelper.SafeInvoke(batteryPercentageLabel,
                () =>
                {
                    batteryPercentageLabel.Text =
                        $@"{(BatteryManagerStore.Instance.BatteryLifePercent <= 100 ?
                            BatteryManagerStore.Instance.BatteryLifePercent.ToString(CultureInfo.InvariantCulture)
                            : "0")}%";
                });
        }

        private void SetBatteryChargeStatus()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BatteryManager));

            if (BatteryManagerStore.Instance.IsCharging) return;

            UtilityHelper.SafeInvoke(batteryStatusLabel, () =>
            {
                switch (BatteryManagerStore.Instance.BatteryState)
                {
                    case BatteryState.Full:
                        batteryStatusLabel.Text = @"Full Battery";
                        batteryImage.Image = ImageCache.Full;
                        break;
                    case BatteryState.Adequate:
                        batteryStatusLabel.Text = @"Adequate Battery";
                        batteryImage.Image = ImageCache.Sufficient;
                        break;
                    case BatteryState.Sufficient:
                        batteryStatusLabel.Text = @"Sufficient Battery";
                        batteryImage.Image = ImageCache.Normal;
                        break;
                    case BatteryState.Low:
                        batteryStatusLabel.Text = @"Battery Low";
                        batteryImage.Image = ImageCache.Low;
                        break;
                    case BatteryState.Critical:
                        batteryStatusLabel.Text = @"Battery Critical";
                        batteryImage.Image = ImageCache.Critical;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }
        
        public void Dispose()
        {
            if (_disposed) return;

            batteryStatusLabel = null;
            batteryPercentageLabel = null;
            remainingTimeLabel = null;
            batteryImage = null;

            _disposed = true;
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