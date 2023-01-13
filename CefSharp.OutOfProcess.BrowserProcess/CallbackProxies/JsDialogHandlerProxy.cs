using CefSharp.OutOfProcess.Interface;
using CefSharp.OutOfProcess.Interface.Callbacks;

namespace CefSharp.OutOfProcess.BrowserProcess.CallbackProxies
{
    internal sealed class JsDialogHandlerProxy : CallbackProxyBase<IJsDialogCallback>, IJsDialogHandler
    {
        public JsDialogHandlerProxy(IOutOfProcessHostRpc host)
            : base(host)
        {

        }

        public void Callback(JsDialogCallbackDetails details)
        {
            var cb = GetCallback(details.CallbackId);

            cb.Continue(details.Success, details.UserInput);
        }

        bool IJsDialogHandler.OnBeforeUnloadDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string messageText, bool isReload, IJsDialogCallback callback)
        {
            var result = host.OnBeforeUnloadDialog(((OutOfProcessChromiumWebBrowser)chromiumWebBrowser).Id, messageText, isReload, CreateCallback(callback));
            return result.Result;
        }

        void IJsDialogHandler.OnDialogClosed(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            host.OnDialogClosed(((OutOfProcessChromiumWebBrowser)chromiumWebBrowser).Id);
        }

        bool IJsDialogHandler.OnJSDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, CefJsDialogType dialogType, string messageText, string defaultPromptText, IJsDialogCallback callback, ref bool suppressMessage)
        {
            var result = host.OnJSDialog(((OutOfProcessChromiumWebBrowser)chromiumWebBrowser).Id, originUrl, dialogType.ToString(), messageText, defaultPromptText, CreateCallback(callback), suppressMessage);
            return result.Result;
        }

        void IJsDialogHandler.OnResetDialogState(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            host.OnResetDialogState(((OutOfProcessChromiumWebBrowser)chromiumWebBrowser).Id);
        }
    }
}
