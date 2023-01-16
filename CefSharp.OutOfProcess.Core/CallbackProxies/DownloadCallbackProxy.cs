namespace CefSharp.OutOfProcess
{
    using CefSharp.OutOfProcess.Interface.Callbacks;
    using CefSharp.OutOfProcess.Internal;

    internal sealed class DownloadCallbackProxy : CallbackProxyBase, IBeforeDownloadCallback, IDownloadItemCallback
    {
        public DownloadCallbackProxy(OutOfProcessHost outOfProcessHost, int callback, IChromiumWebBrowserInternal chromiumWebBrowser)
            : base(outOfProcessHost, callback, chromiumWebBrowser)
        {
        }

        void IDownloadItemCallback.Cancel()
        {
            outOfProcessHost.InvokeDownloadCallback(new DownloadCallbackDetails()
            {
                CallbackId = callback,
                BrowserId = chromiumWebBrowser.Id,
                Cancel = true,
            });
        }

        void IBeforeDownloadCallback.Continue(string downloadPath, bool showDialog)
        {
            outOfProcessHost.InvokeBeforeDownloadCallback(new BeforeDownloadCallbackDetails()
            {
                CallbackId = callback,
                BrowserId = chromiumWebBrowser.Id,
                DownloadPath = downloadPath,
                ShowDialog = showDialog,
            });
        }

        void IDownloadItemCallback.Pause()
        {
            outOfProcessHost.InvokeDownloadCallback(new DownloadCallbackDetails()
            {
                CallbackId = callback,
                BrowserId = chromiumWebBrowser.Id,
                Pause = true,
            });
        }

        void IDownloadItemCallback.Resume()
        {
            outOfProcessHost.InvokeDownloadCallback(new DownloadCallbackDetails()
            {
                CallbackId = callback,
                BrowserId = chromiumWebBrowser.Id,
                Resume = true,
            });
        }
    }
}
