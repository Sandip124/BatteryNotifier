using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;

namespace BatteryNotifier.Lib.CustomControls.FlatTabControl
{

    //https://www.codeproject.com/Articles/12185/A-NET-Flat-TabControl-CustomDraw
    
    /// <summary>
    /// Summary description for FlatTabControl.
    /// </summary>
    [ToolboxBitmap(typeof(TabControl))]

    public class FlatTabControl : TabControl
    {

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private Container components = null;
        private SubClass scUpDown = null;
        private bool bUpDown; // true when the button UpDown is required
        private ImageList leftRightImages = null;
        private const int nMargin = 5;
        private Color mBackColor = Color.Transparent;
        private Color mBorderColor = Color.Transparent;

        public FlatTabControl()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // double buffering
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            bUpDown = false;

            ControlAdded += new ControlEventHandler(FlatTabControl_ControlAdded);
            ControlRemoved += new ControlEventHandler(FlatTabControl_ControlRemoved);
            SelectedIndexChanged += new EventHandler(FlatTabControl_SelectedIndexChanged);

            leftRightImages = new ImageList();

            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(FlatTabControl));
            Bitmap updownImage = (Bitmap)resources.GetObject("TabIcons.bmp");

            if (updownImage != null)
            {
                updownImage.MakeTransparent(Color.White);
                leftRightImages.Images.AddStrip(updownImage);
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                leftRightImages.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawControl(e.Graphics);
        }

        internal void DrawControl(Graphics g)
        {
            if (!Visible)
                return;

            Rectangle TabControlArea = ClientRectangle;
            Rectangle TabArea = DisplayRectangle;

            //----------------------------
            // fill client area
            Brush br = new SolidBrush(mBackColor); //(SystemColors.Control); UPDATED
            g.FillRectangle(br, TabControlArea);
            br.Dispose();
            //----------------------------

            //----------------------------
            // draw border
            int nDelta = SystemInformation.Border3DSize.Width;

            Pen border = new Pen(mBorderColor);
            TabArea.Inflate(nDelta, nDelta);
            g.DrawRectangle(border, TabArea);
            border.Dispose();
            //----------------------------


            //----------------------------
            // clip region for drawing tabs
            Region rsaved = g.Clip;
            Rectangle rreg;

            int nWidth = TabArea.Width + nMargin;
            if (bUpDown)
            {
                // exclude updown control for painting
                if (Win32.IsWindowVisible(scUpDown.Handle))
                {
                    Rectangle rupdown = new Rectangle();
                    Win32.GetWindowRect(scUpDown.Handle, ref rupdown);
                    Rectangle rupdown2 = RectangleToClient(rupdown);

                    nWidth = rupdown2.X;
                }
            }

            rreg = new Rectangle(TabArea.Left, TabControlArea.Top, nWidth - nMargin, TabControlArea.Height);

            g.SetClip(rreg);

            // draw tabs
            for (int i = 0; i < TabCount; i++)
                DrawTab(g, TabPages[i], i);

            g.Clip = rsaved;
            //----------------------------


            //----------------------------
            // draw background to cover flat border areas
            if (SelectedTab != null)
            {
                TabPage tabPage = SelectedTab;
                Color color = tabPage.BackColor;
                border = new Pen(color);

                TabArea.Offset(1, 1);
                TabArea.Width -= 2;
                TabArea.Height -= 2;

                g.DrawRectangle(border, TabArea);
                TabArea.Width -= 1;
                TabArea.Height -= 1;
                g.DrawRectangle(border, TabArea);

                border.Dispose();
            }
            //----------------------------
        }

