using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Mygod.Windows
{
    public class CursorWindow : Window
    {
        static CursorWindow()
        {
            TopmostProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(true));
            WindowStyleProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(WindowStyle.None));
            WindowStateProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(WindowState.Maximized));
            ResizeModeProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(ResizeMode.NoResize));
            AllowsTransparencyProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(true));
        }

        public CursorWindow()
        {
            Loaded += OnLoad;
        }

        private IntPtr handle;

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        protected virtual void OnLoad(object sender, RoutedEventArgs e)
        {
            handle = new WindowInteropHelper(this).Handle;
            SetWindowLong(handle, -20, GetWindowLong(handle, -20) | 0x80000 | 0x20);
        }

        public void BringWindowToTop()
        {
            BringWindowToTop(handle);
        }
    }
}
