using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using BatteryNotifier.Constants;
using BatteryNotifier.Forms;
using BatteryNotifier.Lib.Providers;
using BatteryNotifier.Lib.Services;
using BatteryNotifier.Utils;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Lib.Manager
{
    public class ContextMenuManager : IDisposable
    {
        private readonly SoundManager _soundManager;

        private bool _disposed;

        private readonly ContextMenuStrip? _contextMenu;
        private readonly ToolStripMenuItem _fullBatteryNotificationItem;
        private readonly ToolStripMenuItem _lowBatteryNotificationItem;
        private readonly ToolStripMenuItem _startMinimizedItem;
        private readonly ToolStripMenuItem _viewSourceItem;
        private readonly ToolStripMenuItem _exitAppItem;
        
        private readonly Dashboard dashboard;

        public ContextMenuManager(SoundManager soundManager, Dashboard dashboard)
        {
            this.dashboard = dashboard;
            _soundManager = soundManager;
            _contextMenu = new ContextMenuStrip { TopLevel = true };
            _fullBatteryNotificationItem = CreateFullBatteryNotificationItem();
            _lowBatteryNotificationItem = CreateLowBatteryNotificationItem();
            _startMinimizedItem = CreateStartMinimizedMenuItem();
            _viewSourceItem = CreateViewSourceMenuItem();
            _exitAppItem = CreateExitApplicationMenuItem();
        }

        public ContextMenuStrip? InitializeContextMenu()
        {
            _contextMenu?.Items.Clear();
            UpdateMenuItemText();
            _contextMenu?.Items.AddRange([
                _fullBatteryNotificationItem,
                _lowBatteryNotificationItem,
                _startMinimizedItem,
                _viewSourceItem,
                _exitAppItem
            ]);
            
            ForceGarbageCollection();

            return _contextMenu;
        }

        private void UpdateMenuItemText()
        {
            _fullBatteryNotificationItem.Text =
                new StringBuilder().Append("Full Battery Notification")
                    .Append(appSetting.Default.fullBatteryNotification ? " ✔" : "")
                    .ToString();
            _lowBatteryNotificationItem.Text =
                new StringBuilder().Append("Low Battery Notification")
                    .Append(appSetting.Default.lowBatteryNotification ? " ✔" : "")
                    .ToString();
            _startMinimizedItem.Text = new StringBuilder().Append("Start Minimized")
                .Append(appSetting.Default.startMinimized ? " ✔" : "")
                .ToString();
            
            ForceGarbageCollection();
        }

        private ToolStripMenuItem CreateExitApplicationMenuItem()
        {
            var exitAppItem = new ToolStripMenuItem
            {
                Text = "Exit Application",
                Name = "ExitApp",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.DefaultRegularFont
            };
            exitAppItem.Click += (s, e) => ExitApp_Click();
            return exitAppItem;
        }

        private ToolStripMenuItem CreateViewSourceMenuItem()
        {
            var viewSourceItem = new ToolStripMenuItem
            {
                Text = "View Source",
                Name = "ViewSource",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.DefaultRegularFont
            };
            viewSourceItem.Click += (s, e) => ViewSource_Click();
            return viewSourceItem;
        }

        private ToolStripMenuItem CreateStartMinimizedMenuItem()
        {
            var startMinimizedItem = new ToolStripMenuItem
            {
                Name = "StartMinimized",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.DefaultRegularFont
            };
            startMinimizedItem.Click += (s, e) => StartMinimized_Click();
            return startMinimizedItem;
        }

        private ToolStripMenuItem CreateLowBatteryNotificationItem()
        {
            var lowBatteryNotificationItem = new ToolStripMenuItem
            {
                Name = "LowBatteryNotification",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.DefaultRegularFont
            };
            lowBatteryNotificationItem.Click += (s, e) => LowBatteryNotification_Click();
            return lowBatteryNotificationItem;
        }

        private ToolStripMenuItem CreateFullBatteryNotificationItem()
        {
            var fullBatteryNotificationItem = new ToolStripMenuItem
            {
                Name = "FullBatteryNotification",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.DefaultRegularFont
            };
            fullBatteryNotificationItem.Click +=
                (s, e) => FullBatteryNotification_Click();
            return fullBatteryNotificationItem;
        }

        public void AttachContextMenu(NotifyIcon notifyIcon)
        {
            notifyIcon.ContextMenuStrip = InitializeContextMenu();
            
            notifyIcon.Click += NotifyIcon_Click;
            notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;
            notifyIcon.BalloonTipClosed += NotifyIcon_BalloonTipClosed;
        }
        
        private void NotifyIcon_Click(object sender, EventArgs e) => BatteryNotifierIcon_Click();
        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e) => BatteryNotifierIcon_BalloonTipClicked();
        private void NotifyIcon_BalloonTipClosed(object sender, EventArgs e) => BatteryNotifierIcon_BalloonTipClosed();

        private void FullBatteryNotification_Click()
        {
            appSetting.Default.fullBatteryNotification = !appSetting.Default.fullBatteryNotification;
            appSetting.Default.Save();

            NotificationService.Instance.PublishNotification(
                new StringBuilder().Append("🔔 Full Battery Notification ")
                    .Append(appSetting.Default.fullBatteryNotification ? "Enabled" : "Disabled")
                    .ToString(),NotificationType.Inline);

            UpdateMenuItemText();
        }

        private void LowBatteryNotification_Click()
        {
            appSetting.Default.lowBatteryNotification = !appSetting.Default.lowBatteryNotification;
            appSetting.Default.Save();

            NotificationService.Instance.PublishNotification(
                new StringBuilder().Append("🔔 Low Battery Notification ")
                    .Append(appSetting.Default.lowBatteryNotification ? "Enabled" : "Disabled")
                    .ToString(),NotificationType.Inline);

            UpdateMenuItemText();
        }

        private void StartMinimized_Click()
        {
            appSetting.Default.startMinimized = !appSetting.Default.startMinimized;
            appSetting.Default.Save();

            NotificationService.Instance.PublishNotification(
                new StringBuilder().Append("🔔 Start Minimized ")
                    .Append(appSetting.Default.startMinimized ? "Enabled" : "Disabled")
                    .ToString(),NotificationType.Inline);

            UpdateMenuItemText();
            
        }

        private void ViewSource_Click()
        {
            UtilityHelper.StartExternalUrlProcess(Constant.SourceRepositoryUrl);
        }

        private void ExitApp_Click()
        {
            dashboard.Close();
        }

        private void BatteryNotifierIcon_Click()
        {
            if (dashboard.Visible)
            {
                dashboard.Hide();
            }
            else
            {
                dashboard.Show();
                dashboard.WindowState = FormWindowState.Normal;
            }
            
            ForceGarbageCollection();
        }

        private void BatteryNotifierIcon_BalloonTipClicked()
        {
            dashboard.Show();
            dashboard.Activate();
        }

        private void BatteryNotifierIcon_BalloonTipClosed()
        {
            _soundManager.StopSound();
        }
        
        private void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
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
                _contextMenu?.Dispose();
                _disposed = true;
            }
        }
    }
}