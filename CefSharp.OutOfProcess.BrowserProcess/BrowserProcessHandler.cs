using CefSharp.Internals;
using PInvoke;
using StreamJsonRpc;
using System;
using System.Linq;
using System.Collections.Generic;
using CefSharp.OutOfProcess.Interface;
using System.Threading.Tasks;
using CefSharp.Wpf.Internals;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    public class BrowserProcessHandler : CefSharp.Handler.BrowserProcessHandler, IOutOfProcessClientRpc
    {
        private readonly int _parentProcessId;
        private readonly bool _offscreenRendering;
        private IList<OutOfProcessChromiumWebBrowser> _browsers = new List<OutOfProcessChromiumWebBrowser>();
        /// <summary>
        /// JSON RPC used for IPC with host
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
                OutOfProcessChromiumWebBrowser browser;
                IWindowInfo windowInfo;
                if (_offscreenRendering)
                {
                    browser = new OffscreenOutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url, null);
                    windowInfo = Core.ObjectFactory.CreateWindowInfo();
                    windowInfo.SetAsWindowless(parentHwnd); // parentHwnd IntPtr.Zero
                    windowInfo.Width = 0;
                    windowInfo.Height = 0;
                }
                else
                {
                    browser = new OutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url, null);
                    windowInfo = new WindowInfo();
                    windowInfo.WindowName = "CefSharpBrowserProcess";
                    windowInfo.SetAsChild(parentHwnd);

                    //Disable Window activation by default
                    //https://bitbucket.org/chromiumembedded/cef/issues/1856/branch-2526-cef-activates-browser-window
                    windowInfo.ExStyle |= OutOfProcessChromiumWebBrowser.WS_EX_NOACTIVATE;
                }

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

                offscreenBrowser.viewRect = new CefSharp.Structs.Rect(0, 0, width, height);

                host.WasResized();
            }
        }

        void IOutOfProcessClientRpc.SetFocus(int browserId, bool focus)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            browser?.GetBrowserHost().SetFocus(focus);
        }
    }
}
