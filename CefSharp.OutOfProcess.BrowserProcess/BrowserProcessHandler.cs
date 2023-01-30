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
        private readonly bool _offscreenRendering;
        private readonly IList<OutOfProcessChromiumWebBrowser> _browsers = new List<OutOfProcessChromiumWebBrowser>();
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
            _jsonRpc.AllowModificationWhileListening = true;
            _outOfProcessServer = _jsonRpc.Attach<IOutOfProcessHostRpc>();
            _jsonRpc.AddLocalRpcTarget<IOutOfProcessClientRpc>(this, null);

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
                GetBrowser(browserId)?.GetBrowserHost().SendDevToolsMessage(message);
                return true;
            });
        }

        Task IOutOfProcessClientRpc.ShowDevTools(int browserId)
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                GetBrowser(browserId).ShowDevTools();
                return true;
            });
        }

        Task IOutOfProcessClientRpc.LoadUrl(int browserId, string url)
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                GetBrowser(browserId).LoadUrl(url);
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
                    browser = new OffscreenOutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url);
                    windowInfo = Core.ObjectFactory.CreateWindowInfo();
                    windowInfo.SetAsWindowless(parentHwnd);
                    windowInfo.Width = 0;
                    windowInfo.Height = 0;
                }
                else
                {
                    browser = new OutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url);
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

        void IOutOfProcessClientRpc.NotifyMoveOrResizeStarted(int browserId, Rect rect)
        {
            var browser = GetBrowser(browserId);
            if(browser == null)
            {
                return;
            }

            var host = browser.GetBrowserHost();
            host.NotifyMoveOrResizeStarted();

            if (_offscreenRendering && browser is OffscreenOutOfProcessChromiumWebBrowser offscreenBrowser)
            {
                host.NotifyMoveOrResizeStarted();
                offscreenBrowser.ViewRect = new Structs.Rect(rect.X, rect.Y, rect.Width, rect.Height);
                host.WasResized();
            }
        }

        void IOutOfProcessClientRpc.SetFocus(int browserId, bool focus)
        {
            GetBrowser(browserId)?.GetBrowserHost().SetFocus(focus);
        }

        void IOutOfProcessClientRpc.SendMouseClickEvent(int browserId, int x, int y, string mouseButtonType, bool mouseUp, int clickCount, uint eventFlags)
        {
            CefThread.ExecuteOnUiThread(() =>
            {
                GetBrowser(browserId)?.GetBrowserHost().SendMouseClickEvent(x, y, (MouseButtonType)Enum.Parse(typeof(MouseButtonType), mouseButtonType), mouseUp, clickCount, (CefEventFlags)eventFlags);
                return true;
            });
        }

        private OutOfProcessChromiumWebBrowser GetBrowser(int id) => _browsers.FirstOrDefault(x => x.Id == id);
    }
}