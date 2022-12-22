using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CefSharp.Avalonia.Internals;
using CefSharp.Dom;
using CefSharp.OutOfProcess;
using CefSharp.OutOfProcess.Internal;
using CefSharp.OutOfProcess.Model;
using PInvoke;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CefSharp.Avalonia
{
    /// <summary>
    /// The Avalonia CEF browser.
    /// </summary>
    public class ChromiumWebBrowser : NativeControlHost, IChromiumWebBrowserInternal
    {
        private static OutOfProcessHost _defaultOutOfProcessHost = null;

        private OutOfProcessHost _host;
        private IntPtr _browserHwnd = IntPtr.Zero;
        private OutOfProcessConnectionTransport _devToolsContextConnectionTransport;
        private IDevToolsContext _devToolsContext;
        private int _id;
        private bool _devToolsReady;

        /// <summary>
        /// Handle we'll use to host the browser
        /// </summary>
        private IntPtr _hwndHost;
        /// <summary>
        /// The ignore URI change
        /// </summary>
        private bool _ignoreUriChange;
        /// <summary>
        /// Initial address
        /// </summary>
        private string _initialAddress;
        /// <summary>
        /// Has the underlying Cef Browser been created (slightly different to initliazed in that
        /// the browser is initialized in an async fashion)
        /// </summary>
        private bool _browserCreated;
        /// <summary>
        /// The browser initialized - boolean represented as 0 (false) and 1(true) as we use Interlocker to increment/reset
        /// </summary>
        private int _browserInitialized;
        /// <summary>
        /// A flag that indicates whether or not the designer is active
        /// NOTE: Needs to be static for OnApplicationExit
        /// </summary>
        private static bool DesignMode;

        /// <summary>
        /// The value for disposal, if it's 1 (one) then this instance is either disposed
        /// or in the process of getting disposed
        /// </summary>
        private int _disposeSignaled;

        /// <summary>
        /// Current DPI Scale
        /// </summary>
        private double _dpiScale;

        /// <summary>
        /// This flag is set when the browser gets focus before the underlying CEF browser
        /// has been initialized.
        /// </summary>
        private bool _initialFocus;

        /// <summary>
        /// Can the browser navigate back.
        /// </summary>
        private bool _canGoBack;

        /// <summary>
        /// Can the browser navigate forward.
        /// </summary>
        private bool _canGoForward;

        /// <summary>
        /// Is the browser currently loading a web page.
        /// </summary>
        private bool _isLoading;

        /// <summary>
        /// Browser Title.
        /// </summary>
        private string _title;

        /// <summary>
        /// Address
        /// </summary>
        private string _address;

		/// <summary>
		/// Activates browser upon creation, the default value is false. Prior to version 73
		/// the default behaviour was to activate browser on creation (Equivilent of setting this property to true).
		/// To restore this behaviour set this value to true immediately after you create the <see cref="ChromiumWebBrowser"/> instance.
		/// https://bitbucket.org/chromiumembedded/cef/issues/1856/branch-2526-cef-activates-browser-window
		/// </summary>
		public bool ActivateBrowserOnCreation { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value><see langword="true" /> if this instance is disposed; otherwise, <see langword="false" />.</value>
        public bool IsDisposed
        {
            get
            {
                return Interlocked.CompareExchange(ref _disposeSignaled, 1, 1) == 1;
            }
        }

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
        public event EventHandler<Dom.PopupEventArgs> Popup;
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
        public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;
        /// <inheritdoc/>
        public event EventHandler<StatusMessageEventArgs> StatusMessage;
        /// <inheritdoc/>
        public event EventHandler<ConsoleEventArgs> ConsoleMessage;
        /// <inheritdoc/>
        public event EventHandler<LifecycleEventArgs> LifecycleEvent;
        /// <inheritdoc/>
        public event EventHandler DevToolsContextAvailable;

		/// <summary>
		/// Event handler that will get called when the browser title changes
		/// </summary>
		public event EventHandler<TitleChangedEventArgs> TitleChanged;

		/// <summary>
		/// Event called after the underlying CEF browser instance has been created
		/// </summary>
		public event EventHandler BrowserCreated;

		/// <summary>
		/// Navigates to the previous page in the browser history. Will automatically be enabled/disabled depending on the
		/// browser state.
		/// </summary>
		/// <value>The back command.</value>
		public ICommand BackCommand { get; private set; }
		/// <summary>
		/// Navigates to the next page in the browser history. Will automatically be enabled/disabled depending on the
		/// browser state.
		/// </summary>
		/// <value>The forward command.</value>
		public ICommand ForwardCommand { get; private set; }
		/// <summary>
		/// Reloads the content of the current page. Will automatically be enabled/disabled depending on the browser state.
		/// </summary>
		/// <value>The reload command.</value>
		public ICommand ReloadCommand { get; private set; }
		
		public ICommand StopCommand { get; private set; }

		/// <summary>
		/// CanGoBack Property
		/// </summary>
		public static readonly DirectProperty<ChromiumWebBrowser, bool> CanGoBackProperty =
            AvaloniaProperty.RegisterDirect<ChromiumWebBrowser, bool>(nameof(CanGoBack), o => o.CanGoBack);

		/// <summary>
		/// A flag that indicates whether the state of the control current supports the GoBack action (true) or not (false).
		/// </summary>
		/// <value><c>true</c> if this instance can go back; otherwise, <c>false</c>.</value>
		public bool CanGoBack
		{
			get { return _canGoBack; }
			private set { SetAndRaise(CanGoBackProperty, ref _canGoBack, value); }
		}

		/// <summary>
		/// CanGoBack Property
		/// </summary>
		public static readonly DirectProperty<ChromiumWebBrowser, bool> CanGoForwardProperty =
			AvaloniaProperty.RegisterDirect<ChromiumWebBrowser, bool>(nameof(CanGoForward), o => o.CanGoForward);

		/// <summary>
		/// A flag that indicates whether the state of the control current supports the GoForward action (true) or not (false).
		/// </summary>
		/// <value><c>true</c> if this instance can go forward; otherwise, <c>false</c>.</value>
		public bool CanGoForward
		{
			get { return _canGoForward; }
			private set { SetAndRaise(CanGoForwardProperty, ref _canGoForward, value); }
		}

		/// <summary>
		/// The title of the web page being currently displayed.
		/// </summary>
		/// <value>The title.</value>
		public string Title
		{
			get { return _title; }
			set { SetAndRaise(TitleProperty, ref _title, value); }
		}

		/// <summary>
		/// The title property
		/// </summary>
		public static readonly DirectProperty<ChromiumWebBrowser, string> TitleProperty =
			AvaloniaProperty.RegisterDirect<ChromiumWebBrowser, string>(nameof(Title), o => o.Title);

		/// <summary>
		/// Handles the <see cref="E:TitleChanged" /> event.
		/// </summary>
		/// <param name="d">The d.</param>
		/// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		private static void OnTitleChanged(ChromiumWebBrowser owner, AvaloniaPropertyChangedEventArgs e)
		{
            var args = new TitleChangedEventArgs(e.GetNewValue<string>());
			owner.TitleChanged?.Invoke(owner, args);
		}		

        static ChromiumWebBrowser()
        {
            AddressProperty.Changed.AddClassHandler<ChromiumWebBrowser>(OnAddressChanged);
            TitleProperty.Changed.AddClassHandler<ChromiumWebBrowser>(OnTitleChanged);
            IsVisibleProperty.Changed.AddClassHandler<ChromiumWebBrowser>(OnVisibleChanged);
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChromiumWebBrowser"/> instance.
		/// </summary>
		public ChromiumWebBrowser() : this(null)
        { 
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChromiumWebBrowser"/> instance.
		/// </summary>
		/// <param name="host">Out of process host</param>
		/// <param name="initialAddress">address to load initially</param>
		public ChromiumWebBrowser(OutOfProcessHost host, string initialAddress = null)
        {
            _host = host;
            _initialAddress = initialAddress;

            _host ??= _defaultOutOfProcessHost;

			if (_host == null)
			{
				throw new ArgumentNullException(nameof(host));
			}

			Focusable = true;

            BackCommand = new DelegateCommand(() => _devToolsContext.GoBackAsync(), () => CanGoBack);
            ForwardCommand = new DelegateCommand(() => _devToolsContext.GoForwardAsync(), () => CanGoForward);
            ReloadCommand = new DelegateCommand(() => _devToolsContext.ReloadAsync(), () => !IsLoading);
            //StopCommand = new DelegateCommand(this.Stop);

            UseLayoutRounding = true;
            LayoutUpdated += OnLayoutUpdated;
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            var bounds = Bounds;

            ResizeBrowser((int)bounds.Width, (int)bounds.Height);
        }

        /// <inheritdoc/>
        int IChromiumWebBrowserInternal.Id
        {
            get { return _id; }
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
        public Frame[] Frames => _devToolsContext?.Frames;

        /// <inheritdoc/>
        public Frame MainFrame => _devToolsContext?.MainFrame;

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

            // Only call Load if initialAddress is null and Address is not empty
            if (string.IsNullOrEmpty(_initialAddress) && !string.IsNullOrEmpty(Address))
            {
                LoadUrl(Address);
            }
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

        /// <inheritdoc />
        public Task<SetPreferenceResponse> SetRequestContextPreferenceAsync(string name, object value)
        {
            if (_host == null)
            {
                throw new ObjectDisposedException(nameof(ChromiumWebBrowser));
            }

            return _host.SetRequestContextPreferenceAsync(_id, name, value);
        }

        ///<inheritdoc/>
        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var handle = base.CreateNativeControlCore(parent);

            _dpiScale = this.GetVisualRoot()?.RenderScaling ?? 1.0;

            _hwndHost = handle.Handle;

            _host.CreateBrowser(this, _hwndHost, url: _initialAddress, out _id);

            _devToolsContextConnectionTransport = new OutOfProcessConnectionTransport(_id, _host);

            var connection = DevToolsConnection.Attach(_devToolsContextConnectionTransport);
            _devToolsContext = Dom.DevToolsContext.CreateForOutOfProcess(connection);

            return handle;
        }

        ///<inheritdoc/>
        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            _host.CloseBrowser(_id);

			base.DestroyNativeControlCore(control);
        }

		protected override void OnGotFocus(GotFocusEventArgs e)
		{
			base.OnGotFocus(e);

			if (InternalIsBrowserInitialized())
            {
                _host.SetFocus(_id, true);
            }
            else
            {
                _initialFocus = true;
            }
        }

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);

			if (InternalIsBrowserInitialized())
			{
				_host.SetFocus(_id, false);
			}
		}

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.SetAddress(string address)
        {
            UiThreadRun(() =>
            {
                _ignoreUriChange = true;
                Address = address;
                _ignoreUriChange = false;
            });
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.SetLoadingStateChange(bool canGoBack, bool canGoForward, bool isLoading)
        {
            UiThreadRun(() =>
            {
                CanGoBack = canGoBack;
                CanGoForward = CanGoForward;
                IsLoading = isLoading;

                ((DelegateCommand)BackCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)ForwardCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)ReloadCommand).RaiseCanExecuteChanged();
            });

            LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(canGoBack, canGoForward, isLoading));
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.SetTitle(string title)
        {
            UiThreadRun(() => Title = title);
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.SetStatusMessage(string msg)
        {
            StatusMessage?.Invoke(this, new StatusMessageEventArgs(msg));
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.OnAfterBrowserCreated(IntPtr hwnd)
        {
            if (IsDisposed)
            {
                return;
            }

            _browserHwnd = hwnd;

            Interlocked.Exchange(ref _browserInitialized, 1);

            UiThreadRun(() =>
            {
                if (!IsDisposed)
                {
                    BrowserCreated?.Invoke(this, EventArgs.Empty);

					var bounds = Bounds;

					ResizeBrowser((int)bounds.Width, (int)bounds.Height);
				}
            });            

            if (_initialFocus)
            {
                _host.SetFocus(_id, true);
            }
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
                if (_dpiScale > 1)
                {
                    width = (int)(width * _dpiScale);
                    height = (int)(height * _dpiScale);
                }

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

        private void ShowInternal(int width, int height)
        {
            if (_browserHwnd != IntPtr.Zero)
            {
                User32.SetWindowPos(_browserHwnd, IntPtr.Zero, 0, 0, width, height, User32.SetWindowPosFlags.SWP_NOZORDER);
            }
        }

        private void HideInternal()
        {
            if (_browserHwnd != IntPtr.Zero)
            {
                User32.SetWindowPos(_browserHwnd, IntPtr.Zero, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }

        /// <summary>
        /// The address (URL) which the browser control is currently displaying.
        /// Will automatically be updated as the user navigates to another page (e.g. by clicking on a link).
        /// </summary>
        /// <value>The address.</value>
        public string Address
        {
            get { return _address; }
            set { SetAndRaise(AddressProperty, ref _address, value); }
        }

        /// <summary>
        /// The address property
        /// </summary>
        public static readonly DirectProperty<ChromiumWebBrowser, string> AddressProperty = AvaloniaProperty.RegisterDirect<ChromiumWebBrowser, string>(nameof(Address), o => o.Address);

        private static void OnAddressChanged(ChromiumWebBrowser browser, AvaloniaPropertyChangedEventArgs e)
        {
            browser.OnAddressChanged(e.GetOldValue<string>(), e.GetNewValue<string>());

            browser.AddressChanged?.Invoke(browser, new AddressChangedEventArgs(e.GetNewValue<string>()));
        }

		/// <summary>
		/// Called when [address changed].
		/// </summary>
		/// <param name="oldValue">The old value.</param>
		/// <param name="newValue">The new value.</param>
		protected virtual void OnAddressChanged(string oldValue, string newValue)
        {
            if(!InternalIsBrowserInitialized())
            {
                _initialAddress = newValue;
                return;
            }

            if (_ignoreUriChange || newValue == null)
            {
                return;
            }

            LoadUrl(newValue);
        }

        /// <summary>
        /// A flag that indicates whether the control is currently loading one or more web pages (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance is loading; otherwise, <c>false</c>.</value>
        public bool IsLoading
        {
            get { return _isLoading ; }
            private set { SetAndRaise(IsLoadingProperty, ref _isLoading, value); }
        }

        /// <summary>
        /// The is loading property
        /// </summary>
        public static readonly DirectProperty<ChromiumWebBrowser, bool> IsLoadingProperty = AvaloniaProperty.RegisterDirect<ChromiumWebBrowser, bool>(nameof(IsLoading), o=> o.IsLoading);

		private static void OnVisibleChanged(ChromiumWebBrowser owner, AvaloniaPropertyChangedEventArgs args)
		{
			if (owner.InternalIsBrowserInitialized())
			{
				var isVisible = args.GetNewValue<bool>();
				if (isVisible)
				{
                    var bounds = owner.Bounds;
					owner.ResizeBrowser((int)bounds.Width, (int)bounds.Height);
				}
				else
				{
					//Hide browser
					owner.ResizeBrowser(0, 0);
				}
			}
		}

		/// <summary>
		/// Runs the specific Action on the Dispatcher in an async fashion
		/// </summary>
		/// <param name="action">The action.</param>
		/// <param name="priority">The priority.</param>
		private void UiThreadRun(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }

            _ = Dispatcher.UIThread.InvokeAsync(action);
        }

        /// <summary>
        /// Check is browserisinitialized
        /// </summary>
        /// <returns>true if browser is initialized</returns>
        private bool InternalIsBrowserInitialized()
        {
            // Use CompareExchange to read the current value - if disposeCount is 1, we set it to 1, effectively a no-op
            // Volatile.Read would likely use a memory barrier which I believe is unnecessary in this scenario
            return Interlocked.CompareExchange(ref _browserInitialized, 0, 0) == 1;
        }

        /// <summary>
        /// Sets a global (static) instance of <see cref="OutOfProcessHost"/> that
        /// will be used when no explicit implementation is provided.
        /// </summary>
        /// <param name="host">host</param>
        /// TODO: This needs improving
        public static void SetDefaultOutOfProcessHost(OutOfProcessHost host)
        {
            _defaultOutOfProcessHost = host;
        }

		protected virtual void Dispose(bool disposing)
		{
            // Attempt to move the disposeSignaled state from 0 to 1.
            // If successful, we can safely dispose of the object.
            if (Interlocked.CompareExchange(ref _disposeSignaled, 1, 0) != 0)
            {
                return;
            }

            if (DesignMode)
            {
                return;
			}

			if (disposing)
			{
				Interlocked.Exchange(ref _browserInitialized, 0);

				// Don't maintain a reference to event listeners anylonger:
				BrowserCreated = null;
				AddressChanged = null;
				LoadingStateChanged = null;
				StatusMessage = null;
				TitleChanged = null;

				var ctx = (DevToolsContext)_devToolsContext;

				ctx.DOMContentLoaded -= DOMContentLoaded;
				ctx.Error -= BrowserProcessCrashed;
				ctx.FrameAttached -= FrameAttached;
				ctx.FrameDetached -= FrameDetached;
				ctx.FrameNavigated -= FrameNavigated;
				ctx.Load -= JavaScriptLoad;
				ctx.PageError -= RuntimeExceptionThrown;
				ctx.Popup -= Popup;
				ctx.Request -= NetworkRequest;
				ctx.RequestFailed -= NetworkRequestFailed;
				ctx.RequestFinished -= NetworkRequestFinished;
				ctx.RequestServedFromCache -= NetworkRequestServedFromCache;
				ctx.Response -= NetworkResponse;
				ctx.Console -= ConsoleMessage;
				ctx.LifecycleEvent -= LifecycleEvent;

                DOMContentLoaded = null;
                BrowserProcessCrashed = null;
                FrameAttached = null;
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
                LifecycleEvent = null;
			}

            _host?.CloseBrowser(_id);
            _host = null;
		}

		~ChromiumWebBrowser()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
