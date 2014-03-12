using System;
using System.Diagnostics;
using System.Net;
using Mygod.Windows;

namespace Mygod.Net
{
    public static class WebsiteManager
    {
        private static readonly WebClient Client = new WebClient();

        public static void CheckForUpdates(Action noUpdates = null, Action<Exception> errorCallback = null)
        {
            try
            {
                var url = Client.DownloadString("http://mygod.tk/product/update/" + CurrentApp.Version.Revision + '/');
                if (!string.IsNullOrWhiteSpace(url)) Process.Start(url);
                else if (noUpdates != null) noUpdates();
            }
            catch (Exception e)
            {
                if (errorCallback == null) throw;
                errorCallback(e);
            }
        }
    }
}
