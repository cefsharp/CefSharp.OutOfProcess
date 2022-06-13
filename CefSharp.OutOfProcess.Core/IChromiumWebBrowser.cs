using System;

namespace CefSharp.OutOfProcess
{
    /// <summary>
    /// OutOfProcess ChromiumWebBrowser
    /// </summary>
    public interface IChromiumWebBrowser
    {
        /// <summary>
        /// Identifier
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Set the browser Hwnd
        /// </summary>
        /// <param name="hwnd">Hwnd</param>
        void SetBrowserHwnd(IntPtr hwnd);

        /// <summary>
        /// Called when a DevTools message arrives from the browser process
        /// </summary>
        /// <param name="jsonMsg"></param>
        void OnDevToolsMessage(string jsonMsg);

        /// <summary>
        /// DevTools is ready in the browser process to create the DevToolsContext
        /// </summary>
        void OnDevToolsReady();
    }
}
