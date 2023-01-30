using CefSharp.Internals;
using System;
using CefSharp.OutOfProcess.Interface;
using CefSharp.Structs;
using CefSharp.Enums;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    /// <summary>
    /// An ChromiumWebBrowser instance specifically for hosting CEF out of process
    /// </summary>
    public class OffscreenOutOfProcessChromiumWebBrowser : OutOfProcessChromiumWebBrowser, IRenderWebBrowser
    {
        private readonly RenderHandler renderHandler;
        private readonly RenderHandler popupRenderHandler;

        public OffscreenOutOfProcessChromiumWebBrowser(IOutOfProcessHostRpc outOfProcessServer, int id, string address = "", IRequestContext requestContext = null)
          : base(outOfProcessServer, id, address, requestContext, true)
        {
            renderHandler = new RenderHandler($"0render_{Id}_");
            popupRenderHandler = new RenderHandler($"0render_{Id}_popup_");
        }

        /// <summary>
        /// The dpi scale factor, if the browser has already been initialized
        /// you must manually call IBrowserHost.NotifyScreenInfoChanged for the
        /// browser to be notified of the change.
        /// </summary>
        public float DpiScaleFactor { get; set; } = 1;

        internal CefSharp.Structs.Rect ViewRect { get; set; }

        /// <summary>
        /// Gets the ScreenInfo - currently used to get the DPI scale factor.
        /// </summary>
        /// <returns>ScreenInfo containing the current DPI scale factor</returns>
        ScreenInfo? IRenderWebBrowser.GetScreenInfo() => GetScreenInfo();

        /// <summary>
        /// Gets the ScreenInfo - currently used to get the DPI scale factor.
        /// </summary>
        /// <returns>ScreenInfo containing the current DPI scale factor</returns>
        protected virtual ScreenInfo? GetScreenInfo()
        {
            CefSharp.Structs.Rect rect = new CefSharp.Structs.Rect();
            CefSharp.Structs.Rect availableRect = new CefSharp.Structs.Rect();

            if (DpiScaleFactor > 1.0)
            {
                rect = rect.ScaleByDpi(DpiScaleFactor);
                availableRect = availableRect.ScaleByDpi(DpiScaleFactor);
            }

            var screenInfo = new ScreenInfo
            {
                DeviceScaleFactor = DpiScaleFactor,
                Rect = rect,
                AvailableRect = availableRect,
            };

            return screenInfo;
        }

        Structs.Rect IRenderWebBrowser.GetViewRect() => ViewRect;

        bool IRenderWebBrowser.GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = ViewRect.X + viewX;
            screenY = ViewRect.Y + viewY;

            return true;
        }

        void IRenderWebBrowser.OnAcceleratedPaint(PaintElementType type, Structs.Rect dirtyRect, IntPtr sharedHandle)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnPaint(PaintElementType type, Structs.Rect dirtyRect, IntPtr buffer, int width, int height)
        {
            var dirtyRectCopy = new Interface.Rect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);
            string file = type == PaintElementType.Popup
                ? popupRenderHandler.OnPaint(buffer, width, height)
                : renderHandler.OnPaint(buffer, width, height);

            OutofProcessHostRpc.NotifyPaint(Id, type == PaintElementType.Popup, dirtyRectCopy, width, height, file);
        }

        void IRenderWebBrowser.OnCursorChange(IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {
            // not implemented
        }

        bool IRenderWebBrowser.StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            // not implemented
            return false;
        }

        void IRenderWebBrowser.UpdateDragCursor(DragOperationsMask operation)
        {
            // not implemented
        }

        void IRenderWebBrowser.OnPopupShow(bool show) => OutofProcessHostRpc.NotifyPopupShow(Id, show);

        void IRenderWebBrowser.OnPopupSize(CefSharp.Structs.Rect rect) => OutofProcessHostRpc.NotifyPopupSize(Id, new Interface.Rect(rect.X, rect.Y, rect.Width, rect.Height));

        void IRenderWebBrowser.OnImeCompositionRangeChanged(Structs.Range selectedRange, CefSharp.Structs.Rect[] characterBounds)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
            // not implemented
        }
    }
}