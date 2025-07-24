using System;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using BatteryNotifier.Properties;
using BatteryNotifier.Utils;

namespace BatteryNotifier.Lib.Manager
{
    public class BatteryManager : IDisposable
    {
        private bool _isCharging;
        private readonly PowerStatus _powerStatus;
        private bool _disposed;
        private readonly Dashboard dashboard;

        public PowerStatus PowerStatus => _powerStatus;

        public BatteryManager(Dashboard dashboard)
        {
            this.dashboard = dashboard;
            _powerStatus = SystemInformation.PowerStatus;
        }

        public void RefreshBatteryStatus(Label batteryStatusLabel ,Label batteryPercentageLabel, Label remainingTimeLabel, PictureBox batteryImage)
        {
            if (!dashboard.Visible) return;

            if (_powerStatus.PowerLineStatus == PowerLineStatus.Online &&
                _powerStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery && !_isCharging)
            {
                _isCharging = true;
                batteryStatusLabel.Text = "⚡ Charging";
                batteryStatusLabel.ForeColor = Color.ForestGreen;
                UpdateChargingAnimation(batteryImage);
            }
            else if (_powerStatus.PowerLineStatus == PowerLineStatus.Offline ||
                     _powerStatus.PowerLineStatus == PowerLineStatus.Unknown)
            {
                _isCharging = false;
                batteryStatusLabel.Text = "🙄 Not Charging";
                batteryStatusLabel.ForeColor = Color.Gray;
                SetBatteryChargeStatus(batteryStatusLabel, batteryImage);
            }
            else if (_powerStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                _isCharging = false;
                batteryStatusLabel.Text = "💀 Are you running on main power !!";
                batteryImage.Image = Resources.Unknown;
            }
            else if (_powerStatus.BatteryChargeStatus == BatteryChargeStatus.Unknown)
            {
                _isCharging = false;
                batteryStatusLabel.Text = "😇 Only God knows about this battery !!";
                batteryImage.Image = Resources.Unknown;
            }

            UpdateBatteryPercentage(batteryPercentageLabel);
            UpdateBatteryChargeRemainingStatus(remainingTimeLabel);
        }

        public void UpdateChargingAnimation(PictureBox? batteryImage)
        {
            if (!_isCharging || batteryImage == null)
                return;

            var desiredImage = ThemeUtils.IsDarkTheme
                ? Resources.ChargingBatteryAnimatedDark
                : Resources.ChargingBatteryAnimated;

            if (batteryImage.Image != desiredImage)
            {
                batteryImage.Image = desiredImage;
            }
        }


        private void UpdateBatteryChargeRemainingStatus(Label remainingTimeLabel)
        {
            if (_powerStatus.BatteryLifeRemaining >= 0)
            {
                var timeSpan = TimeSpan.FromSeconds(_powerStatus.BatteryLifeRemaining);
                remainingTimeLabel.Text = $@"{timeSpan.Hours} hr {timeSpan.Minutes} min remaining";
                return;
            }

            remainingTimeLabel.Text = $@"{Math.Round(_powerStatus.BatteryLifePercent * 100, 0)}% remaining";
        }

        private void UpdateBatteryPercentage(Label batteryPercentageLabel)
        {
            var powerPercent = (int)(_powerStatus.BatteryLifePercent * 100);
            batteryPercentageLabel.Text = $@"{(powerPercent <= 100 ? powerPercent.ToString() : "0")}%";
        }

        private void SetBatteryChargeStatus(Label batteryStatusLabel, PictureBox batteryImage)
        {
            if (_isCharging) return;

            switch (_powerStatus.BatteryLifePercent)
            {
                case >= .96f:
                    batteryStatusLabel.Text = "Full Battery";
                    batteryImage.Image = Resources.Full;
                    break;
                case >= .6f and <= .96f:
                    batteryStatusLabel.Text = "Adequate Battery";
                    batteryImage.Image = Resources.Sufficient;
                    break;
                case >= .4f and <= .6f:
                    batteryStatusLabel.Text = "Sufficient Battery";
                    batteryImage.Image = Resources.Normal;
                    break;
                case < .4f and > .14f:
                    batteryStatusLabel.Text = "Battery Low";
                    batteryImage.Image = Resources.Low;
                    break;
                case <= .14f:
                    batteryStatusLabel.Text = "Battery Critical";
                    batteryImage.Image = Resources.Critical;
                    break;
            }
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
}