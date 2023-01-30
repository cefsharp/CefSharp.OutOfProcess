// Copyright © 2022 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp.OutOfProcess.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Window = System.Windows.Window;
using System.Windows.Controls;
using CefSharp.Wpf.Rendering;
using System.Windows.Media;
using Application = System.Windows.Application;
using System.Threading.Tasks;
using CefSharp.Dom;
using CefSharp.OutOfProcess.WinForms;
using CefSharp.Wpf.Internals;
using CefSharp.OutOfProcess.Wpf.OffscreenHost.Internals;
using System.Collections.Generic;

namespace CefSharp.OutOfProcess.Wpf.OffscreenHost
{
    /// <summary>
    /// ChromiumWebBrowser is the WPF web browser control
    /// </summary>
    /// <seealso cref="Control" />
    /// <seealso cref="Wpf.HwndHost.IWpfWebBrowser" />
    /// based on https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf
    /// and https://stackoverflow.com/questions/6500336/custom-dwm-drawn-window-frame-flickers-on-resizing-if-the-window-contains-a-hwnd/17471534#17471534
    public class OffscreenChromiumWebBrowser : Control, IChromiumWebBrowserInternal, IRenderHandlerInternal
    {
        private const string BrowserNotInitializedExceptionErrorMessage =
            "The ChromiumWebBrowser instance creates the underlying Chromium Embedded Framework (CEF) browser instance in an async fashion. " +
            "The undelying CefBrowser instance is not yet initialized. Use the IsBrowserInitializedChanged event and check " +
            "the IsBrowserInitialized property to determine when the browser has been initialized.";

        private readonly Dictionary<string, string> _keyMapping = new Dictionary<string, string>() {
            { "Left", "ArrowLeft" },
            { "Right", "ArrowRight" },
            { "Up", "ArrowUp" },
            { "Down", "ArrowDown" },
            { "Tab", "Tab" },
        };

        /// <summary>
        /// The image that represents this browser instances
        /// </summary>
        private readonly Image _image = new Image();

        /// <summary>
        /// The popup image
        /// </summary>
        private readonly Image _popupImage = new Image();

        private readonly OutOfProcessHost _host;
        private IntPtr _browserHwnd = IntPtr.Zero;

        private readonly OutOfProcessConnectionTransport _devToolsContextConnectionTransport;
        private readonly IDevToolsContext _devToolsContext;
        private readonly int _id;
        private bool _devToolsReady;

        /// <summary>
        /// The ignore URI change
        /// </summary>
        private bool _ignoreUriChange;
        /// <summary>
        /// Initial address
        /// </summary>
        private readonly string _initialAddress;

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
        /// The HwndSource RootVisual (Window) - We store a reference
        /// to unsubscribe event handlers
        /// </summary>
        private Window _sourceWindow;

        /// <summary>
        /// This flag is set when the browser gets focus before the underlying CEF browser
        /// has been initialized.
        /// </summary>
        private bool _initialFocus;
        private DirectWritableBitmapRenderHandler _renderHandler;
        private DirectWritableBitmapRenderHandler _popupRenderHandler;
        private WindowState _previousWindowState;

        private Rect _popupRect;
        private bool _showPopup;

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value><see langword="true" /> if this instance is disposed; otherwise, <see langword="false" />.</value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeSignaled, 1, 1) == 1;

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
        public event EventHandler<PopupEventArgs> Popup;

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
        public event DependencyPropertyChangedEventHandler TitleChanged;

        /// <summary>
        /// Navigates to the previous page in the browser history. Will automatically be enabled/disabled depending on the
        /// browser state.
        /// </summary>
        /// <value>The back command.</value>
        public ICommand BackCommand { get; }

        /// <summary>
        /// Navigates to the next page in the browser history. Will automatically be enabled/disabled depending on the
        /// browser state.
        /// </summary>
        /// <value>The forward command.</value>
        public ICommand ForwardCommand { get; }

