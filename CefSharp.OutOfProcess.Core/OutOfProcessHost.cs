using PInvoke;
using StreamJsonRpc;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess
{
    public class OutOfProcessHost : IDisposable
    {
        /// <summary>
        /// The CefSharp.OutOfProcess.BrowserProcess.exe name
        /// </summary>
        public const string HostExeName = "CefSharp.OutOfProcess.BrowserProcess.exe";

        private Process _browserProcess;
        private JsonRpc _jsonRpc;
        private int _uiThreadId;
        private int _remoteuiThreadId;
        private int _browserIdentifier = 1;
        private string _outofProcessFilePath;
        private ConcurrentDictionary<int, IChromiumWebBrowser> _browsers = new ConcurrentDictionary<int, IChromiumWebBrowser>();

        private TaskCompletionSource<OutOfProcessHost> _processInitialized = new TaskCompletionSource<OutOfProcessHost>(TaskCreationOptions.RunContinuationsAsynchronously);

        private OutOfProcessHost(string path)
        {
            _outofProcessFilePath = path;
        }

        public bool CreateBrowser(IChromiumWebBrowser browser, IntPtr handle, string url, out int id)
        {
            id = _browserIdentifier++;
            _ = _jsonRpc.NotifyAsync("CreateBrowser", handle.ToInt32(), url, id);

            return _browsers.TryAdd(id, browser);
        }

        internal Task SendDevToolsMessage(int browserId, string message)
        {
            return _jsonRpc.NotifyAsync("SendDevToolsMessage", browserId, message);            
        }

        private Task<OutOfProcessHost> InitializedTask
        {
            get { return _processInitialized.Task; }
        }

        private void Init()
        {
            var currentProcess = Process.GetCurrentProcess();

            var args = $"--parentProcessId={currentProcess.Id}";

            _browserProcess = Process.Start(new ProcessStartInfo(_outofProcessFilePath, args)
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

            _jsonRpc.AddLocalRpcMethod("OnDevToolsMessage", (Action<int, string>)delegate (int browserId, string jsonMsg)
            {
                if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
                {
                    chromiumWebBrowser.OnDevToolsMessage(jsonMsg);
                }
            });

            _jsonRpc.AddLocalRpcMethod("OnDevToolsAgentDetached", (Action<int>)delegate (int browserId)
            {
                if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
                {
                    
                }
            });

            _jsonRpc.AddLocalRpcMethod("OnDevToolsReady", (Action<int>)delegate (int browserId)
            {
                if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
                {
                    chromiumWebBrowser.OnDevToolsReady();
                }
            });

            _jsonRpc.AllowModificationWhileListening = false;
        }

        public void CloseBrowser(int id)
        {
            _ = _jsonRpc?.NotifyAsync("CloseBrowser", id);
        }

        public void Dispose()
        {
            _jsonRpc?.NotifyAsync("CloseHost");
            _jsonRpc?.Dispose();
            _jsonRpc = null;
        }

        public static Task<OutOfProcessHost> CreateAsync(string path = HostExeName)
        {
            if(string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Unable to find Host executable.", path);
            }

            var host = new OutOfProcessHost(fullPath);

            host.Init();            

            return host.InitializedTask;
        }        
    }
}
