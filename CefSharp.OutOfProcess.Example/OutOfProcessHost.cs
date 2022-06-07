using PInvoke;
using StreamJsonRpc;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess.Example
{
    public class OutOfProcessHost : IDisposable
    {
        private Process _browserProcess;
        private JsonRpc _jsonRpc;
        private int _uiThreadId;
        private int _remoteuiThreadId;
        private int _browserIdentifier = 1;
        private ConcurrentDictionary<int, ChromiumWebBrowser> _browsers = new ConcurrentDictionary<int, ChromiumWebBrowser>();
        private TaskCompletionSource<OutOfProcessHost> _processInitialized = new TaskCompletionSource<OutOfProcessHost>(TaskCreationOptions.RunContinuationsAsynchronously);

        private OutOfProcessHost()
        {

        }

        public bool CreateBrowser(ChromiumWebBrowser browser, IntPtr handle, string url)
        {
            var id = _browserIdentifier++;
            _ = _jsonRpc.NotifyAsync("CreateBrowser", handle.ToInt32(), url, id);

            browser.Id = id;

            return _browsers.TryAdd(id, browser);
        }

        private Task<OutOfProcessHost> InitializedTask
        {
            get { return _processInitialized.Task; }
        }

        private void Init()
        {
            var currentProcess = Process.GetCurrentProcess();

            var args = $"--parentProcessId={currentProcess.Id}";

            _browserProcess = Process.Start(new ProcessStartInfo("CefSharp.OutOfProcess.BrowserProcess.exe", args)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            });

            _uiThreadId = Kernel32.GetCurrentThreadId();

            _jsonRpc = JsonRpc.Attach(_browserProcess.StandardInput.BaseStream, _browserProcess.StandardOutput.BaseStream);
            _jsonRpc.AllowModificationWhileListening = true;

            _jsonRpc.AddLocalRpcMethod("OnAfterBrowserCreated", (Action<int, int>)delegate (int id, int ptr)
            {
                if (_browsers.TryGetValue(id, out var chromiumWebBrowser))
                {
                    chromiumWebBrowser.SetBrowserHwnd(new IntPtr(ptr));
                }

                //var attached = User32.AttachThreadInput(_remoteThreadId, _uiThreadId, true);
            });

            _jsonRpc.AddLocalRpcMethod("OnContextInitialized", (Action<int>) delegate (int threadId)
            {
                _remoteuiThreadId = threadId;

                _processInitialized.TrySetResult(this);

                //var attached = User32.AttachThreadInput(_remoteThreadId, _uiThreadId, true);
            });

            _jsonRpc.AllowModificationWhileListening = false;
        }

        internal void CloseBrowser(int id)
        {
            _ = _jsonRpc?.NotifyAsync("CloseBrowser", id);
        }

        public void Dispose()
        {
            _jsonRpc?.NotifyAsync("CloseHost");
            _jsonRpc?.Dispose();
            _jsonRpc = null;
        }

        public static Task<OutOfProcessHost> CreateAsync()
        {
            var host = new OutOfProcessHost();

            host.Init();            

            return host.InitializedTask;
        }        
    }
}