        internal void DrawTab(Graphics g, TabPage tabPage, int nIndex)
        {
            Rectangle recBounds = GetTabRect(nIndex);
            RectangleF tabTextArea = (RectangleF)GetTabRect(nIndex);

            bool bSelected = SelectedIndex == nIndex;

            Point[] pt = new Point[7];
            if (Alignment == TabAlignment.Top)
            {
                pt[0] = new Point(recBounds.Left, recBounds.Bottom);
                pt[1] = new Point(recBounds.Left, recBounds.Top + 3);
                pt[2] = new Point(recBounds.Left + 3, recBounds.Top);
                pt[3] = new Point(recBounds.Right - 3, recBounds.Top);
                pt[4] = new Point(recBounds.Right, recBounds.Top + 3);
                pt[5] = new Point(recBounds.Right, recBounds.Bottom);
                pt[6] = new Point(recBounds.Left, recBounds.Bottom);
            }
            else
            {
                pt[0] = new Point(recBounds.Left, recBounds.Top);
                pt[1] = new Point(recBounds.Right, recBounds.Top);
                pt[2] = new Point(recBounds.Right, recBounds.Bottom - 3);
                pt[3] = new Point(recBounds.Right - 3, recBounds.Bottom);
                pt[4] = new Point(recBounds.Left + 3, recBounds.Bottom);
                pt[5] = new Point(recBounds.Left, recBounds.Bottom - 3);
                pt[6] = new Point(recBounds.Left, recBounds.Top);
            }

            //----------------------------
            // fill this tab with background color
            Brush br = new SolidBrush(tabPage.BackColor);
            g.FillPolygon(br, pt);
            br.Dispose();
            //----------------------------

            //----------------------------
            // draw border
            g.DrawPolygon(new Pen(new SolidBrush(mBorderColor)), pt);

            if (bSelected)
            {
                //----------------------------
                // clear bottom lines
                Pen pen = new Pen(tabPage.BackColor);

                switch (Alignment)
                {
                    case TabAlignment.Top:
                        g.DrawLine(pen, recBounds.Left + 1, recBounds.Bottom, recBounds.Right - 1, recBounds.Bottom);
                        g.DrawLine(pen, recBounds.Left + 1, recBounds.Bottom + 1, recBounds.Right - 1, recBounds.Bottom + 1);
                        break;

                    case TabAlignment.Bottom:
                        g.DrawLine(pen, recBounds.Left + 1, recBounds.Top, recBounds.Right - 1, recBounds.Top);
                        g.DrawLine(pen, recBounds.Left + 1, recBounds.Top - 1, recBounds.Right - 1, recBounds.Top - 1);
                        g.DrawLine(pen, recBounds.Left + 1, recBounds.Top - 2, recBounds.Right - 1, recBounds.Top - 2);
                        break;
                }

                pen.Dispose();
                //----------------------------
            }
            //----------------------------

            //----------------------------
            // draw tab's icon
            if (tabPage.ImageIndex >= 0 && ImageList != null && ImageList.Images[tabPage.ImageIndex] != null)
            {
                int nLeftMargin = 8;
                int nRightMargin = 2;

                Image img = ImageList.Images[tabPage.ImageIndex];

                Rectangle rimage = new Rectangle(recBounds.X + nLeftMargin, recBounds.Y + 1, img.Width, img.Height);

                // adjust rectangles
                float nAdj = nLeftMargin + img.Width + nRightMargin;

                rimage.Y += (recBounds.Height - img.Height) / 2;
                tabTextArea.X += nAdj;
                tabTextArea.Width -= nAdj;

                // draw icon
                g.DrawImage(img, rimage);
            }
            //----------------------------

            //----------------------------
            // draw string
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            br = new SolidBrush(tabPage.ForeColor);

            g.DrawString(tabPage.Text, Font, br, tabTextArea, stringFormat);
            //----------------------------
        }

        internal void DrawIcons(Graphics g)
        {
            if (leftRightImages == null || leftRightImages.Images.Count != 4)
                return;

            //----------------------------
            // calc positions
            Rectangle TabControlArea = ClientRectangle;

            Rectangle r0 = new Rectangle();
            Win32.GetClientRect(scUpDown.Handle, ref r0);

            Brush br = new SolidBrush(SystemColors.Control);
            g.FillRectangle(br, r0);
            br.Dispose();

            Pen border = new Pen(SystemColors.ControlDark);
            Rectangle rborder = r0;
            rborder.Inflate(-1, -1);
            g.DrawRectangle(border, rborder);
            border.Dispose();

            int nMiddle = r0.Width / 2;
            int nTop = (r0.Height - 16) / 2;
            int nLeft = (nMiddle - 16) / 2;

            Rectangle r1 = new Rectangle(nLeft, nTop, 16, 16);
            Rectangle r2 = new Rectangle(nMiddle + nLeft, nTop, 16, 16);
            //----------------------------

            //----------------------------
            // draw buttons
            Image img = leftRightImages.Images[1];
            if (img != null)
            {
                if (TabCount > 0)
                {
                    Rectangle r3 = GetTabRect(0);
                    if (r3.Left < TabControlArea.Left)
                        g.DrawImage(img, r1);
                    else
                    {
                        img = leftRightImages.Images[3];
                        if (img != null)
                            g.DrawImage(img, r1);
                    }
                }
            }

            img = leftRightImages.Images[0];
            if (img != null)
            {
                if (TabCount > 0)
                {
                    Rectangle r3 = GetTabRect(TabCount - 1);
                    if (r3.Right > TabControlArea.Width - r0.Width)
                        g.DrawImage(img, r2);
                    else
                    {
                        img = leftRightImages.Images[2];
                        if (img != null)
                            g.DrawImage(img, r2);
                    }
                }
            }
            //----------------------------
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            FindUpDown();
        }

