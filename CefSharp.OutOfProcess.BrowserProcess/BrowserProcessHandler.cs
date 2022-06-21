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
    public class BrowserProcessHandler : CefSharp.Handler.BrowserProcessHandler, IBrowserProcessServer
    {
        private readonly int _parentProcessId;
        private IList<OutOfProcessChromiumWebBrowser> _browsers = new List<OutOfProcessChromiumWebBrowser>();
        /// <summary>
        /// JSON RPC used for IPC with host
        /// </summary>
        private JsonRpc _jsonRpc;
        private IOutOfProcessServer _outOfProcessServer;

        public BrowserProcessHandler(int parentProcessId)
        {
            _parentProcessId = parentProcessId;
        }

        protected override void OnContextInitialized()
        {
            base.OnContextInitialized();

            _jsonRpc = JsonRpc.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput());
            _outOfProcessServer = _jsonRpc.Attach<IOutOfProcessServer>();
            _jsonRpc.AllowModificationWhileListening = true;
            _jsonRpc.AddLocalRpcTarget<IBrowserProcessServer>(this, null);
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

        Task IBrowserProcessServer.CloseBrowser(int browserId)
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                var p = _browsers.FirstOrDefault(x => x.Id == browserId);

                _browsers.Remove(p);

                p.Dispose();

                return true;
            });
        }

        Task IBrowserProcessServer.SendDevToolsMessage(int browserId, string message)
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                var browser = _browsers.FirstOrDefault(x => x.Id == browserId);

                browser?.GetBrowserHost().SendDevToolsMessage(message);

                return true;
            });
        }

        Task IBrowserProcessServer.CloseHost()
        {
            return CefThread.ExecuteOnUiThread(() =>
            {
                Cef.QuitMessageLoop();

                return true;
            });
        }

        Task IBrowserProcessServer.CreateBrowser(IntPtr parentHwnd, string url, int id)
        {
            //Debugger.Break();

            return CefThread.ExecuteOnUiThread(() =>
            {
                var browser = new OutOfProcessChromiumWebBrowser(_outOfProcessServer, id, url);

                var windowInfo = new WindowInfo();
                windowInfo.WindowName = "CefSharpBrowserProcess";
                windowInfo.SetAsChild(parentHwnd);

                browser.CreateBrowser(windowInfo);

                _browsers.Add(browser);

                return true;
            });
        }
    }
}
