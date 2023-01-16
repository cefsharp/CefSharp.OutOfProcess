using CefSharp.Dom;
using System;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess
{
    /// <summary>
    /// OutOfProcess ChromiumWebBrowser
    /// </summary>
    public interface IChromiumWebBrowser : IDisposable
    {
        /// <summary>
        /// Occurs when the browser address changed.
        /// </summary>
        event EventHandler<AddressChangedEventArgs> AddressChanged;

        /// <summary>
        /// Event handler for changes to the status message.
        /// </summary>
        event EventHandler<StatusMessageEventArgs> StatusMessage;

        /// <summary>
        /// Event handler that will get called when the Loading state has changed.
        /// This event will be fired twice. Once when loading is initiated either programmatically or
        /// by user action, and once when loading is terminated due to completion, cancellation of failure. 
        /// </summary>
        event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;

        /// <summary>
        /// Raised when the JavaScript <c>DOMContentLoaded</c> <see href="https://developer.mozilla.org/en-US/docs/Web/Events/DOMContentLoaded"/> event is dispatched.
        /// </summary>
        event EventHandler DOMContentLoaded;

        /// <summary>
        /// Raised when the browser crashes
        /// </summary>
        event EventHandler<ErrorEventArgs> BrowserProcessCrashed;

        /// <summary>
        /// Raised when a frame is attached.
        /// </summary>
        event EventHandler<FrameEventArgs> FrameAttached;

        /// <summary>
        /// Raised when a frame is detached.
        /// </summary>
        event EventHandler<FrameEventArgs> FrameDetached;

        /// <summary>
        /// Raised when a frame is navigated to a new url.
        /// </summary>
        event EventHandler<FrameEventArgs> FrameNavigated;

        /// <summary>
        /// Raised when JavaScript within the page calls one of console API methods, e.g. <c>console.log</c> or <c>console.dir</c>. Also emitted if the page throws an error or a warning.
        /// The arguments passed into <c>console.log</c> appear as arguments on the event handler.
        /// </summary>
        event EventHandler<ConsoleEventArgs> ConsoleMessage;

        /// <summary>
        /// Raised when the JavaScript <c>load</c> <see href="https://developer.mozilla.org/en-US/docs/Web/Events/load"/> event is dispatched.
        /// </summary>
        event EventHandler JavaScriptLoad;

        /// <summary>
        /// Raised when an uncaught exception happens within the browser.
        /// </summary>
        event EventHandler<PageErrorEventArgs> RuntimeExceptionThrown;

        /// <summary>
        /// Raised when the page opens a new tab or window.
        /// </summary>
        event EventHandler<PopupEventArgs> Popup;

        /// <summary>
        /// Raised when a browser issues a request. The <see cref="NetworkRequest"/> object is read-only.
        /// In order to intercept and mutate requests, see <see cref="IDevToolsContext.SetRequestInterceptionAsync(bool)"/>
        /// </summary>
        event EventHandler<RequestEventArgs> NetworkRequest;

        /// <summary>
        /// Raised when a request fails, for example by timing out.
        /// </summary>
        event EventHandler<RequestEventArgs> NetworkRequestFailed;

        /// <summary>
        /// Raised when a request finishes successfully.
        /// </summary>
        event EventHandler<RequestEventArgs> NetworkRequestFinished;

        /// <summary>
        /// Raised when a request ended up loading from cache.
        /// </summary>
        event EventHandler<RequestEventArgs> NetworkRequestServedFromCache;

        /// <summary>
        /// Fired for top level page lifecycle events such as navigation, load, paint, etc.
        /// </summary>
        event EventHandler<LifecycleEventArgs> LifecycleEvent;

        /// <summary>
        /// Fired when the <see cref="DevToolsContext"/> is available
        /// </summary>
        event EventHandler DevToolsContextAvailable;

        /// <summary>
        /// Raised when a <see cref="NetworkResponse"/> is received.
        /// </summary>
        /// <example>
        /// An example of handling <see cref="NetworkResponse"/> event:
        /// <code>
        /// <![CDATA[
        /// var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        /// browser.Response += async(sender, e) =>
        /// {
        ///     if (e.Response.Url.Contains("script.js"))
        ///     {
        ///         tcs.TrySetResult(await e.Response.TextAsync());
        ///     }
        /// };
        ///
        /// await Task.WhenAll(
        ///     browser.LoadUrlAsync(TestConstants.ServerUrl + "/grid.html"),
        ///     tcs.Task);
        /// Console.WriteLine(await tcs.Task);
        /// ]]>
        /// </code>
        /// </example>
        event EventHandler<ResponseCreatedEventArgs> NetworkResponse;

        /// <summary>
        /// DevTools Context
        /// </summary>
        IDevToolsContext DevToolsContext { get; }

        /// <summary>
        /// Loads the specified <paramref name="url"/> in the Main Frame.
        /// </summary>
        /// <param name="url">The URL to be loaded.</param>
        /// <remarks>
        /// This is exactly the same as calling Load(string), it was added
        /// as the method name is more meaningful and easier to discover
        /// via Intellisense.
        /// </remarks>
        void LoadUrl(string url);

        /// <summary>
        /// A flag that indicates whether the WebBrowser is initialized (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance is browser initialized; otherwise, <c>false</c>.</value>
        bool IsBrowserInitialized { get; }

        /// <summary>
        /// A flag that indicates whether the WebBrowser has been disposed (<see langword="true" />) or not (<see langword="false" />)
        /// </summary>
        /// <value><see langword="true" /> if this instance is disposed; otherwise, <see langword="false" /></value>
        bool IsDisposed { get; }

        /// <summary>
        /// The address (URL) which the browser control is currently displaying.
        /// Will automatically be updated as the user navigates to another page (e.g. by clicking on a link).
        /// </summary>
        /// <value>The address.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        string Address { get; }

        /// <summary>
        /// Gets all frames attached to the browser.
        /// </summary>
        /// <value>An array of all frames attached to the browser.</value>
        Frame[] Frames { get; }

        /// <summary>
        /// Gets browser's main frame
        /// </summary>
        /// <remarks>
        /// Browser is guaranteed to have a main frame which persists during navigations.
        /// </remarks>
        Frame MainFrame { get; }

        /// <summary>
        /// Navigates to an url
        /// </summary>
        /// <param name="url">URL to navigate to. The url should include scheme, e.g. https://.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout. </param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="WaitUntilNavigation.Load"/>. Given an array of <see cref="WaitUntilNavigation"/>, navigation is considered to be successful after all events have been fired</param>
        /// <returns>Task which resolves to the main resource response. In case of multiple redirects, the navigation will resolve with the response of the last redirect</returns>
        Task<Response> LoadUrlAsync(string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null);

        /// <summary>
        /// Navigate to the previous page in history.
        /// </summary>
        /// <returns>Task that resolves to the main resource response. In case of multiple redirects,
        /// the navigation will resolve with the response of the last redirect. If can not go back, resolves to null.</returns>
        /// <param name="options">Navigation parameters.</param>
        Task<Response> GoBackAsync(NavigationOptions options = null);

        /// <summary>
        /// Navigate to the next page in history.
        /// </summary>
        /// <returns>Task that resolves to the main resource response. In case of multiple redirects,
        /// the navigation will resolve with the response of the last redirect. If can not go forward, resolves to null.</returns>
        /// <param name="options">Navigation parameters.</param>
        Task<Response> GoForwardAsync(NavigationOptions options = null);
        Handler.IDialogHandler DialogHandler { get; set; }

        Handler.IJsDialogHandler JsDialogHandler { get; set; }

        Handler.IDownloadHandler DownloadHandler { get; set; }
    }
}
