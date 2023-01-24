using System.Collections.Generic;
using CefSharp.OutOfProcess.Interface;
using CefSharp.Handler;
using System.Linq;
using CefSharp.OutOfProcess.Interface.Callbacks;

namespace CefSharp.OutOfProcess.BrowserProcess.CallbackProxies
{

    internal sealed class DialogHandlerProxy : CallbackProxyBase<IFileDialogCallback>, IDialogHandler
    {
        public DialogHandlerProxy(IOutOfProcessHostRpc host)
            : base(host)
        {

        }

        public void Callback(FileDialogCallbackDetails details)
        {
            var cb = GetCallback(details.CallbackId);

            if (details.Continue)
            {
                cb.Continue(details.Files.ToList());
            }
            else
            {
                cb.Cancel();
            }
        }

        bool IDialogHandler.OnFileDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, CefFileDialogMode mode, string title, string defaultFilePath, List<string> acceptFilters, IFileDialogCallback callback)
        {
            var result = host.OnFileDialog(((OutOfProcessChromiumWebBrowser)chromiumWebBrowser).Id, mode.ToString(), title, defaultFilePath, acceptFilters.ToArray(), CreateCallback(callback));
            return result.Result;
        }
    }
}
