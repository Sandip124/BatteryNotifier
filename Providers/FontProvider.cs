using System;
using System.Drawing;
using System.Drawing.Text;

namespace BatteryNotifier.Providers
{
    internal class FontProvider
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);

        public PrivateFontCollection FontsCollection = new();

        public static FontProvider Default = new();

        private FontProvider()
        {
            LoadFont(Properties.Resources.Inter_Regular);
            LoadFont(Properties.Resources.Inter_Bold);
        }

        private void LoadFont(byte[] fontResource)
        {
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontResource.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontResource, 0, fontPtr, fontResource.Length);
            uint dummy = 0;
            FontsCollection.AddMemoryFont(fontPtr, fontResource.Length);
            AddFontMemResourceEx(fontPtr, (uint)fontResource.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
        }

        public static Font GetRegularFont(float size = 8)
        {
            return new Font(Default.FontsCollection.Families[0], size, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Font GetBoldFont(float size = 8)
        {
            return new Font(Default.FontsCollection.Families[0], size, FontStyle.Bold, GraphicsUnit.Point);
        }
    }

}
