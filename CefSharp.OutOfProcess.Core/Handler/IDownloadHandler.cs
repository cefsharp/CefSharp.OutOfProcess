namespace CefSharp.OutOfProcess.Handler
{
    using CefSharp.OutOfProcess.Interface.Callbacks;

    public interface IDownloadHandler
    {
        bool CanDownload(IChromiumWebBrowser chromiumWebBrowser, string url, string requestMethod);

        void OnBeforeDownload(IChromiumWebBrowser chromiumWebBrowser, CefSharp.OutOfProcess.Interface.Callbacks.DownloadItem downloadItem, IBeforeDownloadCallback callback);

        void OnDownloadUpdated(IChromiumWebBrowser chromiumWebBrowser, CefSharp.OutOfProcess.Interface.Callbacks.DownloadItem downloadItem, IDownloadItemCallback callback);
    }
}