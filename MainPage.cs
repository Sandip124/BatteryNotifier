using System.Diagnostics;

namespace BatteryNotifier
{
    public partial class Dashboard : Form
    {
        const string DeveloperUrl = "https://github.com/Sandip124/BatteryNotifier/";

        public Dashboard()
        {
            InitializeComponent();
            InitializeContextMenu();
            SetDefaultLocation();
        }

        private void CloseIcon_Click(object sender, EventArgs e)
        {
           this.Hide();
        }

        private void CloseIcon_MouseEnter(object sender, EventArgs e)
        {
            this.CloseIcon.Image = Properties.Resources.Close_Square_Hover;
        }

        private void CloseIcon_MouseLeave(object sender, EventArgs e)
        {
            this.CloseIcon.Image = Properties.Resources.Close_Square;
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {

            RefreshBatteryStatus();
            BatteryStatusTimer.Enabled = true;

        }

        private void RefreshBatteryStatus()
        {
            PowerStatus status = SystemInformation.PowerStatus;

            if (status.BatteryChargeStatus == BatteryChargeStatus.Charging)
            {
                BatteryStatus.Text = "Charging";
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.High)
            {
                BatteryStatus.Text = "Full Battery";
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.Low)
            {
                BatteryStatus.Text = "Battery Low";
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.Critical)
            {
                BatteryStatus.Text = "Battery Critical";
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                BatteryStatus.Text = "Looks like you are running on main power !!";
            }
            else if (status.BatteryChargeStatus == BatteryChargeStatus.Unknown)
            {
                BatteryStatus.Text = "Only God knows about this battery !!";
            }

            int powerPercent = (int)(status.BatteryLifePercent * 100);
            if (powerPercent <= 100)
                BatteryPercentage.Text = powerPercent + " %";
            else
                BatteryPercentage.Text = "0 %";


            int secondsRemaining = status.BatteryLifeRemaining;
            if (secondsRemaining >= 0)
            {
                var timeSpan = TimeSpan.FromSeconds(secondsRemaining);
                RemainingTime.Text = string.Format("{0} hr {1} min remaining", timeSpan.Hours, timeSpan.Minutes);
            }
            else
            {
                RemainingTime.Text = "0 min remaining";
            }
        }

        private void SetDefaultLocation()
        {
            Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(workingArea.Right - Size.Width,
                                      workingArea.Bottom - Size.Height);
        }

        private void InitializeContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Clear();

            ToolStripMenuItem exitAppToolStripItem = new("ExitApplication")
            {
                Text = "Exit Application",
                Name = "ExitApp",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 10.2F)
            };
            exitAppToolStripItem.Click += ExitApp_Click!;

            ToolStripMenuItem viewDeveloperToolStripItem = new("ViewDevelopers")
            {
                Text = "View Developer",
                Name = "ViewDeveloper",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 10.2F)
            };
            viewDeveloperToolStripItem.Click += ViewDeveloper_Click!;

            contextMenu.Items.Add(viewDeveloperToolStripItem);
            contextMenu.Items.Add(exitAppToolStripItem);

            BatteryNotifierIcon.ContextMenuStrip = contextMenu;
        }


        private void ExitApp_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ViewDeveloper_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new(DeveloperUrl);
            sInfo.UseShellExecute = true;
            Process.Start(sInfo);
        }

        private void BatteryNotifierIcon_Click(object sender, EventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private void BatteryStatusTimer_Tick(object sender, EventArgs e)
        {
            this.RefreshBatteryStatus();
        }
    }
}
