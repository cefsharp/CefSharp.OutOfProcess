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
        int Id { get; set; }

        /// <summary>
        /// Set the browser Hwnd
        /// </summary>
        /// <param name="hwnd">Hwnd</param>
        void SetBrowserHwnd(IntPtr hwnd);
    }
}
