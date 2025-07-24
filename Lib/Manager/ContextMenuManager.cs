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
        private readonly SoundManager _soundManager;

        private bool _disposed;

        private readonly ContextMenuStrip? _contextMenu;
        private readonly ToolStripMenuItem _fullBatteryNotificationItem;
        private readonly ToolStripMenuItem _lowBatteryNotificationItem;
        private readonly ToolStripMenuItem _startMinimizedItem;
        private readonly ToolStripMenuItem _viewSourceItem;
        private readonly ToolStripMenuItem _exitAppItem;
        
        private readonly Dashboard dashboard;

        public ContextMenuManager(NotificationManager notificationManager, SoundManager soundManager,
            Dashboard dashboard, Label notificationLabel)
        {
            this.dashboard = dashboard;
            _notificationManager = notificationManager;
            _soundManager = soundManager;
            _contextMenu = new ContextMenuStrip { TopLevel = true };
            _fullBatteryNotificationItem = CreateFullBatteryNotificationItem(notificationLabel);
            _lowBatteryNotificationItem = CreateLowBatteryNotificationItem(notificationLabel);
            _startMinimizedItem = CreateStartMinimizedMenuItem(notificationLabel);
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

            return _contextMenu;
        }

        private void UpdateMenuItemText()
        {
            _fullBatteryNotificationItem.Text =
                "Full Battery Notification" + (appSetting.Default.fullBatteryNotification ? " ✔" : "");
            _lowBatteryNotificationItem.Text =
                "Low Battery Notification" + (appSetting.Default.lowBatteryNotification ? " ✔" : "");
            _startMinimizedItem.Text = "Start Minimized" + (appSetting.Default.startMinimized ? " ✔" : "");
        }

        private ToolStripMenuItem CreateExitApplicationMenuItem()
        {
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

        private ToolStripMenuItem CreateStartMinimizedMenuItem(Label notificationLabel)
        {
            var startMinimizedItem = new ToolStripMenuItem
            {
                Name = "StartMinimized",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            startMinimizedItem.Click += (s, e) => StartMinimized_Click(notificationLabel);
            return startMinimizedItem;
        }

        private ToolStripMenuItem CreateLowBatteryNotificationItem(Label notificationLabel)
        {
            var lowBatteryNotificationItem = new ToolStripMenuItem
            {
                Name = "LowBatteryNotification",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            lowBatteryNotificationItem.Click += (s, e) => LowBatteryNotification_Click(notificationLabel);
            return lowBatteryNotificationItem;
        }

        private ToolStripMenuItem CreateFullBatteryNotificationItem( Label notificationLabel)
        {
            var fullBatteryNotificationItem = new ToolStripMenuItem
            {
                Name = "FullBatteryNotification",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontProvider.GetRegularFont(10.2F)
            };
            fullBatteryNotificationItem.Click +=
                (s, e) => FullBatteryNotification_Click(notificationLabel);
            return fullBatteryNotificationItem;
        }

        public void AttachContextMenu(NotifyIcon notifyIcon)
        {
            if (notifyIcon == null)
                throw new ArgumentNullException(nameof(notifyIcon));
            
            notifyIcon.ContextMenuStrip = InitializeContextMenu();

            notifyIcon.Click -= NotifyIcon_Click;
            notifyIcon.BalloonTipClicked -= NotifyIcon_BalloonTipClicked;
            notifyIcon.BalloonTipClosed -= NotifyIcon_BalloonTipClosed;
            
            notifyIcon.Click += NotifyIcon_Click;
            notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;
            notifyIcon.BalloonTipClosed += NotifyIcon_BalloonTipClosed;
        }
        
        private void NotifyIcon_Click(object sender, EventArgs e) => BatteryNotifierIcon_Click();
        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e) => BatteryNotifierIcon_BalloonTipClicked();
        private void NotifyIcon_BalloonTipClosed(object sender, EventArgs e) => BatteryNotifierIcon_BalloonTipClosed();

        private void FullBatteryNotification_Click(Label notificationLabel)
        {
            appSetting.Default.fullBatteryNotification = !appSetting.Default.fullBatteryNotification;
            appSetting.Default.Save();

            _notificationManager.ShowInAppNotification(
                notificationLabel,
                "🔔 Full Battery Notification " +
                (appSetting.Default.fullBatteryNotification ? "Enabled" : "Disabled"));

            UpdateMenuItemText();
        }

        private void LowBatteryNotification_Click(Label notificationLabel)
        {
            appSetting.Default.lowBatteryNotification = !appSetting.Default.lowBatteryNotification;
            appSetting.Default.Save();

            _notificationManager.ShowInAppNotification(
                notificationLabel,
                "🔔 Low Battery Notification " +
                (appSetting.Default.lowBatteryNotification ? "Enabled" : "Disabled"));

            UpdateMenuItemText();
        }

        private void StartMinimized_Click(Label notificationLabel)
        {
            appSetting.Default.startMinimized = !appSetting.Default.startMinimized;
            appSetting.Default.Save();

            _notificationManager.ShowInAppNotification(
                notificationLabel,
                "🔔 Start Minimized " +
                (appSetting.Default.startMinimized ? "Enabled" : "Disabled"));

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