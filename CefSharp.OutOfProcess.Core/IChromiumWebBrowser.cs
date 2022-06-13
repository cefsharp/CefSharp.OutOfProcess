using CefSharp.Puppeteer;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess
{
    /// <summary>
    /// OutOfProcess ChromiumWebBrowser
    /// </summary>
    public interface IChromiumWebBrowser
    {
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
        /// Gets all frames attached to the page.
        /// </summary>
        /// <value>An array of all frames attached to the page.</value>
        Frame[] Frames { get; }

        /// <summary>
        /// Gets page's main frame
        /// </summary>
        /// <remarks>
        /// Page is guaranteed to have a main frame which persists during navigations.
        /// </remarks>
        Frame MainFrame { get; }

        /// <summary>
        /// Navigates to an url
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
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
    }
}
