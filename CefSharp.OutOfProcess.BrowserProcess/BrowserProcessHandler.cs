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
            Debugger.Break();

            return CefThread.ExecuteOnUiThread(() =>
            {
                var browser = new OutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url);

                var windowInfo = Core.ObjectFactory.CreateWindowInfo();
                windowInfo.SetAsWindowless(parentHwnd); // parentHwnd IntPtr.Zero
                windowInfo.Width = 0;
                windowInfo.Height = 0;
                browser.CreateBrowser(windowInfo);

                _browsers.Add(browser);

                return true;
            });
        }

        void IOutOfProcessClientRpc.SetFocus(int browserId, bool focus)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            browser?.GetBrowserHost().SetFocus(focus);
        }

        void IOutOfProcessClientRpc.SendCaptureLostEvent(int browserId)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            browser?.GetBrowserHost().SendCaptureLostEvent();
        }

        void IOutOfProcessClientRpc.SendMouseClickEvent(int browserId, int X, int Y, Copy.CefSharp.MouseButtonType changedButton, bool mouseUp, int clickCount, Copy.CefSharp.CefEventFlags modifiers)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            browser?.GetBrowserHost().SendMouseClickEvent(X, Y, (MouseButtonType)changedButton, mouseUp, clickCount, (CefEventFlags)modifiers);
        }

        void IOutOfProcessClientRpc.SendMouseMoveEvent(int browserId, int X, int Y, bool mouseLeave, Copy.CefSharp.CefEventFlags modifiers)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            browser?.GetBrowserHost().SendMouseMoveEvent(X, Y, mouseLeave, (CefEventFlags)modifiers);
        }

        void IOutOfProcessClientRpc.NotifyMoveOrResizeStarted(int browserId, int width, int height, int screenX, int screenY)
        {
            var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

            var host = browser?.GetBrowserHost();

            if (browser != null)
            {
                browser.browserLocation = new System.Drawing.Point(screenX, screenY);
                browser?.GetBrowserHost().NotifyMoveOrResizeStarted();

                browser.viewRect = new Structs.Rect(0, 0, width, height);

                host.WasResized();
            }
        }
    }
}