        private void FlatTabControl_ControlAdded(object? sender, ControlEventArgs e)
        {
            FindUpDown();
            UpdateUpDown();
        }

        private void FlatTabControl_ControlRemoved(object? sender, ControlEventArgs e)
        {
            FindUpDown();
            UpdateUpDown();
        }

        private void FlatTabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateUpDown();
            Invalidate();   // we need to update border and background colors
        }

        private void FindUpDown()
        {
            bool bFound = false;

            // find the UpDown control
            IntPtr pWnd = Win32.GetWindow(Handle, Win32.GW_CHILD);

            while (pWnd != IntPtr.Zero)
            {
                //----------------------------
                // Get the window class name
                char[] className = new char[33];

                int length = Win32.GetClassName(pWnd, className, 32);

                string s = new string(className, 0, length);
                //----------------------------

                if (s == "msctls_updown32")
                {
                    bFound = true;

                    if (!bUpDown)
                    {
                        //----------------------------
                        // Subclass it
                        scUpDown = new SubClass(pWnd, true);
                        scUpDown.SubClassedWndProc += new SubClass.SubClassWndProcEventHandler(scUpDown_SubClassedWndProc);
                        //----------------------------

                        bUpDown = true;
                    }
                    break;
                }

                pWnd = Win32.GetWindow(pWnd, Win32.GW_HWNDNEXT);
            }

            if (!bFound && bUpDown)
                bUpDown = false;
        }

        private void UpdateUpDown()
        {
            if (bUpDown)
            {
                if (Win32.IsWindowVisible(scUpDown.Handle))
                {
                    Rectangle rect = new Rectangle();

                    Win32.GetClientRect(scUpDown.Handle, ref rect);
                    Win32.InvalidateRect(scUpDown.Handle, ref rect, true);
                }
            }
        }

        #region scUpDown_SubClassedWndProc Event Handler

        private int scUpDown_SubClassedWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Win32.WM_PAINT:
                    {
                        //------------------------
                        // redraw
                        IntPtr hDC = Win32.GetWindowDC(scUpDown.Handle);
                        Graphics g = Graphics.FromHdc(hDC);

                        DrawIcons(g);

                        g.Dispose();
                        Win32.ReleaseDC(scUpDown.Handle, hDC);
                        //------------------------

                        // return 0 (processed)
                        m.Result = IntPtr.Zero;

                        //------------------------
                        // validate current rect
                        Rectangle rect = new Rectangle();

                        Win32.GetClientRect(scUpDown.Handle, ref rect);
                        Win32.ValidateRect(scUpDown.Handle, ref rect);
                        //------------------------
                    }
                    return 1;
            }

            return 0;
        }
        #endregion

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new Container();
        }


        #endregion

        #region Properties

        [Editor(typeof(TabpageExCollectionEditor), typeof(UITypeEditor))]
        public new TabPageCollection TabPages
        {
            get
            {
                return base.TabPages;
            }
        }

        new public TabAlignment Alignment
        {
            get { return base.Alignment; }
            set
            {
                TabAlignment ta = value;
                if (ta != TabAlignment.Top && ta != TabAlignment.Bottom)
                    ta = TabAlignment.Top;

                base.Alignment = ta;
            }
        }

        [Browsable(false)]
        new public bool Multiline
        {
            get { return base.Multiline; }
            set { base.Multiline = false; }
        }

        [Browsable(true)]
        public override Color BackColor
        {
            get { return mBackColor; }
            set { mBackColor = value; Invalidate(); }
        }

        [Browsable(true)]
        public Color BorderColor
        {
            get { return mBorderColor; }
            set { mBorderColor = value; Invalidate(); }
        }

        #endregion

        #region TabpageExCollectionEditor

        internal class TabpageExCollectionEditor : CollectionEditor
        {
            public TabpageExCollectionEditor(Type type) : base(type)
            {
            }

            protected override Type CreateCollectionItemType()
            {
                return typeof(TabPage);
            }
        }

        #endregion
    }

    //#endregion
}
