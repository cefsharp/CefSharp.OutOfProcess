// Copyright © 2022 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp.OutOfProcess.Internal;
using CefSharp.OutOfProcess;
using CefSharp.OutOfProcess.Wpf.HwndHost.Internals;
using CefSharp.Dom;
using PInvoke;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Window = System.Windows.Window;
using System.Collections.Generic;

namespace CefSharp.OutOfProcess.Wpf.HwndHost
{
    /// <summary>
    /// ChromiumWebBrowser is the WPF web browser control
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Control" />
    /// <seealso cref="CefSharp.Wpf.HwndHost.IWpfWebBrowser" />
    /// based on https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf
    /// and https://stackoverflow.com/questions/6500336/custom-dwm-drawn-window-frame-flickers-on-resizing-if-the-window-contains-a-hwnd/17471534#17471534
    public class ChromiumWebBrowser : System.Windows.Interop.HwndHost, IChromiumWebBrowserInternal
    {
        private const string BrowserNotInitializedExceptionErrorMessage =
            "The ChromiumWebBrowser instance creates the underlying Chromium Embedded Framework (CEF) browser instance in an async fashion. " +
            "The undelying CefBrowser instance is not yet initialized. Use the IsBrowserInitializedChanged event and check " +
            "the IsBrowserInitialized property to determine when the browser has been initialized.";

