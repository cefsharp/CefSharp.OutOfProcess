namespace CefSharp.OutOfProcess
{
    using System.Collections.Generic;
    using CefSharp.OutOfProcess.Interface.Callbacks;
    using CefSharp.OutOfProcess.Internal;

    internal sealed class FileDialogCallbackProxy : CallbackProxyBase, IFileDialogCallback
    {
        public FileDialogCallbackProxy(OutOfProcessHost outOfProcessHost, int callback, IChromiumWebBrowserInternal chromiumWebBrowser)
            : base(outOfProcessHost, callback, chromiumWebBrowser)
        {
        }

        public void Cancel()
        {
            outOfProcessHost.InvokeFileDialogCallback(new FileDialogCallbackDetails()
            {
                CallbackId = callback,
                BrowserId = chromiumWebBrowser.Id,
                Continue = false,
            });
        }

        public void Continue(List<string> filePaths)
        {
            outOfProcessHost.InvokeFileDialogCallback(new FileDialogCallbackDetails()
            {
                CallbackId = callback,
                BrowserId = chromiumWebBrowser.Id,
                Continue = true,
                Files = filePaths.ToArray(),
            });
        }
    }
}
