using CefSharp.OutOfProcess.Model;
using System;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess
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

        /// <summary>
        /// Notify the browser that the window hosting it is about to be moved or resized.
        /// This will dismiss any existing popups (dropdowns).
        /// </summary>
        /// <param name="browserId">browser Id</param>
        void NotifyMoveOrResizeStarted(int browserId);

        /// <summary>
        /// Set whether the browser is focused.
        /// </summary>
        /// <param name="id">browser id</param>
        /// <param name="focus">set focus</param>
        void SetFocus(int browserId, bool focus);

        /// <summary>
        /// Set the value associated with preference name. If value is null the
        /// preference will be restored to its default value. If setting the preference
        /// fails then <see cref="SetPreferenceResponse.ErrorMessage"/> will be populated
        /// with a detailed description of the problem.
        /// Preferences set via the command-line usually cannot be modified.
        /// </summary>
        /// <param name="browserId">The browser id.</param>
        /// <param name="name">The preference name</param>
        /// <param name="value">The preference value</param>
        Task<SetPreferenceResponse> SetRequestContextPreferenceAsync(int browserId, string name, object value);
    }
}
