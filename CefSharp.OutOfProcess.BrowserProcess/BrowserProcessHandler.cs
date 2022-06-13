using CefSharp.Internals;
using PInvoke;
using StreamJsonRpc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    public class BrowserProcessHandler : CefSharp.Handler.BrowserProcessHandler
    {
        private readonly int _parentProcessId;
        private IList<OutOfProcessChromiumWebBrowser> _browsers = new List<OutOfProcessChromiumWebBrowser>();
        /// <summary>
        /// JSON RPC used for IPC with host
        /// </summary>
        private JsonRpc _jsonRpc;

        public BrowserProcessHandler(int parentProcessId)
        {
            _parentProcessId = parentProcessId;
        }

        protected override void OnContextInitialized()
        {
            base.OnContextInitialized();

            _jsonRpc = JsonRpc.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput());
            _jsonRpc.AllowModificationWhileListening = true;

            _jsonRpc.AddLocalRpcMethod("CloseBrowser", (Action<int>)delegate (int browserId)
            {
                _ = CefThread.ExecuteOnUiThread(() =>
                {
                    var p = _browsers.FirstOrDefault(x => x.Id == browserId);

                    _browsers.Remove(p);

                    p.Dispose();

                    return true;
                });
            });

            _jsonRpc.AddLocalRpcMethod("CloseHost", (Action)delegate ()
            {
                _ = CefThread.ExecuteOnUiThread(() =>
                {
                    Cef.QuitMessageLoop();

                    return true;
                });
            });

            _jsonRpc.AddLocalRpcMethod("CreateBrowser", (Action<int, string, int>)delegate (int parentHwnd, string url, int id)
            {
                //Debugger.Break();

                _ = CefThread.ExecuteOnUiThread(() =>
                {

                    var browser = new OutOfProcessChromiumWebBrowser(_jsonRpc, id, url);

                    var windowInfo = new WindowInfo();
                    windowInfo.WindowName = "CefSharpBrowserProcess";
                    windowInfo.SetAsChild(new IntPtr(parentHwnd));

                    browser.CreateBrowser(windowInfo);

                    _browsers.Add(browser);

                    return true;
                });
            });

            var threadId = Kernel32.GetCurrentThreadId();

            _ = _jsonRpc.NotifyAsync("OnContextInitialized", threadId, Cef.CefSharpVersion, Cef.CefVersion, Cef.ChromiumVersion);
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
    }
}