        /// <summary>
        /// Reloads the content of the current page. Will automatically be enabled/disabled depending on the browser state.
        /// </summary>
        /// <value>The reload command.</value>
        public ICommand ReloadCommand { get; }

        /// <summary>
        /// Command which cleans up the Resources used by the ChromiumWebBrowser
        /// </summary>
        /// <value>The cleanup command.</value>
        public ICommand CleanupCommand { get; }

        private static readonly DependencyPropertyKey sTitlePropertyKey;
        public static readonly DependencyProperty TitleProperty;


        static OffscreenChromiumWebBrowser()
        {
            sTitlePropertyKey = DependencyProperty.RegisterReadOnly(
                nameof(Title),
                typeof(string),
                typeof(OffscreenChromiumWebBrowser),
                new PropertyMetadata(OnTitleChanged));
            TitleProperty = sTitlePropertyKey.DependencyProperty;
        }

        public OffscreenChromiumWebBrowser(OutOfProcessHost host, string initialAddress = null)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _initialAddress = initialAddress;

            Focusable = true;
            FocusVisualStyle = null;
            IsTabStop = true;

            SizeChanged += OnSizeChanged;
            IsVisibleChanged += OnIsVisibleChanged;

            BackCommand = new DelegateCommand(() => _devToolsContext.GoBackAsync(), () => CanGoBack);
            ForwardCommand = new DelegateCommand(() => _devToolsContext.GoForwardAsync(), () => CanGoForward);
            ReloadCommand = new DelegateCommand(() => _devToolsContext.ReloadAsync(), () => !IsLoading);
            CleanupCommand = new DelegateCommand(Dispose);

            PresentationSource.AddSourceChangedHandler(this, PresentationSourceChangedHandler);

            UseLayoutRounding = true;

            var hwndHost = new WindowInteropHelper(Application.Current.MainWindow).Handle;

            _host.CreateBrowser(this, hwndHost, url: _initialAddress, out _id);
            _devToolsContextConnectionTransport = new OutOfProcessConnectionTransport(_id, _host);

