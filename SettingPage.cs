using BatteryNotifier.Helpers;

namespace BatteryNotifier
{
    public partial class SettingPage : Form
    {
        public SettingPage()
        {
            InitializeComponent();
            SetDefaultLocation();
        }

        private void SetDefaultLocation()
        {
            UIHelper.ShowModal(this, appSetting.Default.showAsModal);
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            this.CloseIcon.Image = Properties.Resources.CloseIconHover;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            this.CloseIcon.Image = Properties.Resources.CloseIcon;
        }

        private void CloseIcon_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SettingPage_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            ShowAsWindow.Checked = appSetting.Default.showAsModal;

            showFullBatteryNotification.Checked = appSetting.Default.fullBatteryNotification;
            fullBatteryTrackbar.Value = appSetting.Default.fullBatteryNotificationValue;
            fullbatteryPercentageValue.Value = appSetting.Default.fullBatteryNotificationValue;
            fullbatterySoundPath.Text = appSetting.Default.fullBatteryNotificationMusic;

            showLowBatteryNotification.Checked = appSetting.Default.lowBatteryNotification;
            lowBatteryTrackbar.Value = appSetting.Default.lowBatteryNotificationValue;
            lowBatteryPercentageValue.Value = appSetting.Default.lowBatteryNotificationValue;
            lowBatterySoundPath.Text = appSetting.Default.lowBatteryNotificationMusic;
        }

        private void fullBatteryTrackbar_Scroll(object sender, EventArgs e)
        {
            fullbatteryPercentageValue.Value = fullBatteryTrackbar.Value;
        }

        private void lowBatteryTrackbar_Scroll(object sender, EventArgs e)
        {
            lowBatteryPercentageValue.Value = lowBatteryTrackbar.Value;
        }

        private void browseFullBatterySoundButton_Click(object sender, EventArgs e)
        {
            var fileBrowser = new OpenFileDialog();
            fileBrowser.ShowDialog();

            if (fileBrowser.CheckFileExists)
            {
                fullbatterySoundPath.Text = fileBrowser.FileName;
                appSetting.Default.fullBatteryNotificationMusic = fullbatterySoundPath.Text;
                appSetting.Default.Save();
            }
        }

        private void browseLowBatterySoundButton_Click(object sender, EventArgs e)
        {
            var fileBrowser = new OpenFileDialog();
            fileBrowser.ShowDialog();

            if (fileBrowser.CheckFileExists)
            {
                lowBatterySoundPath.Text = fileBrowser.FileName;
                appSetting.Default.lowBatteryNotificationMusic = lowBatterySoundPath.Text;
                appSetting.Default.Save();
            }
            
        }

        private void ShowAsWindow_CheckedChanged(object sender, EventArgs e)
        {
            appSetting.Default.showAsModal = ShowAsWindow.Checked;
            appSetting.Default.Save();
        }

        private void showFullBatteryNotification_CheckedChanged(object sender, EventArgs e)
        {
            appSetting.Default.fullBatteryNotification = showFullBatteryNotification.Checked;
            appSetting.Default.Save();
        }

        private void showLowBatteryNotification_CheckedChanged(object sender, EventArgs e)
        {
            appSetting.Default.lowBatteryNotification = showLowBatteryNotification.Checked;
            appSetting.Default.Save();
        }

        private void fullBatteryTrackbar_ValueChanged(object sender, EventArgs e)
        {
            appSetting.Default.fullBatteryNotificationValue = fullBatteryTrackbar.Value;
            appSetting.Default.Save();
        }

        private void lowBatteryTrackbar_ValueChanged(object sender, EventArgs e)
        {
            appSetting.Default.lowBatteryNotificationValue = lowBatteryTrackbar.Value;
            appSetting.Default.Save();
        }

        private void SettingPage_Activated(object sender, EventArgs e)
        {
            LoadSettings();
        }
    }
}