        private const int WS_CHILD = 0x40000000,
            WS_VISIBLE = 0x10000000,
            LBS_NOTIFY = 0x00000001,
            HOST_ID = 0x00000002,
            LISTBOX_ID = 0x00000001,
            WS_VSCROLL = 0x00200000,
            WS_BORDER = 0x00800000,
            WS_CLIPCHILDREN = 0x02000000;

        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(int dwExStyle,
                                              string lpszClassName,
                                              string lpszWindowName,
                                              int style,
                                              int x, int y,
                                              int width, int height,
                                              IntPtr hwndParent,
                                              IntPtr hMenu,
                                              IntPtr hInst,
                                              [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(HandleRef hWnd, int index, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int index, IntPtr dwNewLong);

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
        private readonly string _initialAddress;
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
        /// The HwndSource RootVisual (Window) - We store a reference
        /// to unsubscribe event handlers
        /// </summary>
        private Window _sourceWindow;

        /// <summary>
        /// Store the previous window state, used to determine if the
        /// Windows was previous <see cref="WindowState.Minimized"/>
        /// and resume rendering
        /// </summary>
        private WindowState _previousWindowState;

        /// <summary>
        /// This flag is set when the browser gets focus before the underlying CEF browser
        /// has been initialized.
        /// </summary>
        private bool _initialFocus;

        /// <summary>
        /// Contains the initial requests context preferences if any given in constructor.
        /// </summary>
        private readonly IDictionary<string, object> _requestContextPreferences;

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
        /// <summary>
        /// Prints the current browser contents.
        /// </summary>
        /// <value>The print command.</value>
        public ICommand PrintCommand { get; private set; }
        /// <summary>
        /// Increases the zoom level.
        /// </summary>
        /// <value>The zoom in command.</value>
        public ICommand ZoomInCommand { get; private set; }
        /// <summary>
        /// Decreases the zoom level.
        /// </summary>
        /// <value>The zoom out command.</value>
        public ICommand ZoomOutCommand { get; private set; }
        /// <summary>
        /// Resets the zoom level to the default. (100%)
        /// </summary>
        /// <value>The zoom reset command.</value>
        public ICommand ZoomResetCommand { get; private set; }
        /// <summary>
        /// Opens up a new program window (using the default text editor) where the source code of the currently displayed web
        /// page is shown.
        /// </summary>
        /// <value>The view source command.</value>
        public ICommand ViewSourceCommand { get; private set; }
        /// <summary>
        /// Command which cleans up the Resources used by the ChromiumWebBrowser
        /// </summary>
        /// <value>The cleanup command.</value>
        public ICommand CleanupCommand { get; private set; }
        /// <summary>
        /// Stops loading the current page.
        /// </summary>
        /// <value>The stop command.</value>
        public ICommand StopCommand { get; private set; }
        /// <summary>
        /// Cut selected text to the clipboard.
        /// </summary>
        /// <value>The cut command.</value>
        public ICommand CutCommand { get; private set; }
        /// <summary>
        /// Copy selected text to the clipboard.
        /// </summary>
        /// <value>The copy command.</value>
        public ICommand CopyCommand { get; private set; }
        /// <summary>
        /// Paste text from the clipboard.
        /// </summary>
        /// <value>The paste command.</value>
        public ICommand PasteCommand { get; private set; }
        /// <summary>
        /// Select all text.
        /// </summary>
        /// <value>The select all command.</value>
        public ICommand SelectAllCommand { get; private set; }
        /// <summary>
        /// Undo last action.
        /// </summary>
        /// <value>The undo command.</value>
        public ICommand UndoCommand { get; private set; }
        /// <summary>
        /// Redo last action.
        /// </summary>
        /// <value>The redo command.</value>
        public ICommand RedoCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromiumWebBrowser"/> instance.
        /// </summary>
        /// <param name="host">Out of process host</param>
        /// <param name="initialAddress">address to load initially</param>
        /// <param name="requestContextPreferences">requestContextPreferences to set</param>
        public ChromiumWebBrowser(OutOfProcessHost host, string initialAddress = null, IDictionary<string, object> requestContextPreferences = null)
        {
            if(host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            _requestContextPreferences = requestContextPreferences;
            _host = host;
            _initialAddress = initialAddress;

            Focusable = true;
            FocusVisualStyle = null;

            WebBrowser = this;

            SizeChanged += OnSizeChanged;
            IsVisibleChanged += OnIsVisibleChanged;

            BackCommand = new DelegateCommand(() => _devToolsContext.GoBackAsync(), () => CanGoBack);
            ForwardCommand = new DelegateCommand(() => _devToolsContext.GoForwardAsync(), () => CanGoForward);
            ReloadCommand = new DelegateCommand(() => _devToolsContext.ReloadAsync(), () => !IsLoading);
            //PrintCommand = new DelegateCommand(this.Print);
            //ZoomInCommand = new DelegateCommand(ZoomIn);
            //ZoomOutCommand = new DelegateCommand(ZoomOut);
            //ZoomResetCommand = new DelegateCommand(ZoomReset);
            //ViewSourceCommand = new DelegateCommand(this.ViewSource);
            CleanupCommand = new DelegateCommand(Dispose);
            //StopCommand = new DelegateCommand(this.Stop);
            //CutCommand = new DelegateCommand(this.Cut);
            //CopyCommand = new DelegateCommand(this.Copy);
            //PasteCommand = new DelegateCommand(this.Paste);
            //SelectAllCommand = new DelegateCommand(this.SelectAll);
            //UndoCommand = new DelegateCommand(this.Undo);
            //RedoCommand = new DelegateCommand(this.Redo);

            PresentationSource.AddSourceChangedHandler(this, PresentationSourceChangedHandler);

            UseLayoutRounding = true;
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
        public Frame[] Frames => _devToolsContext == null ? null : _devToolsContext.Frames;

        /// <inheritdoc/>
        public Frame MainFrame => _devToolsContext == null ? null : _devToolsContext.MainFrame;

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

        /// <summary>
        /// Set Request Context Preferences for this browser.
        /// </summary>
        /// <param name="preferences">The preferences.</param>
        public void SetRequestContextPreferences(IDictionary<string, object> preferences)
        {
            _host.SetRequestContextPreferences(this._id, preferences);
        }

        private void PresentationSourceChangedHandler(object sender, SourceChangedEventArgs args)
        {
            if (args.NewSource != null)
            {
                var source = (HwndSource)args.NewSource;

                var matrix = source.CompositionTarget.TransformToDevice;

                _dpiScale = matrix.M11;

                var window = source.RootVisual as Window;
                if (window != null)
                {
                    window.StateChanged += OnWindowStateChanged;
                    window.LocationChanged += OnWindowLocationChanged;
                    _sourceWindow = window;

                    if (CleanupElement == null)
                    {
                        CleanupElement = window;
                    }
                    else if (CleanupElement is Window parent)
                    {
                        //If the CleanupElement is a window then move it to the new Window
                        if (parent != window)
                        {
                            CleanupElement = window;
                        }
                    }
                }
            }
            else if (args.OldSource != null)
            {
                var window = args.OldSource.RootVisual as Window;
                if (window != null)
                {
                    window.StateChanged -= OnWindowStateChanged;
                    window.LocationChanged -= OnWindowLocationChanged;
                    _sourceWindow = null;
                }
            }
        }

        ///<inheritdoc/>
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            _dpiScale = newDpi.DpiScaleX;

            //If the DPI changed then we need to resize.
            ResizeBrowser((int)ActualWidth, (int)ActualHeight);

            base.OnDpiChanged(oldDpi, newDpi);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeBrowser((int)e.NewSize.Width, (int)e.NewSize.Height);
        }

        ///<inheritdoc/>
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            if (_hwndHost == IntPtr.Zero)
            {
                _hwndHost = CreateWindowEx(0, "static", "",
                            WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN,
                            0, 0,
                            (int)ActualWidth, (int)ActualHeight,
                            hwndParent.Handle,
                            (IntPtr)HOST_ID,
                            IntPtr.Zero,
                            0);
            }

            _host.CreateBrowser(this, _hwndHost, url: _initialAddress, out _id, _requestContextPreferences);

            _devToolsContextConnectionTransport = new OutOfProcessConnectionTransport(_id, _host);

            var connection = DevToolsConnection.Attach(_devToolsContextConnectionTransport);
            _devToolsContext = Dom.DevToolsContext.CreateForOutOfProcess(connection);

            return new HandleRef(null, _hwndHost);
        }

        ///<inheritdoc/>
        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
        }

        ///<inheritdoc/>
        protected override bool TabIntoCore(TraversalRequest request)
        {
            if(InternalIsBrowserInitialized())
            {
                _host.SetFocus(_id, true);

                return true;
            }

            return base.TabIntoCore(request);
        }

        ///<inheritdoc/>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            if(!e.Handled)
            {
                if (InternalIsBrowserInitialized())
                {
                    _host.SetFocus(_id, true);
                }
                else
                {
                    _initialFocus = true;
                }
            }

            base.OnGotKeyboardFocus(e);
        }

