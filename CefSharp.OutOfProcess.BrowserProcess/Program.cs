using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CefSharp.Internals;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    public class Program
    {
        private static bool _disposed;

        [STAThread]
        public static int Main(string[] args)
        {
            Cef.EnableHighDPISupport();

            //Debugger.Launch();

            var parentProcessId = int.Parse(CommandLineArgsParser.GetArgumentValue(args, "--parentProcessId"));

            var parentProcess = Process.GetProcessById(parentProcessId);

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\OutOfProcessCache"),
                MultiThreadedMessageLoop = false
            };

            var browserProcessHandler = new BrowserProcessHandler(parentProcessId);

            Cef.EnableWaitForBrowsersToClose();

            Cef.Initialize(settings, performDependencyCheck:true, browserProcessHandler: browserProcessHandler);

            _ = Task.Run(() =>
            {
                parentProcess.WaitForExit();

                if(_disposed)
                {
                    return;
                }

                _ = CefThread.ExecuteOnUiThread(() =>
                {
                    Cef.QuitMessageLoop();

                    return true;
                });
            });

            Cef.RunMessageLoop();

            _disposed = true;

            Cef.WaitForBrowsersToClose();

            Cef.Shutdown();

            return 0;
        }
    }
}
