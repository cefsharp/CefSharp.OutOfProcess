using CefSharp.Internals;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using CefSharp.OutOfProcess.Interface;
using System.Diagnostics;
using CefSharp.Structs;
using CefSharp.Enums;
using CefSharp.Wpf.Internals;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.IO;
using CefSharp.Callback;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    /// <summary>
    /// An ChromiumWebBrowser instance specifically for hosting CEF out of process
    /// </summary>
    public partial class OutOfProcessChromiumWebBrowser : IRenderWebBrowser
    {
        public const string BrowserNotInitializedExceptionErrorMessage =
            "The ChromiumWebBrowser instance creates the underlying Chromium Embedded Framework (CEF) browser instance in an async fashion. " +
            "The undelying CefBrowser instance is not yet initialized. Use the IsBrowserInitializedChanged event and check " +
            "the IsBrowserInitialized property to determine when the browser has been initialized.";

        public const uint WS_EX_NOACTIVATE = 0x08000000;

        private IDevToolsMessageObserver _devtoolsMessageObserver;
        private IRegistration _devtoolsRegistration;

        /// <summary>
        /// Internal ID used for tracking browsers between Processes;
        /// </summary>
        private int _id;

        /// <summary>
        /// The managed cef browser adapter
        /// </summary>
        private IBrowserAdapter _managedCefBrowserAdapter;

        /// <summary>
        /// JSON RPC used for IPC with host
        /// </summary>
        private IOutOfProcessHostRpc _outofProcessHostRpc;

        /// <summary>
        /// Flag to guard the creation of the underlying browser - only one instance can be created
        /// </summary>
        private bool _browserCreated;

        /// <summary>
        /// Used as workaround for issue https://github.com/cefsharp/CefSharp/issues/3021
        /// </summary>
        private long _canExecuteJavascriptInMainFrameId;

        /// <summary>
        /// The browser initialized - boolean represented as 0 (false) and 1(true) as we use Interlocker to increment/reset
        /// </summary>
        private int _browserInitialized;

        /// <summary>
        /// The value for disposal, if it's 1 (one) then this instance is either disposed
        /// or in the process of getting disposed
        /// </summary>
        private int _disposeSignaled;

        /// <summary>
        /// Id
        /// </summary>
        public int Id => _id;

        /// <summary>
        /// Get access to the core <see cref="IBrowser"/> instance.
        /// Maybe null if the underlying CEF Browser has not yet been
        /// created or if this control has been disposed. Check
        /// <see cref="IBrowser.IsDisposed"/> before accessing.
        /// </summary>
        public IBrowser BrowserCore { get; internal set; }

        /// <summary>
        /// A flag that indicates if you can execute javascript in the main frame.
        /// Flag is set to true in IRenderProcessMessageHandler.OnContextCreated.
        /// and false in IRenderProcessMessageHandler.OnContextReleased
        /// </summary>
        public bool CanExecuteJavascriptInMainFrame { get; private set; }
        /// <summary>
        /// Implement <see cref="IDialogHandler" /> and assign to handle dialog events.
        /// </summary>
        /// <value>The dialog handler.</value>
        public IDialogHandler DialogHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IJsDialogHandler" /> and assign to handle events related to JavaScript Dialogs.
        /// </summary>
        /// <value>The js dialog handler.</value>
        public IJsDialogHandler JsDialogHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IKeyboardHandler" /> and assign to handle events related to key press.
        /// </summary>
        /// <value>The keyboard handler.</value>
        public IKeyboardHandler KeyboardHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IRequestHandler" /> and assign to handle events related to browser requests.
        /// </summary>
        /// <value>The request handler.</value>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), DefaultValue(null)]
        public IRequestHandler RequestHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IDownloadHandler" /> and assign to handle events related to downloading files.
        /// </summary>
        /// <value>The download handler.</value>
        public IDownloadHandler DownloadHandler { get; set; }
        /// <summary>
        /// Implement <see cref="ILoadHandler" /> and assign to handle events related to browser load status.
        /// </summary>
        /// <value>The load handler.</value>
        public ILoadHandler LoadHandler { get; set; }
        /// <summary>
        /// Implement <see cref="ILifeSpanHandler" /> and assign to handle events related to popups.
        /// </summary>
        /// <value>The life span handler.</value>
        public ILifeSpanHandler LifeSpanHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IDisplayHandler" /> and assign to handle events related to browser display state.
        /// </summary>
        /// <value>The display handler.</value>
        public IDisplayHandler DisplayHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IContextMenuHandler" /> and assign to handle events related to the browser context menu
        /// </summary>
        /// <value>The menu handler.</value>
        public IContextMenuHandler MenuHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IRenderProcessMessageHandler" /> and assign to handle messages from the render process.
        /// </summary>
        /// <value>The render process message handler.</value>
        public IRenderProcessMessageHandler RenderProcessMessageHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IFindHandler" /> to handle events related to find results.
        /// </summary>
        /// <value>The find handler.</value>
        public IFindHandler FindHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IAudioHandler" /> to handle audio events.
        /// </summary>
        public IAudioHandler AudioHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IFrameHandler" /> to handle frame events.
        /// </summary>
        public IFrameHandler FrameHandler { get; set; }
        /// <summary>
        /// The <see cref="IFocusHandler" /> for this ChromiumWebBrowser.
        /// </summary>
        /// <value>The focus handler.</value>
        /// <remarks>If you need customized focus handling behavior for WinForms, the suggested
        /// best practice would be to inherit from DefaultFocusHandler and try to avoid
        /// needing to override the logic in OnGotFocus. The implementation in
        /// DefaultFocusHandler relies on very detailed behavior of how WinForms and
        /// Windows interact during window activation.</remarks>
        public IFocusHandler FocusHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IDragHandler" /> and assign to handle events related to dragging.
        /// </summary>
        /// <value>The drag handler.</value>
        public IDragHandler DragHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IResourceRequestHandlerFactory" /> and control the loading of resources
        /// </summary>
        /// <value>The resource handler factory.</value>
        public IResourceRequestHandlerFactory ResourceRequestHandlerFactory { get; set; }

        /// <summary>
        /// Event handler that will get called when the resource load for a navigation fails or is canceled.
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI
        /// thread. It is unwise to block on this thread for any length of time as your browser will become unresponsive and/or hang..
        /// To access UI elements you'll need to Invoke/Dispatch onto the UI Thread.
        /// </summary>
        public event EventHandler<LoadErrorEventArgs> LoadError;
        /// <summary>
        /// Event handler that will get called when the browser begins loading a frame. Multiple frames may be loading at the same
        /// time. Sub-frames may start or continue loading after the main frame load has ended. This method may not be called for a
        /// particular frame if the load request for that frame fails. For notification of overall browser load status use
        /// OnLoadingStateChange instead.
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI
        /// thread. It is unwise to block on this thread for any length of time as your browser will become unresponsive and/or hang..
        /// To access UI elements you'll need to Invoke/Dispatch onto the UI Thread.
        /// </summary>
        /// <remarks>Whilst this may seem like a logical place to execute js, it's called before the DOM has been loaded, implement
        /// <see cref="IRenderProcessMessageHandler.OnContextCreated" /> as it's called when the underlying V8Context is created
        /// </remarks>
        public event EventHandler<FrameLoadStartEventArgs> FrameLoadStart;
        /// <summary>
        /// Event handler that will get called when the browser is done loading a frame. Multiple frames may be loading at the same
        /// time. Sub-frames may start or continue loading after the main frame load has ended. This method will always be called
        /// for all frames irrespective of whether the request completes successfully.
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI
        /// thread. It is unwise to block on this thread for any length of time as your browser will become unresponsive and/or hang..
        /// To access UI elements you'll need to Invoke/Dispatch onto the UI Thread.
        /// </summary>
        public event EventHandler<FrameLoadEndEventArgs> FrameLoadEnd;
        /// <summary>
        /// Event handler that will get called when the Loading state has changed.
        /// This event will be fired twice. Once when loading is initiated either programmatically or
        /// by user action, and once when loading is terminated due to completion, cancellation of failure.
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI
        /// thread. It is unwise to block on this thread for any length of time as your browser will become unresponsive and/or hang..
        /// To access UI elements you'll need to Invoke/Dispatch onto the UI Thread.
        /// </summary>
        public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;
        /// <summary>
        /// Event handler for receiving Javascript console messages being sent from web pages.
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI
        /// thread. It is unwise to block on this thread for any length of time as your browser will become unresponsive and/or hang..
        /// To access UI elements you'll need to Invoke/Dispatch onto the UI Thread.
        /// (The exception to this is when you're running with settings.MultiThreadedMessageLoop = false, then they'll be the same thread).
        /// </summary>
        public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;
        /// <summary>
        /// Event handler for changes to the status message.
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI
        /// thread. It is unwise to block on this thread for any length of time as your browser will become unresponsive and/or hang.
        /// To access UI elements you'll need to Invoke/Dispatch onto the UI Thread.
        /// (The exception to this is when you're running with settings.MultiThreadedMessageLoop = false, then they'll be the same thread).
        /// </summary>
        public event EventHandler<StatusMessageEventArgs> StatusMessage;
        /// <summary>
        /// Event handler that will get called when the message that originates from CefSharp.PostMessage
        /// </summary>
        public event EventHandler<JavascriptMessageReceivedEventArgs> JavascriptMessageReceived;

        /// <summary>
        /// A flag that indicates whether the WebBrowser is initialized (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance is browser initialized; otherwise, <c>false</c>.</value>
        bool IChromiumWebBrowserBase.IsBrowserInitialized
        {
            get { return InternalIsBrowserInitialized(); }
        }

        void IWebBrowserInternal.SetCanExecuteJavascriptOnMainFrame(long frameId, bool canExecute)
        {
            //When loading pages of a different origin the frameId changes
            //For the first loading of a new origin the messages from the render process
            //Arrive in a different order than expected, the OnContextCreated message
            //arrives before the OnContextReleased, then the message for OnContextReleased
            //incorrectly overrides the value
            //https://github.com/cefsharp/CefSharp/issues/3021

            if (frameId > _canExecuteJavascriptInMainFrameId && !canExecute)
            {
                return;
            }

            _canExecuteJavascriptInMainFrameId = frameId;
            CanExecuteJavascriptInMainFrame = canExecute;
        }

        void IWebBrowserInternal.SetJavascriptMessageReceived(JavascriptMessageReceivedEventArgs args)
        {
            //Run the event on the ThreadPool (rather than the CEF Thread we are currently on).
            _ = Task.Run(() => JavascriptMessageReceived?.Invoke(this, args));
        }

        /// <summary>
        /// Handles the <see cref="E:FrameLoadStart" /> event.
        /// </summary>
        /// <param name="args">The <see cref="FrameLoadStartEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.OnFrameLoadStart(FrameLoadStartEventArgs args)
        {
            FrameLoadStart?.Invoke(this, args);
            _outofProcessHostRpc.NotifyFrameLoadStart(_id, args.Frame.Name, args.Url);
        }

        /// <summary>
        /// Handles the <see cref="E:FrameLoadEnd" /> event.
        /// </summary>
        /// <param name="args">The <see cref="FrameLoadEndEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.OnFrameLoadEnd(FrameLoadEndEventArgs args)
        {
            FrameLoadEnd?.Invoke(this, args);
            _outofProcessHostRpc.NotifyFrameLoadEnd(_id, args.Frame.Name, args.Url, args.HttpStatusCode);
        }

        /// <summary>
        /// Handles the <see cref="E:ConsoleMessage" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ConsoleMessageEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.OnConsoleMessage(ConsoleMessageEventArgs args)
        {
            ConsoleMessage?.Invoke(this, args);
        }

        /// <summary>
        /// Handles the <see cref="E:StatusMessage" /> event.
        /// </summary>
        /// <param name="args">The <see cref="StatusMessageEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.OnStatusMessage(StatusMessageEventArgs args)
        {
            StatusMessage?.Invoke(this, args);
        }

        /// <summary>
        /// Handles the <see cref="E:LoadError" /> event.
        /// </summary>
        /// <param name="args">The <see cref="LoadErrorEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.OnLoadError(LoadErrorEventArgs args)
        {
            LoadError?.Invoke(this, args);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has parent.
        /// </summary>
        /// <value><c>true</c> if this instance has parent; otherwise, <c>false</c>.</value>
        bool IWebBrowserInternal.HasParent { get; set; }

        /// <summary>
        /// Used by CefSharp.Puppeteer to associate a single DevToolsContext with a ChromiumWebBrowser instance.
        /// </summary>
        IDisposable IWebBrowserInternal.DevToolsContext { get; set; }

        /// <summary>
        /// Gets the browser adapter.
        /// </summary>
        /// <value>The browser adapter.</value>
        IBrowserAdapter IWebBrowserInternal.BrowserAdapter
        {
            get { return _managedCefBrowserAdapter; }
        }

        void IWebBrowserInternal.OnAfterBrowserCreated(IBrowser browser)
        {
            if (IsDisposed || browser.IsDisposed)
            {
                return;
            }

            BrowserCore = browser;
            Interlocked.Exchange(ref _browserInitialized, 1);

            var host = browser.GetHost();
            _outofProcessHostRpc.NotifyBrowserCreated(_id, host.GetWindowHandle());

            var observer = new CefSharpDevMessageObserver();
            observer.OnDevToolsAgentDetached((b) =>
            {
                _outofProcessHostRpc.NotifyDevToolsAgentDetached(_id);
            });
            observer.OnDevToolsMessage((b, m) =>
            {
                using var reader = new StreamReader(m);
                var msg = reader.ReadToEnd();

                _outofProcessHostRpc.NotifyDevToolsMessage(_id, msg);
            });

            _devtoolsMessageObserver = observer;

            _devtoolsRegistration = host.AddDevToolsMessageObserver(_devtoolsMessageObserver);

            var devToolsClient = browser.GetDevToolsClient();

            //TODO: Do we need perforamnce and Log enabled?
            var devToolsEnableTask = Task.WhenAll(
                devToolsClient.Page.EnableAsync(),
                devToolsClient.Page.SetLifecycleEventsEnabledAsync(true),
                devToolsClient.Runtime.EnableAsync(),
                devToolsClient.Network.EnableAsync(),
                devToolsClient.Performance.EnableAsync(),
                devToolsClient.Log.EnableAsync());

            _ = devToolsEnableTask.ContinueWith(t =>
            {
                ((IDisposable)devToolsClient).Dispose();

                _outofProcessHostRpc.NotifyDevToolsReady(_id);

            }, TaskScheduler.Default);            
        }

        /// <summary>
        /// Sets the loading state change.
        /// </summary>
        /// <param name="args">The <see cref="LoadingStateChangedEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.SetLoadingStateChange(LoadingStateChangedEventArgs args)
        {
            CanGoBack = args.CanGoBack;
            CanGoForward = args.CanGoForward;
            IsLoading = args.IsLoading;

            _outofProcessHostRpc.NotifyLoadingStateChange(_id, args.CanGoBack, args.CanGoForward, args.IsLoading);

            LoadingStateChanged?.Invoke(this, args);
        }

        /// <inheritdoc/>
        public void LoadUrl(string url) => BrowserCore.MainFrame.LoadUrl(url);

        /// <inheritdoc/>
        public Task<LoadUrlAsyncResponse> LoadUrlAsync(string url)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<WaitForNavigationAsyncResponse> WaitForNavigationAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<LoadUrlAsyncResponse> WaitForInitialLoadAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool TryGetBrowserCoreById(int browserId, out IBrowser browser)
        {
            var browserAdapter = _managedCefBrowserAdapter;

            if (IsDisposed || browserAdapter == null || browserAdapter.IsDisposed)
            {
                browser = null;

                return false;
            }

            browser = browserAdapter.GetBrowser(browserId);

            return browser != null;
        }

        /// <inheritdoc/>
        public async Task<DevTools.DOM.Rect> GetContentSizeAsync()
        {
            ThrowExceptionIfDisposed();
            ThrowExceptionIfBrowserNotInitialized();

            using (var devToolsClient = BrowserCore.GetDevToolsClient())
            {
                //Get the content size
                var layoutMetricsResponse = await devToolsClient.Page.GetLayoutMetricsAsync().ConfigureAwait(continueOnCapturedContext: false);

                return layoutMetricsResponse.CssContentSize;
            }
        }

        /// <summary>
        /// Sets the handler references to null.
        /// Where required also calls Dispose().
        /// </summary>
        private void FreeHandlersExceptLifeSpanAndFocus()
        {
            AudioHandler?.Dispose();
            AudioHandler = null;
            DialogHandler = null;
            FindHandler = null;
            RequestHandler = null;
            DisplayHandler = null;
            LoadHandler = null;
            KeyboardHandler = null;
            JsDialogHandler = null;
            DragHandler = null;
            DownloadHandler = null;
            MenuHandler = null;
            ResourceRequestHandlerFactory = null;
            RenderProcessMessageHandler = null;

            this.DisposeDevToolsContext();
        }

        /// <summary>
        /// Check is browser is initialized
        /// </summary>
        /// <returns>true if browser is initialized</returns>
        private bool InternalIsBrowserInitialized()
        {
            // Use CompareExchange to read the current value - if disposeCount is 1, we set it to 1, effectively a no-op
            // Volatile.Read would likely use a memory barrier which I believe is unnecessary in this scenario
            return Interlocked.CompareExchange(ref _browserInitialized, 0, 0) == 1;
        }

        /// <summary>
        /// Throw exception if browser not initialized.
        /// </summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        private void ThrowExceptionIfBrowserNotInitialized()
        {
            if (!InternalIsBrowserInitialized())
            {
                throw new Exception(BrowserNotInitializedExceptionErrorMessage);
            }
        }

        /// <summary>
        /// Throw exception if disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when a supplied object has been disposed.</exception>
        private void ThrowExceptionIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("ChromiumWebBrowser");
            }
        }

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

        /// <summary>
        /// A flag that indicates whether the WebBrowser is initialized (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance is browser initialized; otherwise, <c>false</c>.</value>
        public bool IsBrowserInitialized
        {
            get { return InternalIsBrowserInitialized(); }
        }
        /// <summary>
        /// A flag that indicates whether the control is currently loading one or more web pages (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance is loading; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool IsLoading { get; private set; }
        /// <summary>
        /// The text that will be displayed as a ToolTip
        /// </summary>
        /// <value>The tooltip text.</value>
        public string TooltipText { get; private set; }
        /// <summary>
        /// The address (URL) which the browser control is currently displaying.
        /// Will automatically be updated as the user navigates to another page (e.g. by clicking on a link).
        /// </summary>
        /// <value>The address.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public string Address { get; private set; }
        /// <summary>
        /// A flag that indicates whether the state of the control current supports the GoBack action (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance can go back; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool CanGoBack { get; private set; }
        /// <summary>
        /// A flag that indicates whether the state of the control currently supports the GoForward action (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance can go forward; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool CanGoForward { get; private set; }
        /// <summary>
        /// Gets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequestContext RequestContext { get; private set; }
        /// <summary>
        /// Implement <see cref="IAccessibilityHandler" /> to handle events related to accessibility.
        /// </summary>
        /// <value>The accessibility handler.</value>
        public IAccessibilityHandler AccessibilityHandler { get; set; }
        /// <summary>
        /// Occurs when the browser address changed.
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI
        /// thread. It is unwise to block on this thread for any length of time as your browser will become unresponsive and/or hang..
        /// To access UI elements you'll need to Invoke/Dispatch onto the UI Thread.
        /// (The exception to this is when you're running with settings.MultiThreadedMessageLoop = false, then they'll be the same thread).
        /// </summary>
        public event EventHandler<AddressChangedEventArgs> AddressChanged;
        /// <summary>
        /// Occurs when [title changed].
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI
        /// thread. It is unwise to block on this thread for any length of time as your browser will become unresponsive and/or hang..
        /// To access UI elements you'll need to Invoke/Dispatch onto the UI Thread.
        /// (The exception to this is when you're running with settings.MultiThreadedMessageLoop = false, then they'll be the same thread).
        /// </summary>
        public event EventHandler<TitleChangedEventArgs> TitleChanged;

        /// <summary>
        /// Create a new ChromiumWebBrowser. If you use <see cref="CefSharp.JavascriptBinding.JavascriptBindingSettings.LegacyBindingEnabled"/> = true then you must
        /// set <paramref name="automaticallyCreateBrowser"/> to false and call <see cref="CreateBrowser"/> after the objects are registered.
        /// The underlying Chromium Embedded Framework(CEF) Browser is created asynchronouly, to subscribe to the <see cref="BrowserInitialized"/> event it is recommended
        /// that you set <paramref name="automaticallyCreateBrowser"/> to false, subscribe to the event and then call <see cref="CreateBrowser(IWindowInfo, IBrowserSettings)"/>
        /// to ensure you are subscribe to the event before it's fired (Issue https://github.com/cefsharp/CefSharp/issues/3552).
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="address">Initial address (url) to load</param>
        /// <param name="browserSettings">The browser settings to use. If null, the default settings are used.</param>
        /// <param name="requestContext">See <see cref="RequestContext" /> for more details. Defaults to null</param>
        /// <param name="automaticallyCreateBrowser">automatically create the underlying Browser</param>
        /// <param name="onAfterBrowserCreated">
        /// Use as an alternative to the <see cref="BrowserInitialized"/> event. If the underlying Chromium Embedded Framework (CEF) browser is created successfully,
        /// this action is guranteed to be called after the browser created where as the <see cref="BrowserInitialized"/> event may be called before
        /// you have a chance to subscribe to the event as the CEF Browser is created async. (Issue https://github.com/cefsharp/CefSharp/issues/3552).
        /// </param>
        /// <exception cref="System.InvalidOperationException">Cef::Initialize() failed</exception>
        public OutOfProcessChromiumWebBrowser(IOutOfProcessHostRpc outOfProcessServer, int id, string address = "",
            IRequestContext requestContext = null)
        {
            _id = id;
            RequestContext = requestContext;
            _outofProcessHostRpc = outOfProcessServer;

            Cef.AddDisposable(this);
            Address = address;

            _managedCefBrowserAdapter = ManagedCefBrowserAdapter.Create(this, true);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="OutOfProcessChromiumWebBrowser"/> class.
        /// </summary>
        ~OutOfProcessChromiumWebBrowser()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="OutOfProcessChromiumWebBrowser"/> object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources for the <see cref="OutOfProcessChromiumWebBrowser"/>
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Attempt to move the disposeSignaled state from 0 to 1. If successful, we can be assured that
            // this thread is the first thread to do so, and can safely dispose of the object.
            if (Interlocked.CompareExchange(ref _disposeSignaled, 1, 0) != 0)
            {
                return;
            }

            if (disposing)
            {
                _devtoolsRegistration?.Dispose();
                _devtoolsRegistration = null;
                _devtoolsMessageObserver?.Dispose();
                _devtoolsMessageObserver = null;

                CanExecuteJavascriptInMainFrame = false;
                Interlocked.Exchange(ref _browserInitialized, 0);

                // Don't reference event listeners any longer:
                AddressChanged = null;
                ConsoleMessage = null;
                FrameLoadEnd = null;
                FrameLoadStart = null;
                LoadError = null;
                LoadingStateChanged = null;
                StatusMessage = null;
                TitleChanged = null;
                JavascriptMessageReceived = null;

                // Release reference to handlers, except LifeSpanHandler which is done after Disposing
                // ManagedCefBrowserAdapter otherwise the ILifeSpanHandler.DoClose will not be invoked.
                // We also leave FocusHandler and override with a NoFocusHandler implementation as
                // it so we can block taking Focus (we're dispoing afterall). Issue #3715
                FreeHandlersExceptLifeSpanAndFocus();

                FocusHandler = new NoFocusHandler();

                BrowserCore = null;

                _managedCefBrowserAdapter?.Dispose();
                _managedCefBrowserAdapter = null;

                // LifeSpanHandler is set to null after managedCefBrowserAdapter.Dispose so ILifeSpanHandler.DoClose
                // is called.
                LifeSpanHandler = null;
            }

            Cef.RemoveDisposable(this);
        }

        /// <summary>
        /// Create the underlying browser. The instance address, browser settings and request context will be used.
        /// </summary>
        /// <param name="windowInfo">Window information used when creating the browser</param>
        /// <param name="browserSettings">Browser initialization settings</param>
        /// <exception cref="System.Exception">An instance of the underlying browser has already been created, this method can only be called once.</exception>
        public void CreateBrowser(IWindowInfo windowInfo = null, IBrowserSettings browserSettings = null)
        {
            if (windowInfo.ParentWindowHandle != IntPtr.Zero)
            {
                monitorInfo.Init();
                MonitorInfo.GetMonitorInfoForWindowHandle(windowInfo.ParentWindowHandle, ref monitorInfo);
            }

            ((IRenderWebBrowser)this).HasParent = true;

            Debugger.Break();

            if (_browserCreated)
            {
                throw new Exception("An instance of the underlying browser has already been created, this method can only be called once.");
            }

            _browserCreated = true;

            if (browserSettings == null)
            {
                browserSettings = Core.ObjectFactory.CreateBrowserSettings(autoDispose: true);
            }

            //TODO: We need some sort of timeout and
            //if we use the same approach for WPF/WinForms then
            //we need to move the common code into the partial class
            GlobalContextInitialized.ExecuteOrEnqueue((success) =>
            {
                if (!success)
                {
                    return;
                }

                _managedCefBrowserAdapter.CreateBrowser(windowInfo, browserSettings, RequestContext, Address);

                //Dispose of BrowserSettings if we created it, if user created then they're responsible
                if (browserSettings.AutoDispose)
                {
                    browserSettings.Dispose();
                }
                browserSettings = null;

            });
        }

        /// <inheritdoc/>
        public void Load(string url)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The javascript object repository, one repository per ChromiumWebBrowser instance.
        /// </summary>
        public IJavascriptObjectRepository JavascriptObjectRepository
        {
            get { return _managedCefBrowserAdapter?.JavascriptObjectRepository; }
        }

        /// <summary>
        /// TODO: Improve focus
        /// Has Focus 
        /// </summary>
        /// <returns>returns false</returns>
        bool IChromiumWebBrowserBase.Focus()
        {
            ThrowExceptionIfDisposed();
            ThrowExceptionIfBrowserNotInitialized();

            BrowserCore.GetHost().SetFocus(true);

            return true;
        }

        /// <summary>
        /// Returns the current CEF Browser Instance
        /// </summary>
        /// <returns>browser instance or null</returns>
        public IBrowser GetBrowser()
        {
            ThrowExceptionIfDisposed();
            ThrowExceptionIfBrowserNotInitialized();

            return BrowserCore;
        }

        /// <summary>
        /// Sets the address.
        /// </summary>
        /// <param name="args">The <see cref="AddressChangedEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.SetAddress(AddressChangedEventArgs args)
        {
            Address = args.Address;

            AddressChanged?.Invoke(this, args);

            _outofProcessHostRpc.NotifyAddressChanged(_id, args.Address);
        }

        /// <summary>
        /// Sets the title.
        /// </summary>
        /// <param name="args">The <see cref="TitleChangedEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.SetTitle(TitleChangedEventArgs args)
        {
            TitleChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Sets the tooltip text.
        /// </summary>
        /// <param name="tooltipText">The tooltip text.</param>
        void IWebBrowserInternal.SetTooltipText(string tooltipText)
        {
            TooltipText = tooltipText;
        }

        /// <summary>
        /// The MonitorInfo based on the current hwnd
        /// </summary>
        private MonitorInfoEx monitorInfo;


        /// <summary>
        /// The dpi scale factor, if the browser has already been initialized
        /// you must manually call IBrowserHost.NotifyScreenInfoChanged for the
        /// browser to be notified of the change.
        /// </summary>
        public float DpiScaleFactor { get; set; } = 1;
        public System.Drawing.Point browserLocation { get; internal set; }

        public Rect viewRect { get; internal set; }
        IPermissionHandler IWebBrowser.PermissionHandler 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        /// <summary>
        /// Gets the ScreenInfo - currently used to get the DPI scale factor.
        /// </summary>
        /// <returns>ScreenInfo containing the current DPI scale factor</returns>
        ScreenInfo? IRenderWebBrowser.GetScreenInfo() => GetScreenInfo();

        /// <summary>
        /// Gets the ScreenInfo - currently used to get the DPI scale factor.
        /// </summary>
        /// <returns>ScreenInfo containing the current DPI scale factor</returns>
        protected virtual ScreenInfo? GetScreenInfo()
        {
            Rect rect = monitorInfo.Monitor;
            Rect availableRect = monitorInfo.WorkArea;

            if (DpiScaleFactor > 1.0)
            {
                rect = rect.ScaleByDpi(DpiScaleFactor);
                availableRect = availableRect.ScaleByDpi(DpiScaleFactor);
            }

            var screenInfo = new ScreenInfo
            {
                DeviceScaleFactor = DpiScaleFactor,
                Rect = rect,
                AvailableRect = availableRect
            };

            return screenInfo;
        }

        Rect IRenderWebBrowser.GetViewRect() => viewRect;

        bool IRenderWebBrowser.GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = browserLocation.X;
            screenY = browserLocation.Y;

            return true;
        }

        void IRenderWebBrowser.OnAcceleratedPaint(PaintElementType type, Rect dirtyRect, IntPtr sharedHandle)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnPaint(PaintElementType type, Structs.Rect dirtyRect, IntPtr buffer, int width, int height)
        {
            var dirtyRectCopy = new Copy.CefSharp.Structs.Rect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

            // PixelFormat PixelFormat = PixelFormats.Pbgra32;
            int BytesPerPixel = 32 / 8;
            int maximumPixels = 3600 * 2000;// width * height;
            int maximumNumberOfBytes = maximumPixels * BytesPerPixel;

            bool createNewBitmap = mappedFile == null || currentSize.Height != height || currentSize.Width != width;

            if (createNewBitmap)
            {
                //If the MemoryMappedFile is smaller than we need then create a larger one
                //If it's larger then we need then rather than going through the costly expense of
                //allocating a new one we'll just use the old one and only access the number of bytes we require.
                if (viewAccessor == null)
                {
                    //  ReleaseMemoryMappedView(ref mappedFile, ref viewAccessor);

                    renderFileName = $"0render_{_id}_{Guid.NewGuid()}";
                    mappedFile = MemoryMappedFile.CreateNew(renderFileName, maximumNumberOfBytes, MemoryMappedFileAccess.ReadWrite);

                    viewAccessor = mappedFile.CreateViewAccessor(0, maximumNumberOfBytes, MemoryMappedFileAccess.Write);
                }

                currentSize = new Size(width, height);
            }

            var usedBytes = width * height * BytesPerPixel;

            
            //{
            //    Buffer.MemoryCopy(
            //        viewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle().ToPointer(), buffer.ToPointer(),
            //        (uint)usedBytes,
            //        maximumNumberOfBytes);
            //}
            CopyMemory(viewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle(), buffer, (uint)usedBytes);
            viewAccessor.Flush();
            _outofProcessHostRpc.NotifyPaint(Id, type == PaintElementType.Popup, dirtyRectCopy, width, height, IntPtr.Zero, null, renderFileName);
        }

        string renderFileName;

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", ExactSpelling = true)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);


        protected void ReleaseMemoryMappedView(ref MemoryMappedFile mappedFile, ref MemoryMappedViewAccessor stream)
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }

            if (mappedFile != null)
            {
                mappedFile.Dispose();
                mappedFile = null;
            }
        }

        MemoryMappedViewAccessor viewAccessor;
        MemoryMappedFile mappedFile;
        Size currentSize;

        void IRenderWebBrowser.OnCursorChange(IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {
            // throw new NotImplementedException();
        }

        bool IRenderWebBrowser.StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.UpdateDragCursor(DragOperationsMask operation)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnPopupShow(bool show)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnPopupSize(Rect rect)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnImeCompositionRangeChanged(Structs.Range selectedRange, Rect[] characterBounds)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
            //throw new NotImplementedException();
        }
    }
}