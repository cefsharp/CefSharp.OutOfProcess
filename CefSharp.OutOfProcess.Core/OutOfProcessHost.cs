using CefSharp.OutOfProcess.Interface;
using CefSharp.OutOfProcess.Interface.Callbacks;
using CefSharp.OutOfProcess.Internal;
using PInvoke;
using StreamJsonRpc;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess
{
    public class OutOfProcessHost : IOutOfProcessHostRpc, IDisposable
    {
        /// <summary>
        /// The CefSharp.OutOfProcess.BrowserProcess.exe name
        /// </summary>
        public const string HostExeName = "CefSharp.OutOfProcess.BrowserProcess.exe";

        private Process _browserProcess;
        private JsonRpc _jsonRpc;
        private IOutOfProcessClientRpc _outOfProcessClient;
        private string _cefSharpVersion;
        private string _cefVersion;
        private string _chromiumVersion;
        private int _uiThreadId;
        private int _remoteuiThreadId;
        private int _browserIdentifier = 1;
        private string _outofProcessHostExePath;
        private string _cachePath;
        private ConcurrentDictionary<int, IChromiumWebBrowserInternal> _browsers = new ConcurrentDictionary<int, IChromiumWebBrowserInternal>();
        private TaskCompletionSource<OutOfProcessHost> _processInitialized = new TaskCompletionSource<OutOfProcessHost>(TaskCreationOptions.RunContinuationsAsynchronously);

        private OutOfProcessHost(string outOfProcessHostExePath, string cachePath = null)
        {
            _outofProcessHostExePath = outOfProcessHostExePath;
            _cachePath = cachePath;
        }

        public event EventHandler<DownloadCallbackDetails> DownloadCallback;

        public event EventHandler<BeforeDownloadCallbackDetails> BeforeDownloadCallback;


        /// <summary>
        /// UI Thread assocuated with this <see cref="OutOfProcessHost"/>
        /// </summary>
        public int UiThreadId
        {
            get { return _uiThreadId; }
        }

        /// <summary>
        /// Thread Id of the UI Thread running in the Browser Process
        /// </summary>
        public int RemoteUiThreadId
        {
            get { return _remoteuiThreadId; }
        }

        /// <summary>
        /// CefSharp Version
        /// </summary>
        public string CefSharpVersion
        {
            get { return _cefSharpVersion; }
        }

        /// <summary>
        /// Cef Version
        /// </summary>
        public string CefVersion
        {
            get { return _cefVersion; }
        }

        /// <summary>
        /// Chromium Version
        /// </summary>
        public string ChromiumVersion
        {
            get { return _chromiumVersion; }
        }

        /// <summary>
        /// Sends an IPC message to the Browser Process instructing it
        /// to create a new Out of process browser
        /// </summary>
        /// <param name="browser">The <see cref="IChromiumWebBrowserInternal"/> that will host the browser</param>
        /// <param name="handle">handle used to host the control</param>
        /// <param name="url"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool CreateBrowser(IChromiumWebBrowserInternal browser, IntPtr handle, string url, out int id)
        {
            id = _browserIdentifier++;
            _ = _outOfProcessClient.CreateBrowser(handle, url, id);

            return _browsers.TryAdd(id, browser);
        }

        internal Task SendDevToolsMessageAsync(int browserId, string message)
        {
            return _outOfProcessClient.SendDevToolsMessage(browserId, message);
        }

        private Task<OutOfProcessHost> InitializedTask
        {
            get { return _processInitialized.Task; }
        }

        private void Init()
        {
            var currentProcess = Process.GetCurrentProcess();

            var args = $"--parentProcessId={currentProcess.Id} --cachePath={_cachePath}";

            _browserProcess = Process.Start(new ProcessStartInfo(_outofProcessHostExePath, args)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            });

            _browserProcess.Exited += OnBrowserProcessExited;

            _jsonRpc = JsonRpc.Attach(_browserProcess.StandardInput.BaseStream, _browserProcess.StandardOutput.BaseStream);

            _outOfProcessClient = _jsonRpc.Attach<IOutOfProcessClientRpc>();
            _jsonRpc.AllowModificationWhileListening = true;
            _jsonRpc.AddLocalRpcTarget<IOutOfProcessHostRpc>(this, null);
            _jsonRpc.AllowModificationWhileListening = false;

            _uiThreadId = Kernel32.GetCurrentThreadId();
        }

        private void OnBrowserProcessExited(object sender, EventArgs e)
        {
            var exitCode = _browserProcess.ExitCode;
        }

        void IOutOfProcessHostRpc.NotifyAddressChanged(int browserId, string address)
        {
            GetBrowser(browserId)?.SetAddress(address);
        }

        void IOutOfProcessHostRpc.NotifyBrowserCreated(int browserId, IntPtr browserHwnd)
        {
            GetBrowser(browserId)?.OnAfterBrowserCreated(browserHwnd);
        }

        void IOutOfProcessHostRpc.NotifyContextInitialized(int threadId, string cefSharpVersion, string cefVersion, string chromiumVersion)
        {
            _remoteuiThreadId = threadId;
            _cefSharpVersion = cefSharpVersion;
            _cefVersion = cefVersion;
            _chromiumVersion = chromiumVersion;

            _processInitialized.TrySetResult(this);
        }

        void IOutOfProcessHostRpc.NotifyDevToolsAgentDetached(int browserId)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {

            }
        }

        void IOutOfProcessHostRpc.NotifyDevToolsMessage(int browserId, string devToolsMessage)
        {
            GetBrowser(browserId)?.OnDevToolsMessage(devToolsMessage);
        }

        void IOutOfProcessHostRpc.NotifyDevToolsReady(int browserId)
        {
            GetBrowser(browserId)?.OnDevToolsReady();
        }

        void IOutOfProcessHostRpc.NotifyLoadingStateChange(int browserId, bool canGoBack, bool canGoForward, bool isLoading)
        {
            GetBrowser(browserId)?.SetLoadingStateChange(canGoBack, canGoForward, isLoading);
        }

        void IOutOfProcessHostRpc.NotifyStatusMessage(int browserId, string statusMessage)
        {
            GetBrowser(browserId)?.SetStatusMessage(statusMessage);
        }

        void IOutOfProcessHostRpc.NotifyTitleChanged(int browserId, string title)
        {
            GetBrowser(browserId)?.SetTitle(title);
        }

        public void NotifyMoveOrResizeStarted(int id)
        {
            _outOfProcessClient.NotifyMoveOrResizeStarted(id);
        }

        /// <summary>
        /// Set whether the browser is focused. (Used for Normal Rendering e.g. WinForms)
        /// </summary>
        /// <param name="id">browser id</param>
        /// <param name="focus">set focus</param>
        public void SetFocus(int id, bool focus)
        {
            _outOfProcessClient.SetFocus(id, focus);
        }

        public void CloseBrowser(int id)
        {
            _ = _outOfProcessClient.CloseBrowser(id);
        }

        public void Dispose()
        {
            _ = _outOfProcessClient.CloseHost();
            _jsonRpc?.Dispose();
            _jsonRpc = null;
        }

        public static Task<OutOfProcessHost> CreateAsync(string path = HostExeName, string cachePath = null)
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

            var host = new OutOfProcessHost(fullPath, cachePath);

            host.Init();            

            return host.InitializedTask;
        }


        void IOutOfProcessHostRpc.OnBeforeDownload(int browserId, CefSharp.OutOfProcess.Interface.Callbacks.DownloadItem downloadItem, int callback)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.DownloadHandler?.OnBeforeDownload(chromiumWebBrowser, downloadItem, new DownloadCallbackProxy(this, callback, chromiumWebBrowser));
            }
        }

        void IOutOfProcessHostRpc.OnDownloadUpdated(int browserId, CefSharp.OutOfProcess.Interface.Callbacks.DownloadItem downloadItem, int callback)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                GetBrowser(browserId)?.DownloadHandler?.OnDownloadUpdated(chromiumWebBrowser, downloadItem, new DownloadCallbackProxy(this, callback, chromiumWebBrowser));
            }
        }

        private IChromiumWebBrowserInternal GetBrowser(int browserId)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                return chromiumWebBrowser;
            }

            return null;
        }

        Task<bool> IOutOfProcessHostRpc.OnCanDownloadAsync(int browserId, string url, string requestMethod)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                var handler = chromiumWebBrowser.DownloadHandler;
                if (handler == null)
                {
                    return Task.FromResult(false);
                }

                var result = handler.CanDownload(chromiumWebBrowser, url, requestMethod);
                return Task.FromResult(result);
            }

            return Task.FromResult(false);
        }

        internal void InvokeBeforeDownloadCallback(BeforeDownloadCallbackDetails callbackDetails) => BeforeDownloadCallback.Invoke(this, callbackDetails);

        internal void InvokeDownloadCallback(DownloadCallbackDetails callbackDetails) => DownloadCallback.Invoke(this, callbackDetails);
    }
}
