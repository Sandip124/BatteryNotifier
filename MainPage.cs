namespace BatteryNotifier
{
    public partial class Dashboard : Form
    {

        private Point lastLocation;
        private bool mouseDown;

        public Dashboard()
        {
            InitializeComponent();
        }

        private void AppHeader_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void AppHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                Location = new Point(
                    Location.X - lastLocation.X + e.X, Location.Y - lastLocation.Y + e.Y);

                Update();
            }

        }

        private void AppHeader_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void AppHeaderTitle_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void AppHeaderTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                Location = new Point(
                    Location.X - lastLocation.X + e.X, Location.Y - lastLocation.Y + e.Y);

                Update();
            }
        }

        private void AppHeaderTitle_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
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
    }
}