        ///<inheritdoc/>
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            if (!e.Handled)
            {
                if (InternalIsBrowserInitialized())
                {
                    _host.SetFocus(_id, false);
                }
                else
                {
                    _initialFocus = false;
                }
            }

            base.OnLostKeyboardFocus(e);
        }


        ///<inheritdoc/>
        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_SETFOCUS = 0x0007;
            const int WM_MOUSEACTIVATE = 0x0021;
            switch (msg)
            {
                case WM_SETFOCUS:
                case WM_MOUSEACTIVATE:
                {
                    if(InternalIsBrowserInitialized())
                    {
                        
                        _host.SetFocus(_id, true);

                        handled = true;

                        return IntPtr.Zero;
                    }
                    break;
                }
            }
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        /// <summary>
        /// If not in design mode; Releases unmanaged and - optionally - managed resources for the <see cref="ChromiumWebBrowser"/>
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            // Attempt to move the disposeSignaled state from 0 to 1. If successful, we can be assured that
            // this thread is the first thread to do so, and can safely dispose of the object.
            if (Interlocked.CompareExchange(ref _disposeSignaled, 1, 0) != 0)
            {
                return;
            }

            if (!DesignMode)
            {
                InternalDispose(disposing);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources for the <see cref="ChromiumWebBrowser"/>
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        /// <remarks>
        /// This method cannot be inlined as the designer will attempt to load libcef.dll and will subsiquently throw an exception.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InternalDispose(bool disposing)
        {
            Interlocked.Exchange(ref _browserInitialized, 0);

            if (disposing)
            {
                SizeChanged -= OnSizeChanged;
                IsVisibleChanged -= OnIsVisibleChanged;

                PresentationSource.RemoveSourceChangedHandler(this, PresentationSourceChangedHandler);
                // Release window event listeners if PresentationSourceChangedHandler event wasn't
                // fired before Dispose
                if (_sourceWindow != null)
                {
                    _sourceWindow.StateChanged -= OnWindowStateChanged;
                    _sourceWindow.LocationChanged -= OnWindowLocationChanged;
                    _sourceWindow = null;
                }


                UiThreadRunAsync(() =>
                {
                    OnIsBrowserInitializedChanged(true, false);

                    //To Minic the WPF behaviour this happens after OnIsBrowserInitializedChanged
                    IsBrowserInitializedChanged?.Invoke(this, EventArgs.Empty);

                    WebBrowser = null;
                });

                // Don't maintain a reference to event listeners anylonger:
                //ConsoleMessage = null;
                //FrameLoadEnd = null;
                //FrameLoadStart = null;
                IsBrowserInitializedChanged = null;
                //LoadError = null;
                LoadingStateChanged = null;
                StatusMessage = null;
                TitleChanged = null;

                if (CleanupElement != null)
                {
                    CleanupElement.Unloaded -= OnCleanupElementUnloaded;
                }
            }
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.SetAddress(string address)
        {
            UiThreadRunAsync(() =>
            {
                _ignoreUriChange = true;
                SetCurrentValue(AddressProperty, address);
                _ignoreUriChange = false;

                // The tooltip should obviously also be reset (and hidden) when the address changes.
                SetCurrentValue(TooltipTextProperty, null);
            });
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.SetLoadingStateChange(bool canGoBack, bool canGoForward, bool isLoading)
        {
            UiThreadRunAsync(() =>
            {
                SetCurrentValue(CanGoBackProperty, canGoBack);
                SetCurrentValue(CanGoForwardProperty, canGoForward);
                SetCurrentValue(IsLoadingProperty, isLoading);

                ((DelegateCommand)BackCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)ForwardCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)ReloadCommand).RaiseCanExecuteChanged();
            });

            LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(canGoBack, canGoForward, isLoading));
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.SetTitle(string title)
        {
            UiThreadRunAsync(() => SetCurrentValue(TitleProperty, title));
        }

        /// <summary>
        /// Sets the tooltip text.
        /// </summary>
        /// <param name="tooltipText">The tooltip text.</param>
        //void IWebBrowserInternal.SetTooltipText(string tooltipText)
        //{
        //    UiThreadRunAsync(() => SetCurrentValue(TooltipTextProperty, tooltipText));
        //}

        /// <summary>
        /// Handles the <see cref="E:ConsoleMessage" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ConsoleMessageEventArgs"/> instance containing the event data.</param>
        //void IWebBrowserInternal.OnConsoleMessage(ConsoleMessageEventArgs args)
        //{
        //    ConsoleMessage?.Invoke(this, args);
        //}

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

            UiThreadRunAsync(() =>
            {
                if (!IsDisposed)
                {
                    OnIsBrowserInitializedChanged(false, true);
                    //To Minic the WPF behaviour this happens after OnIsBrowserInitializedChanged
                    IsBrowserInitializedChanged?.Invoke(this, EventArgs.Empty);
                }
            });

            ResizeBrowser((int)ActualWidth, (int)ActualHeight);

            if (_initialFocus)
            {
                _host.SetFocus(_id, true);
            }
        }

