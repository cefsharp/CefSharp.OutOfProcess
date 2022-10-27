using Copy.CefSharp;
using System;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess.Interface
{
    /// <summary>
    /// Send messages to the Remote Host (Browser process)
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
        /// <param name="browserId">browser Id</param>
        /// <param name="message"devtools message (json)></param>
        /// <returns>Task</returns>
        Task SendDevToolsMessage(int browserId, string message);

        /// <summary>
        /// Close the Browser Process (host)
        /// </summary>
        /// <returns>Task</returns>
        Task CloseHost();

        /// <summary>
        /// Create a new browser within the Browser Process
        /// </summary>
        /// <param name="parentHwnd">parent Hwnd</param>
        /// <param name="url">start url</param>
        /// <param name="browserId">browser id</param>
        /// <returns>Task</returns>
        Task CreateBrowser(IntPtr parentHwnd, string url, int browserId);

        // TODO for PR to provide both render options. Task CreateBrowser(string sharedFileAccesor, string url, int browserId);

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

        void LoadUrl(int browserId, string address);


        void SendCaptureLostEvent(int browserId);

        void SendMouseClickEvent(int browserId, int X, int Y, MouseButtonType changedButton, bool mouseUp, int clickCount, CefEventFlags modifiers);

        void SendMouseMoveEvent(int browserId, int X, int Y, bool mouseLeave, CefEventFlags modifiers);

        void ExecuteJavaScriptAsync(int browserId, string script);
    }
}
