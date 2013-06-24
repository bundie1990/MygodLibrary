using Microsoft.Win32;

namespace Mygod.Windows
{
    public static class StartupManager
    {
        private static readonly RegistryKey Run = 
            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public static bool IsStartAtWindowsStartup(string applicationID)
        {
            return Run.GetValue(applicationID) == '"' + CurrentApp.Path + '"';
        }
        public static void SetStartAtWindowsStartup(string applicationID, bool start)
        {
            if (start) Run.SetValue(applicationID, '"' + CurrentApp.Path + '"');
            else Run.DeleteValue(applicationID);
        }
    }
}
