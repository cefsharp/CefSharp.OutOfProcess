using CefSharp.Internals;
using PInvoke;
using StreamJsonRpc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CefSharp.OutOfProcess.Model;
using System.Diagnostics;

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

        Task IOutOfProcessClientRpc.CreateBrowser(IntPtr parentHwnd, string url, int id)
        {
            //Debugger.Break();

            return CefThread.ExecuteOnUiThread(() =>
            {
                var browser = new OutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url);

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

        /// <inheritdoc />
        Task<SetPreferenceResponse> IOutOfProcessClientRpc.SetRequestContextPreferenceAsync(int browserId, string name, object value)
        {
            //Debugger.Break();

            if (CefThread.HasShutdown)
            {
                return Task.FromResult(new SetPreferenceResponse(false, "Cef.Shutdown has already been called, it is no longer possible to call SetPreferenceAsync."));
            }

            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            if (browser == null)
            {
                return Task.FromResult(new SetPreferenceResponse(false, $"Browser with Id {browserId} was null."));
            }

            return CefThread.ExecuteOnUiThread(() =>
            {
                var ctx = browser?.GetRequestContext();

                if (ctx == null)
                {
                    return new SetPreferenceResponse(false, "RequestContext was null.");
                }

                // StreamJsonRpc is converting ints to long which makes sense as we accept
                // object. CefValue doesn't support Int64, so we just convert to int
                // This should hopefully be fine as it's unliklely any preference requires
                // an Int64 value.
                if (value?.GetType() == typeof(long))
                {
                    value = Convert.ToInt32(value);
                }

                var success = ctx.SetPreference(name, value, out string error);

                return new SetPreferenceResponse(success, error);
            });
        }
    }
}
