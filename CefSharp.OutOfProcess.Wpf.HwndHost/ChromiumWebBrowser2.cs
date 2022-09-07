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
using CefSharp.Wpf.Internals;
using Copy.CefSharp;
using Application = System.Windows.Application;
using CefSharp.Wpf;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess.Wpf.HwndHost
{
    /// <summary>
    /// ChromiumWebBrowser is the WPF web browser control
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Control" />
    /// <seealso cref="CefSharp.Wpf.HwndHost.IWpfWebBrowser" />
    /// based on https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf
    /// and https://stackoverflow.com/questions/6500336/custom-dwm-drawn-window-frame-flickers-on-resizing-if-the-window-contains-a-hwnd/17471534#17471534
    public class ChromiumWebBrowser2 : Control, IChromiumWebBrowserInternal
    {
        private const string BrowserNotInitializedExceptionErrorMessage =
            "The ChromiumWebBrowser instance creates the underlying Chromium Embedded Framework (CEF) browser instance in an async fashion. " +
            "The undelying CefBrowser instance is not yet initialized. Use the IsBrowserInitializedChanged event and check " +
            "the IsBrowserInitialized property to determine when the browser has been initialized.";

        /// <summary>
        /// The image that represents this browser instances
        /// </summary>
        private Image image = new Image();

        /// <summary>
        /// The popup image
        /// </summary>
        private Image popupImage;

        private OutOfProcessHost _host;
        private IntPtr _browserHwnd = IntPtr.Zero;
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
        /// This flag is set when the browser gets focus before the underlying CEF browser
        /// has been initialized.
        /// </summary>
        private bool _initialFocus;
        private DirectWritableBitmapRenderHandler _renderHandler;

        /// <summary>
        /// Activates browser upon creation, the default value is false. Prior to version 73
        /// the default behaviour was to activate browser on creation (Equivilent of setting this property to true).
        /// To restore this behaviour set this value to true immediately after you create the <see cref="ChromiumWebBrowser2"/> instance.
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
        public event EventHandler JavaScriptLoad;

        /// <inheritdoc/>
        public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;
        /// <inheritdoc/>
        public event EventHandler<StatusMessageEventArgs> StatusMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromiumWebBrowser2"/> instance.
        /// </summary>
        /// <param name="host">Out of process host</param>
        /// <param name="initialAddress">address to load initially</param>
        public ChromiumWebBrowser2(OutOfProcessHost host, string initialAddress = null)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            _host = host;
            _initialAddress = initialAddress;

            Focusable = true;
            FocusVisualStyle = null;

            SizeChanged += OnSizeChanged;
            IsVisibleChanged += OnIsVisibleChanged;

            PresentationSource.AddSourceChangedHandler(this, PresentationSourceChangedHandler);

            UseLayoutRounding = true;

            {// copied from method build window core
                _hwndHost = new WindowInteropHelper(Application.Current.MainWindow).Handle; // new line
                _host.CreateBrowser(this, _hwndHost, url: _initialAddress, out _id);
            }
        }


        /// <inheritdoc/>
        int IChromiumWebBrowserInternal.Id
        {
            get { return _id; }
        }

        /// <inheritdoc/>
        public bool IsBrowserInitialized => _browserHwnd != IntPtr.Zero;


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
                    //   window.StateChanged += OnWindowStateChanged;
                    //   window.LocationChanged += OnWindowLocationChanged;
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
                    //  window.StateChanged -= OnWindowStateChanged;
                    //  window.LocationChanged -= OnWindowLocationChanged;
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
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
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
        /// Releases unmanaged and - optionally - managed resources for the <see cref="ChromiumWebBrowser2"/>
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
                _renderHandler?.Dispose();

                SizeChanged -= OnSizeChanged;
                IsVisibleChanged -= OnIsVisibleChanged;

                PresentationSource.RemoveSourceChangedHandler(this, PresentationSourceChangedHandler);
                // Release window event listeners if PresentationSourceChangedHandler event wasn't
                // fired before Dispose
                if (_sourceWindow != null)
                {
                    //  _sourceWindow.StateChanged -= OnWindowStateChanged;
                    //  _sourceWindow.LocationChanged -= OnWindowLocationChanged;
                    _sourceWindow = null;
                }


                UiThreadRunAsync(() =>
                {
                    OnIsBrowserInitializedChanged(true, false);

                    //To Minic the WPF behaviour this happens after OnIsBrowserInitializedChanged
                    IsBrowserInitializedChanged?.Invoke(this, EventArgs.Empty);
                });

                // Don't maintain a reference to event listeners anylonger:
                //ConsoleMessage = null;
                //FrameLoadEnd = null;
                //FrameLoadStart = null;
                IsBrowserInitializedChanged = null;
                //LoadError = null;
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
                }
            });

            if (_initialFocus)
            {
                _host.SetFocus(_id, true);
            }
        }

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
            DependencyProperty.Register(nameof(Address), typeof(string), typeof(ChromiumWebBrowser2),
                                        new UIPropertyMetadata(null, OnAddressChanged));

        /// <summary>
        /// Handles the <see cref="E:AddressChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAddressChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (ChromiumWebBrowser2)sender;
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

            _host.LoadUrl(_id, newValue);
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
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(ChromiumWebBrowser2), new PropertyMetadata(false));

        /// <summary>
        /// Event called after the underlying CEF browser instance has been created and
        /// when the <see cref="ChromiumWebBrowser2"/> instance has been Disposed.
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
        /// The <see cref="ChromiumWebBrowser2"/> will be Disposed when <see cref="FrameworkElement.Unloaded"/> is called.
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
            DependencyProperty.Register(nameof(CleanupElement), typeof(FrameworkElement), typeof(ChromiumWebBrowser2), new PropertyMetadata(null, OnCleanupElementChanged));

        /// <summary>
        /// Handles the <see cref="E:CleanupElementChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnCleanupElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (ChromiumWebBrowser2)sender;
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
                    //   HideInternal();
                }
                else
                {
                    // ShowInternal(width, height);
                }
            }

            var point = PointToScreen(new Point());
            _host.NotifyMoveOrResizeStarted(_id, width, height, (int)point.X, (int)point.Y);
        }

        /////// <summary>
        /////// When minimized set the browser window size to 0x0 to reduce resource usage.
        /////// https://github.com/chromiumembedded/cef/blob/c7701b8a6168f105f2c2d6b239ce3958da3e3f13/tests/cefclient/browser/browser_window_std_win.cc#L87
        /////// </summary>
        ////internal virtual void HideInternal()
        ////{
        ////    ////if (_browserHwnd != IntPtr.Zero)
        ////    ////{
        ////    ////    User32.SetWindowPos(_browserHwnd, IntPtr.Zero, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOACTIVATE);
        ////    ////}
        ////}

        /////// <summary>
        /////// Show the browser (called after previous minimised)
        /////// </summary>
        ////internal virtual void ShowInternal(int width, int height)
        ////{
        ////    if (_browserHwnd != IntPtr.Zero)
        ////    {
        ////      //  User32.SetWindowPos(_browserHwnd, IntPtr.Zero, 0, 0, width, height, User32.SetWindowPosFlags.SWP_NOZORDER);
        ////    }
        ////}

        ////private void OnWindowStateChanged(object sender, EventArgs e)
        ////{
        ////    var window = (Window)sender;

        ////    switch (window.WindowState)
        ////    {
        ////        case WindowState.Normal:
        ////        case WindowState.Maximized:
        ////        {
        ////            if (_previousWindowState == WindowState.Minimized && InternalIsBrowserInitialized())
        ////            {
        ////                ResizeBrowser((int)ActualWidth, (int)ActualHeight);
        ////            }
        ////            break;
        ////        }
        ////        case WindowState.Minimized:
        ////        {
        ////            if (InternalIsBrowserInitialized())
        ////            {
        ////                //Set the browser size to 0,0 to reduce CPU usage
        ////                ResizeBrowser(0, 0);
        ////            }
        ////            break;
        ////        }
        ////    }

        ////    _previousWindowState = window.WindowState;
        ////}

        ////private void OnWindowLocationChanged(object sender, EventArgs e)
        ////{
        ////    if (InternalIsBrowserInitialized())
        ////    {
        ////        _host.NotifyMoveOrResizeStarted(_id);
        ////    }
        ////}

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

        void Dispose()
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }

        void IChromiumWebBrowserInternal.OnPaint(bool isPopup, Copy.CefSharp.Structs.Rect directRect, int width, int height, IntPtr buffer, byte[] data)
        {
            const int DefaultDpi = 96;
            var scale = DefaultDpi * 1.0;
            if (_renderHandler == null)
            {
                _renderHandler = new DirectWritableBitmapRenderHandler(scale, scale);
            }

            UiThreadRunAsync(() =>
            {
                _renderHandler.OnPaint(isPopup, directRect, buffer, data, width, height, this.image);
                InvalidateVisual();
            }, DispatcherPriority.Render);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (image != null)
            {
                drawingContext.DrawImage(image.Source, new Rect(0, 0, ActualWidth, ActualHeight));
            }
        }

        #region wpf browser impl

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseMove" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //If not, touch will cause duplicate events (mousemove and touchmove) and so does stylus.
            //Use e.StylusDevice == null to ensure only mouse.
            if (!e.Handled && _host != null && e.StylusDevice == null)
            {
                var point = e.GetPosition(this);
                var modifiers = e.GetModifiers();

                this._host.SendMouseMoveEvent(_id, (int)point.X, (int)point.Y, false, modifiers);
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
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //OnMouseLeave event from touch or stylus needn't to be handled.
            //Use e.StylusDevice == null to ensure only mouse.
            if (!e.Handled && _host != null && e.StylusDevice == null)
            {
                var modifiers = e.GetModifiers();
                var point = e.GetPosition(this);

                _host.SendMouseMoveEvent(_id, (int)point.X, (int)point.Y, true, modifiers);

                // ((IWebBrowserInternal)this).SetTooltipText(null);
            }

            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.LostMouseCapture" /> attached event reaches an element in
        /// its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains event data.</param>
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (!e.Handled && _host != null)
            {
                _host.SendCaptureLostEvent(_id);
            }

            base.OnLostMouseCapture(e);
        }

        /// <summary>
        /// Handles the <see cref="E:MouseButton" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        private void OnMouseButton(MouseButtonEventArgs e)
        {
            if (!e.Handled && _host != null)
            {
                var modifiers = e.GetModifiers();
                var mouseUp = (e.ButtonState == MouseButtonState.Released);
                var point = e.GetPosition(this);

                ////if (e.ChangedButton == MouseButton.XButton1)
                ////{
                ////    if (CanGoBack && mouseUp)
                ////    {
                ////        this.Back();
                ////    }
                ////}
                ////else if (e.ChangedButton == MouseButton.XButton2)
                ////{
                ////    if (CanGoForward && mouseUp)
                ////    {
                ////        this.Forward();
                ////    }
                ////}
                ////else
                {
                    //Chromium only supports values of 1, 2 or 3.
                    //https://github.com/cefsharp/CefSharp/issues/3940
                    //Anything greater than 3 then we send click count of 1
                    var clickCount = e.ClickCount;

                    if (clickCount > 3)
                    {
                        clickCount = 1;
                    }

                    _host.SendMouseClickEvent(_id, (int)point.X, (int)point.Y, (MouseButtonType)e.ChangedButton, mouseUp, clickCount, modifiers);
                }

                e.Handled = true;
            }
        }

        #endregion

        //TODO
        //public event EventArgs FrameLoaded;

        //TODO
        //internal Task ExecuteJavascriptAsync(string script)
        //{
        //    _host.ExecuteJavascriptAsync(script);
        //}
    }
}
