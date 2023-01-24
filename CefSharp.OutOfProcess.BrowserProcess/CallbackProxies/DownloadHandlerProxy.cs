namespace CefSharp.OutOfProcess.BrowserProcess.CallbackProxies
{
    using System.Diagnostics;
    using CefSharp;
    using CefSharp.OutOfProcess.Interface;
    using CefSharp.OutOfProcess.Interface.Callbacks;

    internal class DownloadHandlerProxy : CallbackProxyBase<object>, IDownloadHandler
    {
        public DownloadHandlerProxy(IOutOfProcessHostRpc host)
            : base(host)
        {            
        }

        public void BeforeDownloadCallback(BeforeDownloadCallbackDetails details)
        {
            ((CefSharp.OutOfProcess.Interface.Callbacks.IBeforeDownloadCallback)GetCallback(details.CallbackId)).Continue(details.DownloadPath, details.ShowDialog);
        }

        public void DownloadCallback(DownloadCallbackDetails details)
        {
            var cb = (CefSharp.OutOfProcess.Interface.Callbacks.IDownloadItemCallback)GetCallback(details.CallbackId);
            if (details.Cancel)
            {
                cb.Cancel();
            }
            else if (details.Pause)
            {
                cb.Pause();
            }
            else if (details.Resume)
            {
                cb.Resume();
            }
        }

        bool IDownloadHandler.CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return host.OnCanDownloadAsync(((OutOfProcessChromiumWebBrowser)chromiumWebBrowser).Id, url, requestMethod).Result;
        }

        void IDownloadHandler.OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, CefSharp.DownloadItem downloadItem, CefSharp.IBeforeDownloadCallback callback)
        {
            host.OnBeforeDownload(((OutOfProcessChromiumWebBrowser)chromiumWebBrowser).Id, Convert(downloadItem), CreateCallback(callback));
        }

        void IDownloadHandler.OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefSharp.DownloadItem downloadItem, CefSharp.IDownloadItemCallback callback)
        {
            host.OnDownloadUpdated(((OutOfProcessChromiumWebBrowser)chromiumWebBrowser).Id, Convert(downloadItem), CreateCallback(callback));
        }

        private static CefSharp.OutOfProcess.Interface.Callbacks.DownloadItem Convert(CefSharp.DownloadItem item) => new CefSharp.OutOfProcess.Interface.Callbacks.DownloadItem()
        {
            SuggestedFileName = item.SuggestedFileName,
            CurrentSpeed = item.CurrentSpeed,
            Id = item.Id,
            ContentDisposition = item.ContentDisposition,
            EndTime = item.EndTime,
            FullPath = item.FullPath,
            IsCancelled = item.IsCancelled,
            IsComplete = item.IsComplete,
            IsInProgress = item.IsInProgress,
            IsValid = item.IsValid,
            MimeType = item.MimeType,
            OriginalUrl = item.OriginalUrl,
            PercentComplete = item.PercentComplete,
            ReceivedBytes = item.ReceivedBytes,
            StartTime = item.StartTime,
            TotalBytes = item.TotalBytes,
            Url = item.Url,
        };
    }
}