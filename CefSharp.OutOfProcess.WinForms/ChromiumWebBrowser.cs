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
        private int _remoteThreadId = -1;
        private int _uiThreadId = -1;
        private OutOfProcessHost _host;
        private readonly string _initialAddress;
        private int _id;
        private IDevToolsContext _devToolsContext;
        private OutOfProcessConnectionTransport _devToolsContextConnectionTransport;

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

            _host.CreateBrowser(this, Handle, url: _initialAddress, out _id);

            _devToolsContextConnectionTransport = new OutOfProcessConnectionTransport(_id, _host);

            var connection = Connection.Attach(_devToolsContextConnectionTransport);
            _devToolsContext = DevToolsContext.CreateForOutOfProcess(connection);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(disposing)
            {
                if (_remoteThreadId > 0 && _uiThreadId > 0)
                {
                    //User32.AttachThreadInput(_remoteThreadId, _uiThreadId, false);
                }

                _remoteThreadId = -1;
                _uiThreadId = -1;

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

            _ = ctx.InvokeGetFrameTreeAsync().ContinueWith(t =>
            {
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
    }
}
