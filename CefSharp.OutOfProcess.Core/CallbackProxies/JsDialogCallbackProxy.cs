namespace CefSharp.OutOfProcess
{
    using CefSharp.OutOfProcess.Interface.Callbacks;
    using CefSharp.OutOfProcess.Internal;

    internal sealed class JsDialogCallbackProxy : CallbackProxyBase, IJsDialogCallback
    {
        public JsDialogCallbackProxy(OutOfProcessHost outOfProcessHost, int callback, IChromiumWebBrowserInternal chromiumWebBrowser)
            : base(outOfProcessHost, callback, chromiumWebBrowser)
        {
        }

        void IJsDialogCallback.Continue(bool success, string userInput)
        {
            outOfProcessHost.InvokeJsDialogCallback(new JsDialogCallbackDetails()
            {
                CallbackId = callback,
                BrowserId = chromiumWebBrowser.Id,
                Success = success,
                UserInput = userInput,
            });
        }

        void IJsDialogCallback.Continue(bool success)
        {
            outOfProcessHost.InvokeJsDialogCallback(new JsDialogCallbackDetails()
            {
                CallbackId = callback,
                BrowserId = chromiumWebBrowser.Id,
                Success = success,
            });
        }
    }
}
