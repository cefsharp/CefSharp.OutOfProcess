using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp.OutOfProcess.Internal;
using CefSharp.Puppeteer;
using PInvoke;

namespace CefSharp.OutOfProcess.WinForms
{
    public class ChromiumWebBrowser : Control, IChromiumWebBrowserInternal
    {
        private IntPtr _browserHwnd = IntPtr.Zero;
        private OutOfProcessHost _host;
        private readonly string _initialAddress;
        private int _id;
        private IDevToolsContext _devToolsContext;
        private OutOfProcessConnectionTransport _devToolsContextConnectionTransport;
        private bool _devToolsReady;

        /// <inheritdoc/>
        public event EventHandler DOMContentLoaded;
        /// <inheritdoc/>
        public event EventHandler<ErrorEventArgs> BrowserProcessCrashed;
        /// <inheritdoc/>
        public event EventHandler<FrameEventArgs> FrameAttached;
        /// <inheritdoc/>
        public event EventHandler<FrameEventArgs> FrameDetached;
        /// <inheritdoc/>
        public event EventHandler<FrameEventArgs> FrameNavigated;
        /// <inheritdoc/>
        public event EventHandler JavaScriptLoad;
        /// <inheritdoc/>
        public event EventHandler<PageErrorEventArgs> RuntimeExceptionThrown;
        /// <inheritdoc/>
        public event EventHandler<Puppeteer.PopupEventArgs> Popup;
        /// <inheritdoc/>
        public event EventHandler<RequestEventArgs> NetworkRequest;
        /// <inheritdoc/>
        public event EventHandler<RequestEventArgs> NetworkRequestFailed;
        /// <inheritdoc/>
        public event EventHandler<RequestEventArgs> NetworkRequestFinished;
        /// <inheritdoc/>
        public event EventHandler<RequestEventArgs> NetworkRequestServedFromCache;
        /// <inheritdoc/>
        public event EventHandler<ResponseCreatedEventArgs> NetworkResponse;
        /// <inheritdoc/>
        public event EventHandler<AddressChangedEventArgs> AddressChanged;
        /// <inheritdoc/>
        public event EventHandler<TitleChangedEventArgs> TitleChanged;
        /// <inheritdoc/>
        public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;
        /// <inheritdoc/>
        public event EventHandler<StatusMessageEventArgs> StatusMessage;
        /// <inheritdoc/>
        public event EventHandler<ConsoleEventArgs> ConsoleMessage;
        /// <inheritdoc/>
        public event EventHandler<LifecycleEventArgs> LifecycleEvent;
        /// <inheritdoc/>
        public event EventHandler DevToolsContextAvailable;

        public ChromiumWebBrowser(OutOfProcessHost host, string initialAddress)
        {
            _host = host;
            _initialAddress = initialAddress;
        }

        /// <inheritdoc/>
        int IChromiumWebBrowserInternal.Id
        {
            get { return _id; }
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.SetBrowserHwnd(IntPtr hwnd)
        {
            _browserHwnd = hwnd;
        }

        /// <inheritdoc/>
        protected override Size DefaultSize
        {
            get { return new Size(640, 480); }
        }

        /// <summary>
        /// DevToolsContext - provides communication with the underlying browser
        /// </summary>
        public IDevToolsContext DevToolsContext
        {
            get
            {
                if (_devToolsReady)
                {
                    return _devToolsContext;
                }

                return default;
            }
        }

        /// <inheritdoc/>
        public bool IsBrowserInitialized => _browserHwnd != IntPtr.Zero;

        /// <inheritdoc/>
        public string Address => _devToolsContext == null ? string.Empty : _devToolsContext.Url;

        /// <inheritdoc/>
        public Frame[] Frames => _devToolsContext == null ? null : _devToolsContext.Frames;

        /// <inheritdoc/>
        public Frame MainFrame => _devToolsContext == null ? null : _devToolsContext.MainFrame;

        /// <inheritdoc/>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            var size = Size;

            _host.CreateBrowser(this, Handle, url: _initialAddress, out _id);

            _devToolsContextConnectionTransport = new OutOfProcessConnectionTransport(_id, _host);

            var connection = DevToolsConnection.Attach(_devToolsContextConnectionTransport);
            _devToolsContext = Puppeteer.DevToolsContext.CreateForOutOfProcess(connection);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(disposing)
            {
                _devToolsContext.DOMContentLoaded -= DOMContentLoaded;
                _devToolsContext.Error -= BrowserProcessCrashed;
                _devToolsContext.FrameAttached -= FrameAttached;
                _devToolsContext.FrameDetached -= FrameDetached;
                _devToolsContext.FrameNavigated -= FrameNavigated;
                _devToolsContext.Load -= JavaScriptLoad;
                _devToolsContext.PageError -= RuntimeExceptionThrown;
                _devToolsContext.Popup -= Popup;
                _devToolsContext.Request -= NetworkRequest;
                _devToolsContext.RequestFailed -= NetworkRequestFailed;
                _devToolsContext.RequestFinished -= NetworkRequestFinished;
                _devToolsContext.RequestServedFromCache -= NetworkRequestServedFromCache;
                _devToolsContext.Response -= NetworkResponse;
                _devToolsContext.Console -= ConsoleMessage;

                DOMContentLoaded = null;
                BrowserProcessCrashed = null;
                FrameAttached = null;
                FrameDetached = null;
                FrameNavigated = null;
                JavaScriptLoad = null;
                RuntimeExceptionThrown = null;
                Popup = null;
                NetworkRequest = null;
                NetworkRequestFailed = null;
                NetworkRequestFinished = null;
                NetworkRequestServedFromCache = null;
                NetworkResponse = null;
                ConsoleMessage = null;

                _browserHwnd = IntPtr.Zero;

                _host?.CloseBrowser(_id);
                _host = null;
            }
        }

