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
            Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(workingArea.Right - Size.Width,
                                      workingArea.Bottom - Size.Height);
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
