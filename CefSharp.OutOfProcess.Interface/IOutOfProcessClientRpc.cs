namespace CefSharp.OutOfProcess.Interface
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CefSharp;

    /// <summary>
    /// Send messages to the Remote Host (Browser process).
    /// </summary>
    public interface IOutOfProcessClientRpc
    {
        /// <summary>
        /// Close Browser
        /// </summary>
        /// <param name="browserId">browser Id</param>
        /// <returns>Task</returns>
        Task CloseBrowser(int browserId);

        /// <summary>
        /// Send DevTools message
        /// </summary>
        /// <param name="browserId">browser Id.</param>
        /// <param name="message">devtools message (json).</param>
        /// <returns>Task</returns>
        Task SendDevToolsMessage(int browserId, string message);

        Task ShowDevTools(int browserId);

        Task LoadUrl(int browserId, string url);

        /// <summary>
        /// Close the Browser Process (host).
        /// </summary>
        /// <returns>Task</returns>
        Task CloseHost();

        /// <summary>
        /// Create a new browser within the Browser Process.
        /// </summary>
        /// <param name="parentHwnd">parent Hwnd.</param>
        /// <param name="url">start url.</param>
        /// <param name="browserId">browser id.</param>
        /// <returns>Task</returns>
        ///
        Task CreateBrowser(IntPtr parentHwnd, string url, int browserId, IDictionary<string, object> requestContextPreferences);

        void UpdateRequestContextPreferences(int browserId, IDictionary<string, object> requestContextPreferences);

        void UpdateGlobalRequestContextPreferences(IDictionary<string, object> requestContextPreferences);

        /// <summary>
        /// Notify the browser that the window hosting it is about to be moved or resized.
        /// This will dismiss any existing popups (dropdowns).
        /// </summary>
        /// <param name="browserId">browser Id</param>
        void NotifyMoveOrResizeStarted(int browserIdm, int width, int height, int screenX, int screenY);

        /// <summary>
        /// Set whether the browser is focused.
        /// </summary>
        /// <param name="id">browser id</param>
        /// <param name="focus">set focus</param>
        void SetFocus(int browserId, bool focus);

        /// <summary>
        /// Sends a mouse click to the client.
        /// Custom implementation necessary because IDevToolsContext can't handle clicks on popups
        /// </summary>
        /// <param name="browserId"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="mouseButtonType"></param>
        /// <param name="mouseUp"></param>
        /// <param name="clickCount"></param>
        /// <param name="eventFlags"></param>
        void SendMouseClickEvent(int browserId, int x, int y, string mouseButtonType, bool mouseUp, int clickCount, uint eventFlags);
    }
}
