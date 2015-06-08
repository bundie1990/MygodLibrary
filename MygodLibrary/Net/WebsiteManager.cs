using System;
using System.Diagnostics;
using System.Net;
using Mygod.Windows;

namespace Mygod.Net
{
    public static class WebsiteManager
    {
        private static readonly WebClient Client = new WebClient();

        private static string UpdateUrl
        {
            get { return "http://mygod.tk/product/update/" + CurrentApp.Version.Revision + '/'; }
        }
        public static string Url { get { return Client.DownloadString(UpdateUrl); } }

        public static void CheckForUpdates(Action noUpdates = null, Action<Exception> errorCallback = null)
        {
            try
            {
                var url = Url;
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
