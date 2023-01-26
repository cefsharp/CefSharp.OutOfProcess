using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            Debugger.Launch();

            var parentProcessId = int.Parse(CommandLineArgsParser.GetArgumentValue(args, "--parentProcessId"));

            var parentProcess = Process.GetProcessById(parentProcessId);


            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                MultiThreadedMessageLoop = false
            };

            foreach (var arg in args)
            {
                List<string> splitted = arg.Split('=').ToList();
                var key = splitted.First().Substring(2);
                var value = splitted.Count == 1 ? string.Empty : splitted.Last();
                AddArg(settings, key, value);
            }

            var browserProcessHandler = new BrowserProcessHandler(parentProcessId);

            Cef.EnableWaitForBrowsersToClose();

            var success = Cef.Initialize(settings, performDependencyCheck:true, browserProcessHandler: browserProcessHandler);

            if(!success)
            {
                return 1;
            }

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

        private static void AddArg(CefSettingsBase settings, string key, string value)
        {
            switch (key)
            {
                case "accept-lang":
                    settings.AcceptLanguageList = value;
                    break;
                case "cachePath":
                    settings.CachePath = value;
                    break;
                default:
                    settings.CefCommandLineArgs.Add(key, value);
                    break;
            }
        }
    }
}
