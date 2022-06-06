using System;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    public class BrowserProcessHandler : CefSharp.Handler.BrowserProcessHandler
    {
        private readonly int _parentProcessId;
        private readonly IntPtr _hostHwnd;

        public BrowserProcessHandler(int parentProcessId, IntPtr hostHwnd)
        {
            _parentProcessId = parentProcessId;
            _hostHwnd = hostHwnd;
        }

        protected override void OnContextInitialized()
        {
            base.OnContextInitialized();

            var browser = new OutOfProcessChromiumWebBrowser("https://github.com");

            var windowInfo = new WindowInfo();
            windowInfo.WindowName = "CefSharpBrowserProcess";
            windowInfo.SetAsChild(_hostHwnd);

            browser.CreateBrowser(windowInfo);
        }
    }
}
