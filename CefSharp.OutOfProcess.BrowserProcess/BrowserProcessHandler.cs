using CefSharp.Internals;
using PInvoke;
using StreamJsonRpc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using CefSharp.OutOfProcess.Interface;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    public class BrowserProcessHandler : CefSharp.Handler.BrowserProcessHandler, IOutOfProcessClientRpc
    {
        private readonly int _parentProcessId;
        private IList<OutOfProcessChromiumWebBrowser> _browsers = new List<OutOfProcessChromiumWebBrowser>();
        /// <summary>
        /// JSON RPC used for IPC with host
        /// </summary>
        private JsonRpc _jsonRpc;
        private IOutOfProcessHostRpc _outOfProcessServer;

        public BrowserProcessHandler(int parentProcessId)
        {
            _parentProcessId = parentProcessId;
        }

        protected override void OnContextInitialized()
        {
            base.OnContextInitialized();

            _jsonRpc = JsonRpc.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput());
            _outOfProcessServer = _jsonRpc.Attach<IOutOfProcessHostRpc>();
            _jsonRpc.AllowModificationWhileListening = true;
            _jsonRpc.AddLocalRpcTarget<IOutOfProcessClientRpc>(this, null);
            _jsonRpc.AllowModificationWhileListening = false;

            var threadId = Kernel32.GetCurrentThreadId();

            _outOfProcessServer.NotifyContextInitialized(threadId, Cef.CefSharpVersion, Cef.CefVersion, Cef.ChromiumVersion);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(disposing)
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
                var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

                browser?.GetBrowserHost().SendDevToolsMessage(message);

                return true;
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
            //Debugger.Break();

            return CefThread.ExecuteOnUiThread(() =>
            {
                IRequestContext requestContext = null;
                if (requestContextPreferences != null)
                {
                    requestContext = new RequestContext(Cef.GetGlobalRequestContext());
                    foreach (KeyValuePair<string, object> pref in requestContextPreferences)
                    {
                        requestContext.SetPreference(pref.Key, pref.Value, out _);
                    }
                }

                var browser = new OutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url, requestContext);

                var windowInfo = new WindowInfo();
                windowInfo.WindowName = "CefSharpBrowserProcess";
                windowInfo.SetAsChild(parentHwnd);

                //Disable Window activation by default
                //https://bitbucket.org/chromiumembedded/cef/issues/1856/branch-2526-cef-activates-browser-window
                windowInfo.ExStyle |= OutOfProcessChromiumWebBrowser.WS_EX_NOACTIVATE;

                browser.CreateBrowser(windowInfo);

                _browsers.Add(browser);

                return true;
            });
        }

        void IOutOfProcessClientRpc.NotifyMoveOrResizeStarted(int browserId)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            browser?.GetBrowserHost().NotifyMoveOrResizeStarted();
        }

        void IOutOfProcessClientRpc.SetFocus(int browserId, bool focus)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            browser?.GetBrowserHost().SetFocus(focus);
        }

        /// <summary>
        /// Set Request Context Preferences of the browser.
        /// </summary>
        /// <param name="browserId">The browser id.</param>
        /// <param name="preferences">The preferences.</param>
        void IOutOfProcessClientRpc.SetRequestContextPreferences(int browserId, IDictionary<string, object> preferences)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            if (browser?.GetRequestContext() is IRequestContext requestContext)
            {
                SetRequestContextPreferences(requestContext, preferences);
            }
        }

        /// <summary>
        /// Set Global Request Context Preferences for all browsers.
        /// </summary>
        /// <param name="preferences">The preferences.</param>
        void IOutOfProcessClientRpc.SetGlobalRequestContextPreferences(IDictionary<string, object> preferences)
        {
            if (Cef.GetGlobalRequestContext() is IRequestContext requestContext)
            {
                SetRequestContextPreferences(requestContext, preferences);
            }
        }

        void SetRequestContextPreferences(IRequestContext requestContext, IDictionary<string, object> preferences)
        {
            _ = CefThread.ExecuteOnUiThread(() =>
            {
                foreach (KeyValuePair<string, object> pref in preferences)
                {
                    requestContext.SetPreference(pref.Key, pref.Value, out _);
                }

                return true;
            });
        }
    }
}
