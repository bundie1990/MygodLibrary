using System;
using System.Diagnostics;
using System.Net;

namespace Mygod.Net
{
    public static class WebsiteManager
    {
        private static readonly WebClient Client = new WebClient();

        public static void CheckForUpdates(long id, Action noUpdates = null, Action<Exception> errorCallback = null)
        {
            try
            {
                var url = Client.DownloadString("http://mygod.tk/product/update/" + id + '/');
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
