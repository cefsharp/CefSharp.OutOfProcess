using Copy.CefSharp.Structs;
using System;

namespace CefSharp.OutOfProcess.Interface
{
    /// <summary>
    /// Receive messages from the Remote Host (Browser process).
    /// Methods on this interface are invoked in response to messages
    /// originating from the Remote Host (browser process).
    /// </summary>
    public interface IOutOfProcessHostRpc
    {
        /// <summary>
        /// Invoked after the CEF browser has been created
        /// </summary>
        /// <param name="browserId">browser Id</param>
        /// <param name="browserHwnd">HWND</param>
        /// <returns>Task</returns>
        void NotifyBrowserCreated(int browserId, IntPtr browserHwnd);

        /// <summary>
        /// Adress changed for the specified browser
        /// </summary>
        /// <param name="browserId">browser Id</param>
        /// <param name="address">address</param>
        void NotifyAddressChanged(int browserId, string address);

        /// <summary>
        /// Loading State changed for the specified browser.
        /// This will be called twice, once when the browser starts loading
        /// with <paramref name="isLoading"/> set to true and a second time
        /// with <paramref name="isLoading"/> set to false when the browser
        /// has finished loading.
        /// </summary>
        /// <param name="browserId">browser Id</param>
        /// <param name="canGoBack">can go back</param>
        /// <param name="canGoForward">can go forward</param>
        /// <param name="isLoading">is loading</param>
        void NotifyLoadingStateChange(int browserId, bool canGoBack, bool canGoForward, bool isLoading);
        
        /// <summary>
        /// Context has been initialized in the Remote Host (Browser Process)
        /// </summary>
        /// <param name="threadId">thread Id</param>
        /// <param name="cefSharpVersion">CefSharp Version</param>
        /// <param name="cefVersion">Cef Version</param>
        /// <param name="chromiumVersion">Chromium Version</param>
        void NotifyContextInitialized(int threadId, string cefSharpVersion, string cefVersion, string chromiumVersion);

        void NotifyPaint(int browserId, bool isPopup, Rect dirtyRect, int width, int height, IntPtr buffer);
    }
}