        /// <summary>
        /// A flag that indicates whether the state of the control current supports the GoBack action (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance can go back; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool CanGoBack
        {
            get { return (bool)GetValue(CanGoBackProperty); }
        }

        /// <summary>
        /// The can go back property
        /// </summary>
        public static DependencyProperty CanGoBackProperty = DependencyProperty.Register(nameof(CanGoBack), typeof(bool), typeof(ChromiumWebBrowser));

        /// <summary>
        /// A flag that indicates whether the state of the control currently supports the GoForward action (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance can go forward; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool CanGoForward
        {
            get { return (bool)GetValue(CanGoForwardProperty); }
        }

        /// <summary>
        /// The can go forward property
        /// </summary>
        public static DependencyProperty CanGoForwardProperty = DependencyProperty.Register(nameof(CanGoForward), typeof(bool), typeof(ChromiumWebBrowser));

        /// <summary>
        /// The address (URL) which the browser control is currently displaying.
        /// Will automatically be updated as the user navigates to another page (e.g. by clicking on a link).
        /// </summary>
        /// <value>The address.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        /// <summary>
        /// The address property
        /// </summary>
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register(nameof(Address), typeof(string), typeof(ChromiumWebBrowser),
                                        new UIPropertyMetadata(null, OnAddressChanged));

        /// <summary>
        /// Handles the <see cref="E:AddressChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAddressChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (ChromiumWebBrowser)sender;
            var oldValue = (string)args.OldValue;
            var newValue = (string)args.NewValue;

