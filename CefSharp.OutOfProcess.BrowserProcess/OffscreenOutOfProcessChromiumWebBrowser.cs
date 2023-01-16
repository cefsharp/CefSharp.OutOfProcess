using CefSharp.Internals;
using System;
using CefSharp.OutOfProcess.Interface;
using CefSharp.Wpf.Internals;
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

        /// <summary>
        /// The MonitorInfo based on the current hwnd
        /// </summary>
        private MonitorInfoEx monitorInfo;

        public OffscreenOutOfProcessChromiumWebBrowser(IOutOfProcessHostRpc outOfProcessServer, int id, string address = "", IRequestContext requestContext = null)
          : base(outOfProcessServer, id, address, requestContext, true)
        {
            renderHandler = new RenderHandler($"0render_{_id}_");
            popupRenderHandler = new RenderHandler($"0render_{_id}_popup_");
        }

        /// <summary>
        /// The dpi scale factor, if the browser has already been initialized
        /// you must manually call IBrowserHost.NotifyScreenInfoChanged for the
        /// browser to be notified of the change.
        /// </summary>
        public float DpiScaleFactor { get; set; } = 1;

        public System.Drawing.Point browserLocation { get; internal set; }

        public CefSharp.Structs.Rect viewRect { get; internal set; }

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
            CefSharp.Structs.Rect rect = monitorInfo.Monitor;
            CefSharp.Structs.Rect availableRect = monitorInfo.WorkArea;

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

        CefSharp.Structs.Rect IRenderWebBrowser.GetViewRect() => viewRect;

        bool IRenderWebBrowser.GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = browserLocation.X;
            screenY = browserLocation.Y;

            return true;
        }

        void IRenderWebBrowser.OnAcceleratedPaint(PaintElementType type, CefSharp.Structs.Rect dirtyRect, IntPtr sharedHandle)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnPaint(PaintElementType type, Structs.Rect dirtyRect, IntPtr buffer, int width, int height)
        {
            var dirtyRectCopy = new Interface.Rect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);
            string file = type == PaintElementType.Popup
                ? popupRenderHandler.OnPaint(buffer, width, height)
                : renderHandler.OnPaint(buffer, width, height);

            _outofProcessHostRpc.NotifyPaint(Id, type == PaintElementType.Popup, dirtyRectCopy, width, height, file);
        }

        void IRenderWebBrowser.OnCursorChange(IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {
            // TODO: (CEF)
        }

        bool IRenderWebBrowser.StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            // TODO: (CEF)
            return false;
        }

        void IRenderWebBrowser.UpdateDragCursor(DragOperationsMask operation)
        {
            // TODO: (CEF)
        }

        void IRenderWebBrowser.OnPopupShow(bool show) => _outofProcessHostRpc.OnPopupShow(Id, show);

        void IRenderWebBrowser.OnPopupSize(CefSharp.Structs.Rect rect) => _outofProcessHostRpc.OnPopupSize(Id, new Interface.Rect(rect.X, rect.Y, rect.Width, rect.Height));

        void IRenderWebBrowser.OnImeCompositionRangeChanged(Structs.Range selectedRange, CefSharp.Structs.Rect[] characterBounds)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
            //throw new NotImplementedException();
        }
    }
}