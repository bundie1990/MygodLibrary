using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Mygod.Windows.Input
{
    public static class SystemCursor
    {
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursorFromFile(string lpFileName);

        [DllImport("user32.dll")]
        private static extern bool SetSystemCursor(IntPtr hcur, uint id);

        private static bool cursorShown = true;

        public static void ShowCursor(bool force = false)
        {
            if (!force && cursorShown) return;
            var originalCursor = LoadCursorFromFile(Environment.ExpandEnvironmentVariables(
                Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", "Arrow", null) as string ?? string.Empty));
            SetSystemCursor(originalCursor, 32512);
            cursorShown = true;
        }
        public static void HideCursor(bool force = false)
        {
            if (!force && !cursorShown) return;
            var invisibleCursor = LoadCursorFromFile("Invisible.cur");
            SetSystemCursor(invisibleCursor, 32512);
            cursorShown = false;
        }
    }
}
