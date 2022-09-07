using Copy.CefSharp.Structs;
using System;

namespace CefSharp.OutOfProcess.Internal
{
    public interface IChromiumWebBrowserInternal : IDisposable
    {
        /// <summary>
        /// Identifier
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Set the browser Hwnd
        /// </summary>
        /// <param name="hwnd">Hwnd</param>
        void OnAfterBrowserCreated(IntPtr hwnd);
        void OnPaint(bool isPopup, Rect dirtyRect, int width, int height, IntPtr buffer);
        void SetAddress(string address);
        void SetLoadingStateChange(bool canGoBack, bool canGoForward, bool isLoading);
    }
}