        /// <inheritdoc />
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)
            {
                ShowInternal(Width, Height);
            }
            else
            {
                HideInternal();
            }

            base.OnVisibleChanged(e);
        }

        /// <inheritdoc/>
        protected override void OnSizeChanged(EventArgs e)
        {
            ResizeBrowser(Width, Height);

            base.OnSizeChanged(e);
        }

        /// <summary>
        /// Resizes the browser to the specified <paramref name="width"/> and <paramref name="height"/>.
        /// If <paramref name="width"/> and <paramref name="height"/> are both 0 then the browser
        /// will be hidden and resource usage will be minimised.
        /// </summary>
        /// <param name="width">width</param>
        /// <param name="height">height</param>
        protected virtual void ResizeBrowser(int width, int height)
        {
            if (_browserHwnd != IntPtr.Zero)
            {
                if (width == 0 && height == 0)
                {
                    // For windowed browsers when the frame window is minimized set the
                    // browser window size to 0x0 to reduce resource usage.
                    HideInternal();
                }
                else
                {
                    ShowInternal(width, height);
                }
            }
        }

        /// <summary>
        /// When minimized set the browser window size to 0x0 to reduce resource usage.
        /// https://github.com/chromiumembedded/cef/blob/c7701b8a6168f105f2c2d6b239ce3958da3e3f13/tests/cefclient/browser/browser_window_std_win.cc#L87
        /// </summary>
        internal virtual void HideInternal()
        {
            if (_browserHwnd != IntPtr.Zero)
            {
                User32.SetWindowPos(_browserHwnd, IntPtr.Zero, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }

        /// <summary>
        /// Show the browser (called after previous minimised)
        /// </summary>
        internal virtual void ShowInternal(int width, int height)
        {
            if (_browserHwnd != IntPtr.Zero)
            {
                User32.SetWindowPos(_browserHwnd, IntPtr.Zero, 0, 0, width, height, User32.SetWindowPosFlags.SWP_NOZORDER);
            }
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.OnDevToolsMessage(string jsonMsg)
        {
            _devToolsContextConnectionTransport?.InvokeMessageReceived(jsonMsg);
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.OnDevToolsReady()
        {
            var ctx = (DevToolsContext)_devToolsContext;

            ctx.DOMContentLoaded += DOMContentLoaded;
            ctx.Error += BrowserProcessCrashed;
            ctx.FrameAttached += FrameAttached;
            ctx.FrameDetached += FrameDetached;
            ctx.FrameNavigated += FrameNavigated;
            ctx.Load += JavaScriptLoad;
            ctx.PageError += RuntimeExceptionThrown;
            ctx.Popup += Popup;
            ctx.Request += NetworkRequest;
            ctx.RequestFailed += NetworkRequestFailed;
            ctx.RequestFinished += NetworkRequestFinished;
            ctx.RequestServedFromCache += NetworkRequestServedFromCache;
            ctx.Response += NetworkResponse;
            ctx.Console += ConsoleMessage;
            ctx.LifecycleEvent += LifecycleEvent;

            _ = ctx.InvokeGetFrameTreeAsync().ContinueWith(t =>
                {
                    _devToolsReady = true;

                    DevToolsContextAvailable?.Invoke(this, EventArgs.Empty);

                    //NOW the user can start using the devtools context
                }, TaskScheduler.Current);
        }

        /// <inheritdoc/>
        public void LoadUrl(string url)
        {
            _ = _devToolsContext.GoToAsync(url);
        }

        /// <inheritdoc/>
        public Task<Response> LoadUrlAsync(string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null)
        {
            return _devToolsContext.GoToAsync(url, timeout, waitUntil);
        }

        /// <inheritdoc/>
        public Task<Response> GoBackAsync(NavigationOptions options = null)
        {
            return _devToolsContext.GoBackAsync(options);
        }

        /// <inheritdoc/>
        public Task<Response> GoForwardAsync(NavigationOptions options = null)
        {
            return _devToolsContext.GoForwardAsync(options);
        }

        public void SetTitle(string title)
        {
            TitleChanged?.Invoke(this, new TitleChangedEventArgs(title));
        }

        public void SetAddress(string address)
        {
            AddressChanged?.Invoke(this, new AddressChangedEventArgs(address));
        }

        public void SetLoadingStateChange(bool canGoBack, bool canGoForward, bool isLoading)
        {
            LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(canGoBack, canGoForward, isLoading));
        }

        public void SetStatusMessage(string msg)
        {
            StatusMessage?.Invoke(this, new StatusMessageEventArgs(msg));
        }
    }
}
