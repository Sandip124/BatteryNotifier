using System.Diagnostics;

namespace BatteryNotifier
{
    public partial class Dashboard : Form
    {
        const string DeveloperUrl = "https://github.com/Sandip124/BatteryNotifier/";
        private Point LastLocation;
        private bool MouseDown;

        public Dashboard()
        {
            InitializeComponent();
            InitializeContextMenu();
            SetDefaultLocation();
        }

        private void AppHeader_MouseDown(object sender, MouseEventArgs e)
        {
            MouseDown = true;
            LastLocation = e.Location;
        }

        private void AppHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseDown)
            {
                Location = new Point(
                    Location.X - LastLocation.X + e.X, Location.Y - LastLocation.Y + e.Y);

                Update();
            }

        }

        private void AppHeader_MouseUp(object sender, MouseEventArgs e)
        {
            MouseDown = false;
        }

        private void AppHeaderTitle_MouseDown(object sender, MouseEventArgs e)
        {
            MouseDown = true;
            LastLocation = e.Location;
        }

        private void AppHeaderTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseDown)
            {
                Location = new Point(
                    Location.X - LastLocation.X + e.X, Location.Y - LastLocation.Y + e.Y);

                Update();
            }
        }

        private void AppHeaderTitle_MouseUp(object sender, MouseEventArgs e)
        {
            MouseDown = false;
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
    }
}
