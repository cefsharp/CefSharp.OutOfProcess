namespace CefSharp.OutOfProcess.BrowserProcess
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using CefSharp.Internals;
    using CefSharp.OutOfProcess.BrowserProcess.CallbackProxies;
    using CefSharp.OutOfProcess.Interface;
    using CefSharp.OutOfProcess.Interface.Callbacks;
    using PInvoke;
    using StreamJsonRpc;

    public class BrowserProcessHandler : Handler.BrowserProcessHandler, IOutOfProcessClientRpc
    {
        private readonly int _parentProcessId;
        private readonly bool _offscreenRendering;
        private IList<OutOfProcessChromiumWebBrowser> _browsers = new List<OutOfProcessChromiumWebBrowser>();

        /// <summary>
        /// JSON RPC used for IPC with host.
        /// </summary>
        private JsonRpc _jsonRpc;
        private IOutOfProcessHostRpc _outOfProcessServer;

        public BrowserProcessHandler(int parentProcessId, bool offscreenRendering)
        {
            _parentProcessId = parentProcessId;
            _offscreenRendering = offscreenRendering;
        }

        protected override void OnContextInitialized()
        {
            base.OnContextInitialized();

            _jsonRpc = JsonRpc.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput());
            _jsonRpc.AllowModificationWhileListening = true;
            _outOfProcessServer = _jsonRpc.Attach<IOutOfProcessHostRpc>();
            _jsonRpc.AddLocalRpcTarget<IOutOfProcessClientRpc>(this, null);
            _jsonRpc.AllowModificationWhileListening = false;

            var threadId = Kernel32.GetCurrentThreadId();

            _outOfProcessServer.FileDialogCallback += _outOfProcessServer_FileDialogCallback;
            _outOfProcessServer.JsDialogCallback += _outOfProcessServer_JsDialogCallback;
            _outOfProcessServer.BeforeDownloadCallback += _outOfProcessServer_BeforeDownloadCallback;
            _outOfProcessServer.DownloadCallback += _outOfProcessServer_DownloadCallback;
            _outOfProcessServer.NotifyContextInitialized(threadId, Cef.CefSharpVersion, Cef.CefVersion, Cef.ChromiumVersion);
        }

        private void _outOfProcessServer_DownloadCallback(object sender, DownloadCallbackDetails e)
        {
            ((DownloadHandlerProxy)GetBrowser(e.BrowserId).DownloadHandler)?.DownloadCallback(e);
        }

        private void _outOfProcessServer_BeforeDownloadCallback(object sender, BeforeDownloadCallbackDetails e)
        {
            ((DownloadHandlerProxy)GetBrowser(e.BrowserId).DownloadHandler)?.BeforeDownloadCallback(e);
        }

        private void _outOfProcessServer_JsDialogCallback(object sender, JsDialogCallbackDetails e)
        {
            ((JsDialogHandlerProxy)GetBrowser(e.BrowserId).JsDialogHandler).Callback(e);
        }

        private OutOfProcessChromiumWebBrowser GetBrowser(int id) => _browsers.FirstOrDefault(x => x.Id == id);

        private void _outOfProcessServer_FileDialogCallback(object sender, FileDialogCallbackDetails e)
        {
            CefThread.ExecuteOnUiThread(() =>
            {
                ((DialogHandlerProxy)GetBrowser(e.BrowserId).DialogHandler).Callback(e);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _jsonRpc?.Dispose();
                _jsonRpc = null;
            }
        }

        Task IOutOfProcessClientRpc.CloseBrowser(int browserId)
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                var p = _browsers.FirstOrDefault(x => x.Id == browserId);

                _browsers.Remove(p);

                p.Dispose();

                return true;
            });
        }

        Task IOutOfProcessClientRpc.SendDevToolsMessage(int browserId, string message)
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                GetBrowser(browserId)?.GetBrowserHost().SendDevToolsMessage(message);
            });
        }

        Task IOutOfProcessClientRpc.ShowDevTools(int browserId)
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                GetBrowser(browserId).ShowDevTools();
            });
        }

        Task IOutOfProcessClientRpc.LoadUrl(int browserId, string url)
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                GetBrowser(browserId).LoadUrl(url);
            });
        }

        Task IOutOfProcessClientRpc.CloseHost()
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                Cef.QuitMessageLoop();

                return true;
            });
        }

        Task IOutOfProcessClientRpc.CreateBrowser(IntPtr parentHwnd, string url, int id, IDictionary<string, object> requestContextPreferences)
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                OutOfProcessChromiumWebBrowser browser;

                IRequestContext requestContext = null;
                if (requestContextPreferences != null)
                {
                    requestContext = new RequestContext();
                    foreach (KeyValuePair<string, object> pref in requestContextPreferences)
                    {
                        requestContext.SetPreference(pref.Key, pref.Value, out _);
                    }
                }

                IWindowInfo windowInfo;

                if (_offscreenRendering)
                {
                    browser = new OffscreenOutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url, requestContext);
                    windowInfo = Core.ObjectFactory.CreateWindowInfo();
                    windowInfo.SetAsWindowless(parentHwnd); // parentHwnd IntPtr.Zero
                    windowInfo.Width = 0;
                    windowInfo.Height = 0;
                }
                else
                {
                    browser = new OutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url, requestContext);
                    windowInfo = new WindowInfo();
                    windowInfo.WindowName = "CefSharpBrowserProcess";
                    windowInfo.SetAsChild(parentHwnd);

                    //Disable Window activation by default
                    //https://bitbucket.org/chromiumembedded/cef/issues/1856/branch-2526-cef-activates-browser-window
                    windowInfo.ExStyle |= OutOfProcessChromiumWebBrowser.WS_EX_NOACTIVATE;
                }

                browser.DialogHandler = new DialogHandlerProxy(_outOfProcessServer);
                browser.JsDialogHandler = new JsDialogHandlerProxy(_outOfProcessServer);
                browser.DownloadHandler = new DownloadHandlerProxy(_outOfProcessServer);
                //// TODO: (CEF)  implment all required handlers
                //// js, contextmenu, dialog, download ...

                browser.CreateBrowser(windowInfo);
                _browsers.Add(browser);

                return true;
            });
        }

        void IOutOfProcessClientRpc.NotifyMoveOrResizeStarted(int browserId, int width, int height, int screenX, int screenY)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            browser?.GetBrowserHost().NotifyMoveOrResizeStarted();

            if (_offscreenRendering && browser is OffscreenOutOfProcessChromiumWebBrowser offscreenBrowser)
            {
                offscreenBrowser.browserLocation = new System.Drawing.Point(screenX, screenY);
                var host = browser?.GetBrowserHost();
                host.NotifyMoveOrResizeStarted();

                offscreenBrowser.viewRect = new Structs.Rect(0, 0, width, height);

                host.WasResized();
            }
        }

        void IOutOfProcessClientRpc.SetFocus(int browserId, bool focus)
        {
            GetBrowser(browserId)?.GetBrowserHost().SetFocus(focus);
        }

        /// <summary>
        /// <param name="browserId">The browser id.</param>
        /// <param name="preferences">The preferences.</param>
        /// </summary>
        void IOutOfProcessClientRpc.UpdateRequestContextPreferences(int browserId, IDictionary<string, object> preferences)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            CefThread.ExecuteOnUiThread(() =>
            {
                IRequestContext requestContext = browser.GetRequestContext();
                foreach (KeyValuePair<string, object> pref in preferences)
                {
                    requestContext?.SetPreference(pref.Key, pref.Value, out _);
                }
            });
        }

        /// <summary>
        /// <param name="browserId">The browser id.</param>
        /// <param name="preferences">The preferences.</param>
        /// </summary>
        void IOutOfProcessClientRpc.UpdateGlobalRequestContextPreferences(IDictionary<string, object> preferences)
        {
            CefThread.ExecuteOnUiThread(() =>
            {
                IRequestContext requestContext = Cef.GetGlobalRequestContext();
                foreach (KeyValuePair<string, object> pref in preferences)
                {
                    requestContext?.SetPreference(pref.Key, pref.Value, out _);
                }
            });
        }
    }
}