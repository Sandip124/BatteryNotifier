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
    }
}