            var connection = DevToolsConnection.Attach(_devToolsContextConnectionTransport);
            _devToolsContext = Dom.DevToolsContext.CreateForOutOfProcess(connection);
        }

        protected void ShowDevTools() => _host.ShowDevTools(_id);

        /// <inheritdoc/>
        int IChromiumWebBrowserInternal.Id => _id;

        /// <summary>
        /// DevToolsContext - provides communication with the underlying browser
        /// </summary>
        public IDevToolsContext DevToolsContext => _devToolsReady ? _devToolsContext : default;

        /// <inheritdoc/>
        public bool IsBrowserInitialized => _browserHwnd != IntPtr.Zero;

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            private set => SetValue(sTitlePropertyKey, value);
        }


        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.OnDevToolsMessage(string jsonMsg)
        {
            _devToolsContextConnectionTransport?.InvokeMessageReceived(jsonMsg);
        }

        /// <inheritdoc/>
        async void IChromiumWebBrowserInternal.OnDevToolsReady()
        {
            var ctx = _devToolsContext;
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

            await ((DevToolsContext)_devToolsContext).InvokeGetFrameTreeAsync();
            _devToolsReady = true;

            OnInitializeDevToolsContext(DevToolsContext);
            DevToolsContextAvailable?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public async void LoadUrl(string url)
        {
            await _host.LoadUrl(_id, url);
        }

        /// <inheritdoc/>
        public Task<Response> LoadUrlAsync(string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null) 
            => _devToolsContext.GoToAsync(url, timeout, waitUntil);

        /// <inheritdoc/>
        public Task<Response> GoBackAsync(NavigationOptions options = null) 
            => _devToolsContext.GoBackAsync(options);

        /// <inheritdoc/>
        public Task<Response> GoForwardAsync(NavigationOptions options = null) 
            => _devToolsContext.GoForwardAsync(options);

        protected virtual void OnInitializeDevToolsContext(IDevToolsContext context)
        {
        }

        private void PresentationSourceChangedHandler(object sender, SourceChangedEventArgs args)
        {
            if (args.NewSource != null)
            {
                var source = (HwndSource)args.NewSource;

                var matrix = source.CompositionTarget.TransformToDevice;

                if (source.RootVisual is Window window)
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
            else
            {
                if (args.OldSource?.RootVisual is Window window)
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
            //If the DPI changed then we need to resize.
            ResizeBrowser((int)ActualWidth, (int)ActualHeight);

            base.OnDpiChanged(oldDpi, newDpi);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeBrowser((int)e.NewSize.Width, (int)e.NewSize.Height);
        }

        ///<inheritdoc/>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            ////Debug.WriteLine($"OnGotKeyboardFocus from {e.OldFocus} to {e.NewFocus}. IsHandled: {e.Handled} Souce: {e.Source} OriginalSouce: {e.OriginalSource}");
            if (!e.Handled)
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
            ////Debug.WriteLine($"OnLostKeyboardFocus from {e.OldFocus} to {e.NewFocus}. IsHandled: {e.Handled} Souce: {e.Source} OriginalSouce: {e.OriginalSource}");

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

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources for the <see cref="OffscreenChromiumWebBrowser"/>
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        /// <remarks>
        /// This method cannot be inlined as the designer will attempt to load libcef.dll and will subsiquently throw an exception.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InternalDispose(bool disposing)
        {
            Interlocked.Exchange(ref _browserInitialized, 0);
            Interlocked.Exchange(ref _disposeSignaled, 1);

            if (disposing)
            {
                _renderHandler?.Dispose();
                _popupRenderHandler?.Dispose();

                _host.CloseBrowser(_id);

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
                });

                IsBrowserInitializedChanged = null;
                LoadingStateChanged = null;
                StatusMessage = null;

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
            });
        }

        /// <inheritdoc/>
        void IChromiumWebBrowserInternal.SetLoadingStateChange(bool canGoBack, bool canGoForward, bool isLoading)
        {
            UiThreadRunAsync(() =>
            {
                SetCurrentValue(IsLoadingProperty, isLoading);
            });

            LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(canGoBack, canGoForward, isLoading));
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
                    ResizeBrowser((int)ActualWidth, (int)ActualHeight);

                    OnIsBrowserInitializedChanged(false, true);

                    //To Minic the WPF behaviour this happens after OnIsBrowserInitializedChanged
                    IsBrowserInitializedChanged?.Invoke(this, EventArgs.Empty);

                    // Only call Load if initialAddress is null and Address is not empty
                    if (string.IsNullOrEmpty(_initialAddress) && !string.IsNullOrEmpty(Address))
                    {
                        LoadUrl(Address);
                    }
                }
            });

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
        public bool CanGoBack => (bool)GetValue(CanGoBackProperty);

        /// <summary>
        /// The can go back property
        /// </summary>
        public static readonly DependencyProperty CanGoBackProperty = DependencyProperty.Register(nameof(CanGoBack), typeof(bool), typeof(OffscreenChromiumWebBrowser));

        /// <summary>
        /// A flag that indicates whether the state of the control currently supports the GoForward action (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance can go forward; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool CanGoForward => (bool)GetValue(CanGoForwardProperty);

        /// <summary>
        /// The can go forward property
        /// </summary>
        public static readonly DependencyProperty CanGoForwardProperty = DependencyProperty.Register(nameof(CanGoForward), typeof(bool), typeof(OffscreenChromiumWebBrowser));

        /// <summary>
        /// The address (URL) which the browser control is currently displaying.
        /// Will automatically be updated as the user navigates to another page (e.g. by clicking on a link).
        /// </summary>
        /// <value>The address.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public string Address
        {
            get => (string)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }

        /// <summary>
        /// The address property
        /// </summary>
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register(nameof(Address), typeof(string), typeof(OffscreenChromiumWebBrowser),
                                        new UIPropertyMetadata(null, OnAddressChanged));

        /// <summary>
        /// Handles the <see cref="E:AddressChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAddressChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (OffscreenChromiumWebBrowser)sender;
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
        public bool IsLoading => (bool)GetValue(IsLoadingProperty);

        /// <summary>
        /// The is loading property
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(OffscreenChromiumWebBrowser), new PropertyMetadata(false));

        /// <summary>
        /// Event called after the underlying CEF browser instance has been created and
        /// when the <see cref="OffscreenChromiumWebBrowser"/> instance has been Disposed.
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
            }
        }

        /// <summary>
        /// The CleanupElement controls when the Browser will be Disposed.
        /// The <see cref="OffscreenChromiumWebBrowser"/> will be Disposed when <see cref="FrameworkElement.Unloaded"/> is called.
        /// Be aware that this Control is not usable anymore after it has been disposed.
        /// </summary>
        /// <value>The cleanup element.</value>
        public FrameworkElement CleanupElement
        {
            get => (FrameworkElement)GetValue(CleanupElementProperty);
            set => SetValue(CleanupElementProperty, value);
        }

        Dom.Frame[] IChromiumWebBrowser.Frames => throw new NotImplementedException();

        Dom.Frame IChromiumWebBrowser.MainFrame => throw new NotImplementedException();

        /// <summary>
        /// The cleanup element property
        /// </summary>
        public static readonly DependencyProperty CleanupElementProperty =
            DependencyProperty.Register(nameof(CleanupElement), typeof(FrameworkElement), typeof(OffscreenChromiumWebBrowser), new PropertyMetadata(null, OnCleanupElementChanged));

        /// <summary>
        /// Handles the <see cref="E:CleanupElementChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnCleanupElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (OffscreenChromiumWebBrowser)sender;
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
        /// Runs the specific Action on the Dispatcher in an async fashion
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        private async Task UiThreadRunAsync(Action action, DispatcherPriority priority = DispatcherPriority.DataBind)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else if (!Dispatcher.HasShutdownStarted)
            {
                await Dispatcher.InvokeAsync(action, priority);
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
                    NotifyHideBrowser();
                }
            }
        }

        /// <summary>
        /// Check is <see cref="_browserInitialized"/>
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
            NotifyResizeMoveBrowser(width, height);
        }

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            var window = (Window)sender;

            switch (window.WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                    {
                        if (_previousWindowState == WindowState.Minimized)
                        {
                            NotifyResizeMoveBrowser();
                        }
                        break;
                    }
                case WindowState.Minimized:
                    {
                        NotifyHideBrowser();
                        break;
                    }
            }

            _previousWindowState = window.WindowState;
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseWheel" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseWheelEventArgs" /> that contains the event data.</param>
        protected override async void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (DevToolsContext == null)
            {
                return;
            }

            if (!e.Handled)
            {
                var isShiftKeyDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                await DevToolsContext.Mouse.WheelAsync(isShiftKeyDown ? -e.Delta : 0, !isShiftKeyDown ? -e.Delta : 0);

                e.Handled = true;
            }

            base.OnMouseWheel(e);
        }

        protected void NotifyHideBrowser()
        {
            //// When minimized set the browser window size to 0x0 to reduce resource usage.
            //// https://github.com/chromiumembedded/cef/blob/c7701b8a6168f105f2c2d6b239ce3958da3e3f13/tests/cefclient/browser/browser_window_std_win.cc#L87
            if (InternalIsBrowserInitialized())
            {
                _host.NotifyMoveOrResizeStarted(_id, default);
            }
        }

        private void NotifyResizeMoveBrowser(int width, int height)
        {
            if (InternalIsBrowserInitialized())
            {
                var point = IsArrangeValid && width > 0 ? PointToScreen(default) : default;
                _host.NotifyMoveOrResizeStarted(_id, new Interface.Rect((int)point.X, (int)point.Y, width, height));
            }
        }

        protected void NotifyResizeMoveBrowser() => NotifyResizeMoveBrowser((int)ActualWidth, (int)ActualHeight);

        private void OnWindowLocationChanged(object sender, EventArgs e) => NotifyResizeMoveBrowser();

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

        private void Dispose() => Dispose(true);

        void IDisposable.Dispose() => Dispose(true);

        protected virtual void Dispose(bool isDisposing)
        {
            InternalDispose(isDisposing);
        }

        void IRenderHandlerInternal.OnPaint(bool isPopup, Interface.Rect directRect, int width, int height, string file)
        {
            const int defaultDpi = 96;
            const double scale = defaultDpi * 1.0;
            if (_renderHandler == null && !IsDisposed)
            {
                _renderHandler = new DirectWritableBitmapRenderHandler(scale, scale);
                _popupRenderHandler = new DirectWritableBitmapRenderHandler(scale, scale);
            }

            UiThreadRunAsync(() =>
            {
                if (isPopup)
                {
                    _popupRenderHandler?.OnPaint(directRect, width, height, _popupImage, file);
                }
                else
                {
                    _renderHandler?.OnPaint(directRect, width, height, _image, file);
                }

                InvalidateVisual();
            }, DispatcherPriority.Render);
        }

        void IRenderHandlerInternal.OnPopupShow(bool show)
        {
            _showPopup = show;
        }

        void IRenderHandlerInternal.OnPopupSize(Interface.Rect rect)
        {
            _popupRect = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_image?.Source != null)
            {
                drawingContext.DrawImage(_image.Source, new Rect(0, 0, (int)_image.Source.Width, (int)_image.Source.Height));
            }

            if (_showPopup && _popupImage != null && _image?.Source != null)
            {
                drawingContext.DrawImage(_popupImage.Source, _popupRect);
            }
        }

        #region wpf browser impl

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseMove" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override async void OnMouseMove(MouseEventArgs e)
        {
            if (DevToolsContext == null)
            {
                return;
            }

            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //If not, touch will cause duplicate events (mousemove and touchmove) and so does stylus.
            //Use e.StylusDevice == null to ensure only mouse.
            if (!e.Handled && _host != null && e.StylusDevice == null)
            {
                var point = e.GetPosition(this);

                await DevToolsContext.Mouse
                  .MoveAsync((decimal)point.X, (decimal)point.Y);
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseDown" /> attached event reaches an
        /// element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data.
        /// This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (DevToolsContext == null)
            {
                return;
            }

            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //If not, touch will cause duplicate events (mouseup and touchup) and so does stylus.
            //Use e.StylusDevice == null to ensure only mouse.
            if (e.StylusDevice == null)
            {
                Focus();

                OnMouseButton(e);

                //We should only need to capture the left button exiting the browser
                if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
                {
                    //Capture/Release Mouse to allow for scrolling outside bounds of browser control (#2258).
                    //Known issue when capturing and the device has a touch screen, to workaround this issue
                    //disable WPF StylusAndTouchSupport see for details https://github.com/dotnet/wpf/issues/1323#issuecomment-513870984
                    CaptureMouse();
                }
            }

            base.OnMouseDown(e);
        }


        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseUp" /> routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (DevToolsContext == null)
            {
                return;
            }

            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //If not, touch will cause duplicate events (mouseup and touchup) and so does stylus.
            //Use e.StylusDevice == null to ensure only mouse.
            if (e.StylusDevice == null)
            {
                OnMouseButton(e);

                if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
                {
                    //Release the mouse capture that we grabbed on mouse down.
                    //We won't get here if e.g. the right mouse button is pressed and released
                    //while the left is still held, but in that case the left mouse capture seems
                    //to be released implicitly (even without the left mouse SendMouseClickEvent in leave below)
                    //Use ReleaseMouseCapture over Mouse.Capture(null); as it has additional Mouse.Captured == this check
                    ReleaseMouseCapture();
                }
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseLeave" /> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override async void OnMouseLeave(MouseEventArgs e)
        {
            if (DevToolsContext == null)
            {
                return;
            }

            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //OnMouseLeave event from touch or stylus needn't to be handled.
            //Use e.StylusDevice == null to ensure only mouse.
            if (!e.Handled && _host != null && e.StylusDevice == null)
            {
                var point = e.GetPosition(this);

                await DevToolsContext.Mouse
                .MoveAsync((decimal)point.X, (decimal)point.Y);
            }

            base.OnMouseLeave(e);
        }

        void IChromiumWebBrowserInternal.SetStatusMessage(string msg)
        {
            StatusMessage?.Invoke(this, new StatusMessageEventArgs(msg));
        }

        void IChromiumWebBrowserInternal.SetTitle(string title) => UiThreadRunAsync(() => { Title = title; });

        /// <summary>
        /// Handles the <see cref="E:TitleChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((OffscreenChromiumWebBrowser)d).TitleChanged?.Invoke(d, e);

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.PreviewKeyDown" /> attached event reaches an
        /// element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs" /> that contains the event data.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!e.Handled && DevToolsContext != null)
            {
                HandleKeyPress(e);
            }

            base.OnPreviewKeyDown(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.PreviewKeyUp" /> attached event reaches an
        /// element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs" /> that contains the event data.</param>
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (!e.Handled && DevToolsContext != null)
            {
                HandleKeyPress(e);
            }

            base.OnPreviewKeyUp(e);
        }

        /// <summary>
        /// Handles the <see cref="E:PreviewTextInput" /> event.
        /// </summary>
        /// <param name="e">The <see cref="TextCompositionEventArgs"/> instance containing the event data.</param>
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (!e.Handled)
            {
                HandleTextInput(e);
            }

            base.OnPreviewTextInput(e);
        }

        private void HandleKeyPress(KeyEventArgs e)
        {
            var key = e.SystemKey == Key.None ? e.Key : e.SystemKey;
            if (DevToolsContext == null)
            {
                return;
            }

            // Hooking the Tab key like this makes the tab focusing in essence work like
            // KeyboardNavigation.TabNavigation="Cycle"; you will never be able to Tab out of the web browser control.
            // We also add the condition to allow ctrl+a to work when the web browser control is put inside listbox.
            // Prevent keyboard navigation using arrows and home and end keys
            if (key == Key.Tab || key == Key.Home || key == Key.End || key == Key.Up
                               || key == Key.Down || key == Key.Left || key == Key.Right || key == Key.Escape
                               || (key == Key.A && Keyboard.Modifiers == ModifierKeys.Control))
            {
                e.Handled = true;
            }

            if (!_keyMapping.ContainsKey(e.Key.ToString()))
            {
                return;
            }

            if (e.IsDown)
            {
                DevToolsContext.Keyboard.DownAsync(_keyMapping[e.Key.ToString()]);
            }
            else
            {
                DevToolsContext.Keyboard.UpAsync(_keyMapping[e.Key.ToString()]);
            }
        }

        private async void HandleTextInput(TextCompositionEventArgs e)
        {
            if (DevToolsContext == null)
            {
                return;
            }

            if (e.Text == "\b")
            {
                await DevToolsContext.Keyboard.PressAsync("Backspace");
            }
            else
            {
                await DevToolsContext.Keyboard.TypeAsync(e.Text);
            }

            e.Handled = true;
        }


        /// <summary>
        /// Handles the <see cref="E:MouseButton" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        private void OnMouseButton(MouseButtonEventArgs e)
        {
            if (e.Handled || !_devToolsReady)
            {
                return;
            }

            var mouseUp = e.ButtonState == MouseButtonState.Released;
            var point = e.GetPosition(this);

            //// Chromium only supports values of 1, 2 or 3.
            //// https://github.com/cefsharp/CefSharp/issues/3940
            //// Anything greater than 3 then we send click count of 1
            var clickCount = e.ClickCount;
            if (clickCount > 3)
            {
                clickCount = 1;
            }

            _host.SendMouseClickEvent(_id, (int)point.X, (int)point.Y, (MouseButtonType)e.ChangedButton, mouseUp, clickCount, e.GetModifiers());

            e.Handled = true;
        }

        #endregion
    }
}
