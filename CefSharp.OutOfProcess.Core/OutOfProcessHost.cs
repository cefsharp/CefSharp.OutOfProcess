using CefSharp.OutOfProcess.Interface;
using CefSharp.OutOfProcess.Internal;
using Copy.CefSharp;
using Copy.CefSharp.Structs;
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

        /// <summary>
        /// UI Thread assocuated with this <see cref="OutOfProcessHost"/>
        /// </summary>
        public int UiThreadId => _uiThreadId;

        /// <summary>
        /// Thread Id of the UI Thread running in the Browser Process
        /// </summary>
        public int RemoteUiThreadId => _remoteuiThreadId;

        /// <summary>
        /// CefSharp Version
        /// </summary>
        public string CefSharpVersion => _cefSharpVersion;

        /// <summary>
        /// Cef Version
        /// </summary>
        public string CefVersion => _cefVersion;

        /// <summary>
        /// Chromium Version
        /// </summary>
        public string ChromiumVersion => _chromiumVersion;

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

        private Task<OutOfProcessHost> InitializedTask => _processInitialized.Task;

        private void Init()
        {
            var currentProcess = Process.GetCurrentProcess();

            var args = $"--parentProcessId={currentProcess.Id} --cachePath={_cachePath}";

            _browserProcess = Process.Start(new ProcessStartInfo(_outofProcessHostExePath, args)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
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
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.SetAddress(address);
            }
        }

        void IOutOfProcessHostRpc.NotifyBrowserCreated(int browserId, IntPtr browserHwnd)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.OnAfterBrowserCreated(browserHwnd);
            }
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
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.OnDevToolsMessage(devToolsMessage);
            }
        }

        void IOutOfProcessHostRpc.NotifyDevToolsReady(int browserId)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.OnDevToolsReady();
            }
        }

        void IOutOfProcessHostRpc.NotifyLoadingStateChange(int browserId, bool canGoBack, bool canGoForward, bool isLoading)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.SetLoadingStateChange(canGoBack, canGoForward, isLoading);
            }
        }

        public void NotifyMoveOrResizeStarted(int id, int width, int height, int screenX, int screenY) => _outOfProcessClient.NotifyMoveOrResizeStarted(id, width, height, screenX, screenY);

        public void LoadUrl(int id, string url) => _outOfProcessClient.LoadUrl(id, url);

        /// <summary>
        /// Set whether the browser is focused. (Used for Normal Rendering e.g. WinForms)
        /// </summary>
        /// <param name="id">browser id</param>
        /// <param name="focus">set focus</param>
        public void SetFocus(int id, bool focus) => _outOfProcessClient.SetFocus(id, focus);

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
            if (string.IsNullOrEmpty(path))
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

        void IOutOfProcessHostRpc.NotifyPaint(int browserId, bool isPopup, Rect dirtyRect, int width, int height, IntPtr buffer, byte[] data, string file)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.OnPaint(false, dirtyRect, width, height, buffer, data, file);
            }
        }

        public void SendMouseMoveEvent(int browserId, int x, int y, bool mouseLeave,Copy.CefSharp.CefEventFlags modifiers) 
            => _outOfProcessClient.SendMouseMoveEvent(browserId, x, y, mouseLeave, modifiers);

        public void SendCaptureLostEvent(int browserId) 
            => _outOfProcessClient.SendCaptureLostEvent(browserId);

        public void SendMouseClickEvent(int browserId, int x, int y, Copy.CefSharp.MouseButtonType changedButton, bool mouseUp, int clickCount, Copy.CefSharp.CefEventFlags modifiers) 
            => _outOfProcessClient.SendMouseClickEvent(browserId, x, y, changedButton, mouseUp, clickCount, modifiers);

        public IOutOfProcessClientRpc Client() => _outOfProcessClient;

        void IOutOfProcessHostRpc.NotifyFrameLoadStart(int browserId, string frameName, string url)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.OnFrameLoadStart(frameName, url);
            }
        }

        void IOutOfProcessHostRpc.NotifyFrameLoadEnd(int browserId, string frameName, string url, int httpStatusCode)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.OnFrameLoadEnd(frameName, url, httpStatusCode);
            }
        }
    }
}
