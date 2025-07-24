using System;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Constants;
using BatteryNotifier.Forms;
using BatteryNotifier.Lib.Providers;
using BatteryNotifier.Utils;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Lib.Manager
{
    public class ContextMenuManager : IDisposable
    {
        private readonly NotificationManager _notificationManager;
        private bool _disposed;

        private readonly ContextMenuStrip? _contextMenu;
        private ToolStripMenuItem _fullBatteryNotificationItem;
        private ToolStripMenuItem _lowBatteryNotificationItem;
        private ToolStripMenuItem _startMinimizedItem;

        private readonly Dashboard dashboard;

        public ContextMenuManager(NotificationManager notificationManager, Dashboard dashboard)
        {
            this.dashboard = dashboard;
            _notificationManager = notificationManager;
            _contextMenu = new ContextMenuStrip { TopLevel = true };
        }

        public ContextMenuStrip? InitializeContextMenu(NotifyIcon notifyIcon, Label notificationLabel)
        {
            _contextMenu?.Items.Clear();
            
            CreateFullBatteryNotificationItem(notifyIcon, notificationLabel);

            CreateLowBatteryNotificationItem(notifyIcon, notificationLabel);

            CreateStartMinimizedMenuItem(notifyIcon, notificationLabel);

            var viewSourceItem = CreateViewSourceMenuItem();

            var exitAppItem = CreateExitApplicationMenuItem();

            _contextMenu?.Items.AddRange([
                _fullBatteryNotificationItem,
                _lowBatteryNotificationItem,
                _startMinimizedItem,
                viewSourceItem,
                exitAppItem
            ]);

            _fullBatteryNotificationItem.Text =
                "Full Battery Notification" + (appSetting.Default.fullBatteryNotification ? " ✔" : "");
            _lowBatteryNotificationItem.Text =
                "Low Battery Notification" + (appSetting.Default.lowBatteryNotification ? " ✔" : "");
            _startMinimizedItem.Text = "Start Minimized" + (appSetting.Default.startMinimized ? " ✔" : "");


            return _contextMenu;
        }

        private ToolStripMenuItem CreateExitApplicationMenuItem()
        {
            // Exit Application
            var exitAppItem = new ToolStripMenuItem
            {
                Text = "Exit Application",
                Name = "ExitApp",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            exitAppItem.Click += (s, e) => ExitApp_Click();
            return exitAppItem;
        }

        private ToolStripMenuItem CreateViewSourceMenuItem()
        {
            // View Source
            var viewSourceItem = new ToolStripMenuItem
            {
                Text = "View Source",
                Name = "ViewSource",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            viewSourceItem.Click += (s, e) => ViewSource_Click();
            return viewSourceItem;
        }

        private void CreateStartMinimizedMenuItem(NotifyIcon notifyIcon, Label notificationLabel)
        {
            // Start Minimized
            _startMinimizedItem = new ToolStripMenuItem
            {
                Name = "StartMinimized",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            _startMinimizedItem.Click += (s, e) => StartMinimized_Click(notifyIcon, notificationLabel);
        }

        private void CreateLowBatteryNotificationItem(NotifyIcon notifyIcon, Label notificationLabel)
        {
            // Low Battery Notification
            _lowBatteryNotificationItem = new ToolStripMenuItem
            {
                Name = "LowBatteryNotification",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            _lowBatteryNotificationItem.Click += (s, e) => LowBatteryNotification_Click(notifyIcon, notificationLabel);
        }

        private void CreateFullBatteryNotificationItem(NotifyIcon notifyIcon, Label notificationLabel)
        {
            // Full Battery Notification
            _fullBatteryNotificationItem = new ToolStripMenuItem
            {
                Name = "FullBatteryNotification",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            _fullBatteryNotificationItem.Click +=
                (s, e) => FullBatteryNotification_Click(notifyIcon, notificationLabel);
        }

        public void AttachContextMenu(NotifyIcon notifyIcon, Label notificationLabel)
        {
            notifyIcon.ContextMenuStrip = InitializeContextMenu(notifyIcon, notificationLabel);
            notifyIcon.Click += (s, e) => BatteryNotifierIcon_Click();
            notifyIcon.BalloonTipClicked += (s, e) => BatteryNotifierIcon_BalloonTipClicked();
            notifyIcon.BalloonTipClosed += (s, e) => BatteryNotifierIcon_BalloonTipClosed();
        }

        private void FullBatteryNotification_Click(NotifyIcon notifyIcon, Label notificationLabel)
        {
            appSetting.Default.fullBatteryNotification = !appSetting.Default.fullBatteryNotification;
            appSetting.Default.Save();

            _notificationManager.ShowInAppNotification(
                notificationLabel,
                "🔔 Full Battery Notification " +
                (appSetting.Default.fullBatteryNotification ? "Enabled" : "Disabled"));

            UpdateContextMenu(notifyIcon, notificationLabel);
        }

        private void LowBatteryNotification_Click(NotifyIcon notifyIcon, Label notificationLabel)
        {
            appSetting.Default.lowBatteryNotification = !appSetting.Default.lowBatteryNotification;
            appSetting.Default.Save();

            _notificationManager.ShowInAppNotification(
                notificationLabel,
                "🔔 Low Battery Notification " +
                (appSetting.Default.lowBatteryNotification ? "Enabled" : "Disabled"));

            UpdateContextMenu(notifyIcon, notificationLabel);
        }

        private void StartMinimized_Click(NotifyIcon notifyIcon, Label notificationLabel)
        {
            appSetting.Default.startMinimized = !appSetting.Default.startMinimized;
            appSetting.Default.Save();

            _notificationManager.ShowInAppNotification(
                notificationLabel,
                "🔔 Start Minimized " +
                (appSetting.Default.startMinimized ? "Enabled" : "Disabled"));

            UpdateContextMenu(notifyIcon, notificationLabel);
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
        }

        private void BatteryNotifierIcon_BalloonTipClicked()
        {
            dashboard.Show();
            dashboard.Activate();
        }

        private void BatteryNotifierIcon_BalloonTipClosed()
        {
            var soundManager = new SoundManager();
            soundManager.StopSound();
        }

        private void UpdateContextMenu(NotifyIcon notifyIcon, Label notificationLabel)
        {
            notifyIcon.ContextMenuStrip = InitializeContextMenu(notifyIcon, notificationLabel);
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