            owner.OnAddressChanged(oldValue, newValue);
        }

        /// <summary>
        /// Called when [address changed].
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnAddressChanged(string oldValue, string newValue)
        {
            if (_ignoreUriChange || newValue == null || !InternalIsBrowserInitialized())
            {
                return;
            }

            LoadUrl(newValue);
        }

        /// <summary>
        /// A flag that indicates whether the control is currently loading one or more web pages (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance is loading; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
        }

        /// <summary>
        /// The is loading property
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(ChromiumWebBrowser), new PropertyMetadata(false));

        /// <summary>
        /// Event called after the underlying CEF browser instance has been created and
        /// when the <see cref="ChromiumWebBrowser"/> instance has been Disposed.
        /// <see cref="IsBrowserInitialized"/> will be true when the underlying CEF Browser
        /// has been created and false when the browser is being Disposed.
        /// </summary>
        public event EventHandler IsBrowserInitializedChanged;

        /// <summary>
        /// Called when [is browser initialized changed].
        /// </summary>
        /// <param name="oldValue">if set to <c>true</c> [old value].</param>
        /// <param name="newValue">if set to <c>true</c> [new value].</param>
        protected virtual void OnIsBrowserInitializedChanged(bool oldValue, bool newValue)
        {
            if (newValue && !IsDisposed)
            {
                //var task = this.GetZoomLevelAsync();
                //task.ContinueWith(previous =>
                //{
                //    if (previous.Status == TaskStatus.RanToCompletion)
                //    {
                //        UiThreadRunAsync(() =>
                //        {
                //            if (!IsDisposed)
                //            {
                //                SetCurrentValue(ZoomLevelProperty, previous.Result);
                //            }
                //        });
                //    }
                //    else
                //    {
                //        throw new InvalidOperationException("Unexpected failure of calling CEF->GetZoomLevelAsync", previous.Exception);
                //    }
                //}, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        /// <summary>
        /// The title of the web page being currently displayed.
        /// </summary>
        /// <value>The title.</value>
        /// <remarks>This property is implemented as a Dependency Property and fully supports data binding.</remarks>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// The title property
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ChromiumWebBrowser), new PropertyMetadata(null, OnTitleChanged));

        /// <summary>
        /// Event handler that will get called when the browser title changes
        /// </summary>
        public event DependencyPropertyChangedEventHandler TitleChanged;

        /// <summary>
        /// Handles the <see cref="E:TitleChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = (ChromiumWebBrowser)d;

            owner.TitleChanged?.Invoke(owner, e);
        }

        /// <summary>
        /// The zoom level at which the browser control is currently displaying.
        /// Can be set to 0 to clear the zoom level (resets to default zoom level).
        /// </summary>
        /// <value>The zoom level.</value>
        public double ZoomLevel
        {
            get { return (double)GetValue(ZoomLevelProperty); }
            set { SetValue(ZoomLevelProperty, value); }
        }

        /// <summary>
        /// The zoom level property
        /// </summary>
        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register(nameof(ZoomLevel), typeof(double), typeof(ChromiumWebBrowser),
                                        new UIPropertyMetadata(0d, OnZoomLevelChanged));

        /// <summary>
        /// Handles the <see cref="E:ZoomLevelChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnZoomLevelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (ChromiumWebBrowser)sender;
            var oldValue = (double)args.OldValue;
            var newValue = (double)args.NewValue;

            owner.OnZoomLevelChanged(oldValue, newValue);
        }

        /// <summary>
        /// Called when [zoom level changed].
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnZoomLevelChanged(double oldValue, double newValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Specifies the amount used to increase/decrease to ZoomLevel by
        /// By Default this value is 0.10
        /// </summary>
        /// <value>The zoom level increment.</value>
        public double ZoomLevelIncrement
        {
            get { return (double)GetValue(ZoomLevelIncrementProperty); }
            set { SetValue(ZoomLevelIncrementProperty, value); }
        }

        /// <summary>
        /// The zoom level increment property
        /// </summary>
        public static readonly DependencyProperty ZoomLevelIncrementProperty =
            DependencyProperty.Register(nameof(ZoomLevelIncrement), typeof(double), typeof(ChromiumWebBrowser), new PropertyMetadata(0.10));

        /// <summary>
        /// The CleanupElement controls when the Browser will be Disposed.
        /// The <see cref="ChromiumWebBrowser"/> will be Disposed when <see cref="FrameworkElement.Unloaded"/> is called.
        /// Be aware that this Control is not usable anymore after it has been disposed.
        /// </summary>
        /// <value>The cleanup element.</value>
        public FrameworkElement CleanupElement
        {
            get { return (FrameworkElement)GetValue(CleanupElementProperty); }
            set { SetValue(CleanupElementProperty, value); }
        }

        /// <summary>
        /// The cleanup element property
        /// </summary>
        public static readonly DependencyProperty CleanupElementProperty =
            DependencyProperty.Register(nameof(CleanupElement), typeof(FrameworkElement), typeof(ChromiumWebBrowser), new PropertyMetadata(null, OnCleanupElementChanged));

        /// <summary>
        /// Handles the <see cref="E:CleanupElementChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnCleanupElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (ChromiumWebBrowser)sender;
            var oldValue = (FrameworkElement)args.OldValue;
            var newValue = (FrameworkElement)args.NewValue;

            owner.OnCleanupElementChanged(oldValue, newValue);
        }

        /// <summary>
        /// Called when [cleanup element changed].
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnCleanupElementChanged(FrameworkElement oldValue, FrameworkElement newValue)
        {
            if (oldValue != null)
            {
                oldValue.Unloaded -= OnCleanupElementUnloaded;
            }

            if (newValue != null)
            {
                newValue.Unloaded += OnCleanupElementUnloaded;
            }
        }

        /// <summary>
        /// Handles the <see cref="E:CleanupElementUnloaded" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void OnCleanupElementUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        /// <summary>
        /// The text that will be displayed as a ToolTip
        /// </summary>
        /// <value>The tooltip text.</value>
        public string TooltipText
        {
            get { return (string)GetValue(TooltipTextProperty); }
        }

        /// <summary>
        /// The tooltip text property
        /// </summary>
        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(ChromiumWebBrowser));

        /// <summary>
        /// Gets or sets the WebBrowser.
        /// </summary>
        /// <value>The WebBrowser.</value>
        public IChromiumWebBrowser WebBrowser
        {
            get { return (IChromiumWebBrowser)GetValue(WebBrowserProperty); }
            set { SetValue(WebBrowserProperty, value); }
        }

        /// <summary>
        /// The WebBrowser property
        /// </summary>
        public static readonly DependencyProperty WebBrowserProperty =
            DependencyProperty.Register(nameof(WebBrowser), typeof(IChromiumWebBrowser), typeof(ChromiumWebBrowser), new UIPropertyMetadata(defaultValue: null));

        /// <summary>
        /// Runs the specific Action on the Dispatcher in an async fashion
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        private void UiThreadRunAsync(Action action, DispatcherPriority priority = DispatcherPriority.DataBind)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else if (!Dispatcher.HasShutdownStarted)
            {
                _ = Dispatcher.InvokeAsync(action, priority);
            }
        }

        /// <summary>
        /// Handles the <see cref="E:IsVisibleChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var isVisible = (bool)args.NewValue;

            if (InternalIsBrowserInitialized())
            {
                if (isVisible)
                {
                    ResizeBrowser((int)ActualWidth, (int)ActualHeight);
                }
                else
                {
                    //Hide browser
                    ResizeBrowser(0, 0);
                }
            }
        }

        /// <summary>
        /// Zooms the browser in.
        /// </summary>
        private void ZoomIn()
        {
            UiThreadRunAsync(() =>
            {
                ZoomLevel = ZoomLevel + ZoomLevelIncrement;
            });
        }

        /// <summary>
        /// Zooms the browser out.
        /// </summary>
        private void ZoomOut()
        {
            UiThreadRunAsync(() =>
            {
                ZoomLevel = ZoomLevel - ZoomLevelIncrement;
            });
        }

        /// <summary>
        /// Reset the browser's zoom level to default.
        /// </summary>
        private void ZoomReset()
        {
            UiThreadRunAsync(() =>
            {
                ZoomLevel = 0;
            });
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

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            var window = (Window)sender;

            switch (window.WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                {
                    if (_previousWindowState == WindowState.Minimized && InternalIsBrowserInitialized())
                    {
                        ResizeBrowser((int)ActualWidth, (int)ActualHeight);
                    }
                    break;
                }
                case WindowState.Minimized:
                {
                    if (InternalIsBrowserInitialized())
                    {
                        //Set the browser size to 0,0 to reduce CPU usage
                        ResizeBrowser(0, 0);
                    }
                    break;
                }
            }

            _previousWindowState = window.WindowState;
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            if (InternalIsBrowserInitialized())
            {
                _host.NotifyMoveOrResizeStarted(_id);
            }
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
                throw new ObjectDisposedException("browser", "Browser has been disposed");
            }
        }
    }
}
