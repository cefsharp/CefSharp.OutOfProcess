using System;
using System.Diagnostics;
using System.IO;
using CefSharp.Internals;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Cef.EnableHighDPISupport();

            //Debugger.Launch();

            var parentProcessId = int.Parse(CommandLineArgsParser.GetArgumentValue(args, "--parentProcessId"));
            var hostHwnd = int.Parse(CommandLineArgsParser.GetArgumentValue(args, "--hostHwnd"));

            var parentProcess = Process.GetProcessById(parentProcessId);

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\OutOfProcessCache")
            };

            Cef.EnableWaitForBrowsersToClose();

            Cef.Initialize(settings);

            var browser = new ChromiumWebBrowser("https://github.com");

            var windowInfo = new WindowInfo();
            windowInfo.WindowName = "CefSharpBrowserProcess";
            windowInfo.SetAsChild(new IntPtr(hostHwnd));

            browser.CreateBrowser(windowInfo);

            parentProcess.WaitForExit();

            Cef.WaitForBrowsersToClose();

            Cef.Shutdown();

            return 0;
        }
    }
}
