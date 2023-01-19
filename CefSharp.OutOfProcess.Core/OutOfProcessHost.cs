namespace CefSharp.OutOfProcess
{
    using CefSharp.DevTools.Page;
    using CefSharp.OutOfProcess.Interface;
    using CefSharp.OutOfProcess.Interface.Callbacks;
    using CefSharp.OutOfProcess.Internal;
    using PInvoke;
    using StreamJsonRpc;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Rect = CefSharp.OutOfProcess.Interface.Rect;

    public class OutOfProcessHost : IOutOfProcessHostRpc, IDisposable
    {
        /// <summary>
        /// The CefSharp.OutOfProcess.BrowserProcess.exe name
        /// </summary>
        public const string HostExeName = "CefSharp.OutOfProcess.BrowserProcess.exe";

        private readonly bool _offscreenRendering;
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

        private OutOfProcessHost(string outOfProcessHostExePath, string cachePath = null, bool offscreenRendering = false)
        {
            _outofProcessHostExePath = outOfProcessHostExePath;
            _cachePath = cachePath;
            _offscreenRendering = offscreenRendering;
        }

        public event EventHandler<JsDialogCallbackDetails> JsDialogCallback;

        public event EventHandler<FileDialogCallbackDetails> FileDialogCallback;

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
        /// <param name="browser">The <see cref="IChromiumWebBrowserInternal"/> that will host the browser.</param>
        /// <param name="handle">handle used to host the control.</param>
        /// <param name="url">url.</param>
        /// <param name="requestContextPreferences">request context preference.</param>
        /// <param name="id">id.</param>
        /// <returns>if created.</returns>
        public bool CreateBrowser(IChromiumWebBrowserInternal browser, IntPtr handle, string url, out int id, IDictionary<string, object> requestContextPreferences = null)
        {
            id = _browserIdentifier++;
            _ = _outOfProcessClient.CreateBrowser(handle, url, id, requestContextPreferences);

            return _browsers.TryAdd(id, browser);
        }

        public Task ShowDevTools(int browserId)
        {
            return _outOfProcessClient.ShowDevTools(browserId);
        }

        public void SendMouseClickEvent(int browserId, int x, int y, MouseButtonType mouseButtonType, bool mouseUp, int clickCount, CefEventFlags eventFlags)
        {
            _outOfProcessClient.SendMouseClickEvent(browserId, x, y, mouseButtonType.ToString(), mouseUp, clickCount, (uint)eventFlags);
        }

        public Task LoadUrl(int browserId, string url)
        {
            return _outOfProcessClient.LoadUrl(browserId, url);
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

            var args = $"--parentProcessId={currentProcess.Id} --cachePath={_cachePath} --offscreenRendering={_offscreenRendering}";

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

        void IOutOfProcessHostRpc.NotifyPaint(int browserId, bool isPopup, Rect dirtyRect, int width, int height, string file)
        {
            ((IRenderHandlerInternal)GetBrowser(browserId))?.OnPaint(isPopup, dirtyRect, width, height, file);
        }

        void IOutOfProcessHostRpc.OnPopupShow(int browserId, bool show)
        {
            ((IRenderHandlerInternal)GetBrowser(browserId))?.OnPopupShow(show);
        }

        void IOutOfProcessHostRpc.OnPopupSize(int browserId, Rect rect)
        {
            ((IRenderHandlerInternal)GetBrowser(browserId))?.OnPopupSize(rect);
        }

        public void NotifyMoveOrResizeStarted(int id, int width, int height, int screenX, int screenY)
        {
            _outOfProcessClient.NotifyMoveOrResizeStarted(id, width, height, screenX, screenY);
        }

        Task<bool> IOutOfProcessHostRpc.OnFileDialog(int browserId, string mode, string title, string defaultFilePath, string[] acceptFilters, int callback)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser) && chromiumWebBrowser.DialogHandler != null)
            {

                var result = chromiumWebBrowser.DialogHandler.OnFileDialog(chromiumWebBrowser, (CefFileDialogMode)Enum.Parse(typeof(CefFileDialogMode), mode), title, defaultFilePath, acceptFilters.ToList(), new FileDialogCallbackProxy(this, callback, chromiumWebBrowser));
                return Task.FromResult(result);
            }

            return Task.FromResult(false);
        }

        #region JsDialog

        Task<bool> IOutOfProcessHostRpc.OnBeforeUnloadDialog(int browserId, string messageText, bool isReload, int callback)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                var handler = chromiumWebBrowser.JsDialogHandler;
                if (handler == null)
                {
                    return Task.FromResult(false);
                }

                var result = handler.OnBeforeUnloadDialog(chromiumWebBrowser, messageText, isReload, new JsDialogCallbackProxy(this, callback, chromiumWebBrowser));
                return Task.FromResult(result);
            }

            return Task.FromResult(false);
        }

        void IOutOfProcessHostRpc.OnDialogClosed(int browserId)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.JsDialogHandler?.OnDialogClosed(chromiumWebBrowser);
            }
        }

        Task<bool> IOutOfProcessHostRpc.OnJSDialog(int browserId, string originUrl, string dialogType, string messageText, string defaultPromptText, int callback, bool suppressMessage)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                var handler = chromiumWebBrowser.JsDialogHandler;
                if (handler == null)
                {
                    return Task.FromResult(false);
                }

                var result = handler.OnJSDialog(chromiumWebBrowser, originUrl, (CefJsDialogType)Enum.Parse(typeof(CefJsDialogType), dialogType), messageText, defaultPromptText, new JsDialogCallbackProxy(this, callback, chromiumWebBrowser), ref suppressMessage);
                return Task.FromResult(result);
            }

            return Task.FromResult(false);
        }

        void IOutOfProcessHostRpc.OnResetDialogState(int browserId)
        {
            if (_browsers.TryGetValue(browserId, out var chromiumWebBrowser))
            {
                chromiumWebBrowser.JsDialogHandler?.OnResetDialogState(chromiumWebBrowser);
            }
        }

        #endregion JsDialog

        /// <summary>
        /// Set whether the browser is focused. (Used for Normal Rendering e.g. WinForms).
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
            _browsers.TryRemove(id, out var browser);
        }

        public void Dispose()
        {
            _ = _outOfProcessClient.CloseHost();
            _jsonRpc?.Dispose();
            _jsonRpc = null;
        }

        public void UpdateRequestContextPreferences(int browserId, Dictionary<string, object> pref)
        {
            _outOfProcessClient.UpdateRequestContextPreferences(browserId, pref);
        }

        public static Task<OutOfProcessHost> CreateAsync(string path = HostExeName, bool offScreenRendering = false, string cachePath = null)
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

            var host = new OutOfProcessHost(fullPath, cachePath, offScreenRendering);

            host.Init();

            return host.InitializedTask;
        }

        internal void InvokeFileDialogCallback(FileDialogCallbackDetails callbackDetails) => FileDialogCallback.Invoke(this, callbackDetails);

        internal void InvokeJsDialogCallback(JsDialogCallbackDetails callbackDetails) => JsDialogCallback.Invoke(this, callbackDetails);

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
