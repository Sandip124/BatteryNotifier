using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
    }
}
