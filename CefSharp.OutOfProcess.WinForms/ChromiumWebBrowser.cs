using System;
using System.Drawing;
using System.Windows.Forms;
using CefSharp.OutOfProcess;
using CefSharp.Puppeteer;
using PInvoke;

namespace CefSharp.OutOfProcess.WinForms
{
    public class ChromiumWebBrowser : Control, IChromiumWebBrowser
    {
        private IntPtr _browserHwnd = IntPtr.Zero;
        private int _remoteThreadId = -1;
        private int _uiThreadId = -1;
        private bool _disposed;
        private OutOfProcessHost _host;
        private readonly string _initialAddress;
        private int _id;
        private IDevToolsContext _devToolsContext;

        int IChromiumWebBrowser.Id
        {
            get { return _id; }
        }

        public ChromiumWebBrowser(OutOfProcessHost host, string initialAddress)
        {
            _host = host;
            _initialAddress = initialAddress;
        }

        void IChromiumWebBrowser.SetBrowserHwnd(IntPtr hwnd)
        {
            _browserHwnd = hwnd;
        }

        /// <summary>
        /// Gets the default size of the control.
        /// </summary>
        /// <value>
        /// The default <see cref="T:System.Drawing.Size" /> of the control.
        /// </value>
        protected override Size DefaultSize
        {
            get { return new Size(200, 100); }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            _host.CreateBrowser(this, Handle, url: _initialAddress, out _id);

            var connection = Connection.Attach(new OutOfProcessConnectionTransport(_id, _host));
            _devToolsContext = DevToolsContext.CreateForOutOfProcess(connection);
        }

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
                _disposed = true;

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

        public void OnDevToolsMessage(string jsonMsg)
        {
            if(_devToolsContext != null)
            {
                //TODO: This is messy, cleanup the ownership to improve this
                _devToolsContext = (DevToolsContext)_devToolsContext;
                var transport = _devToolsContext.Client.Transport as OutOfProcessConnectionTransport;

                transport.InvokeMessageReceived(jsonMsg);
            }
        }

        public void OnDevToolsReady()
        {
            var ctx = (DevToolsContext)_devToolsContext;

            _ = ctx.InvokeGetFrameTreeAsync().ContinueWith(t =>
            {
                //NOW the user can start using the devtools context
            });            
        }
    }
}
