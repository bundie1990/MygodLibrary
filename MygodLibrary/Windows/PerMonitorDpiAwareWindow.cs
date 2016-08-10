using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Mygod.Windows
{
    public class PerMonitorDpiAwareWindow : Window
    {
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flag);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy,
                                                uint uFlags);
        [DllImport("SHCore.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, ref int xDpi, ref int yDpi);
        [DllImport("SHCore.dll", SetLastError = true)]
        private static extern int SetProcessDpiAwareness(int awareness);

        private double wpfDpi;
        private int currentDpiX = 96, currentDpiY = 96;
        protected IntPtr WindowHandle;

        public event Action<int, int> DpiChanged;

        public PerMonitorDpiAwareWindow()
        {
            DpiChanged += OnDpiChanged;
            SetProcessDpiAwareness(2);
        }

        private void OnDpiChanged(int dpiX, int dpiY)
        {
            UpdateLayoutTransform((currentDpiX = dpiX) / wpfDpi, (currentDpiY = dpiY) / wpfDpi);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            var source = (HwndSource) PresentationSource.FromVisual(this);
            if (source == null) return;
            source.AddHook(HandleMessages);
            wpfDpi = 96 * source.CompositionTarget.TransformToDevice.M11;
            GetDpiForMonitor(MonitorFromWindow(WindowHandle = source.Handle, 2), 0, ref currentDpiX, ref currentDpiY);
            double scaleFactorX = currentDpiX / wpfDpi, scaleFactorY = currentDpiY / wpfDpi;
            Width *= scaleFactorX;
            Height *= scaleFactorY;
            UpdateLayoutTransform(scaleFactorX, scaleFactorY);
        }

        private IntPtr HandleMessages(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            switch (msg)
            {
                case 0x02E0:    // WM_DPICHANGED
                    unsafe
                    {
                        var newRect = (int*) lparam.ToPointer();    // Left, Top, Right, Bottom
                        SetWindowPos(hwnd, IntPtr.Zero, newRect[0], newRect[1], newRect[2] - newRect[0],
                            newRect[3] - newRect[1], 0x214);    // SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE
                    }
                    var dpi = wparam.ToInt32() & 0xFFFF;
                    if (dpi != currentDpiX || dpi != currentDpiY) DpiChanged(dpi, dpi);
                    break;
            }
            return IntPtr.Zero;
        }

        protected void UpdateLayoutTransform(double scaleFactorX, double scaleFactorY)
        {
            GetVisualChild(0).SetValue(LayoutTransformProperty,
                scaleFactorX == 1 && scaleFactorY == 1 ? null : new ScaleTransform(scaleFactorX, scaleFactorY));
        }
    }
}