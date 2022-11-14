using CefSharp.Internals;
using System;
using CefSharp.OutOfProcess.Interface;
using System.Runtime.InteropServices;
using CefSharp.Wpf.Internals;
using CefSharp.Structs;
using System.IO.MemoryMappedFiles;
using CefSharp.Enums;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    /// <summary>
    /// An ChromiumWebBrowser instance specifically for hosting CEF out of process
    /// </summary>
    public class OffscreenOutOfProcessChromiumWebBrowser : OutOfProcessChromiumWebBrowser, IRenderWebBrowser
    {
        public OffscreenOutOfProcessChromiumWebBrowser(IOutOfProcessHostRpc outOfProcessServer, int id, string address = "", IRequestContext requestContext = null, bool offscreenRendering = false)
          : base(outOfProcessServer, id, address, requestContext, true)
        {
        }

        /// <summary>
        /// The MonitorInfo based on the current hwnd
        /// </summary>
        private MonitorInfoEx monitorInfo;


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
                AvailableRect = availableRect
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
            var dirtyRectCopy = new CefSharp.OutOfProcess.Interface.Rect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

            // PixelFormat PixelFormat = PixelFormats.Pbgra32;
            int BytesPerPixel = 32 / 8;
            int maximumPixels = 3600 * 2000;// width * height;
            int maximumNumberOfBytes = maximumPixels * BytesPerPixel;

            bool createNewBitmap = mappedFile == null || currentSize.Height != height || currentSize.Width != width;

            if (createNewBitmap)
            {
                //If the MemoryMappedFile is smaller than we need then create a larger one
                //If it's larger then we need then rather than going through the costly expense of
                //allocating a new one we'll just use the old one and only access the number of bytes we require.
                if (viewAccessor == null)
                {
                    //  ReleaseMemoryMappedView(ref mappedFile, ref viewAccessor);

                    renderFileName = $"0render_{_id}_{Guid.NewGuid()}";
                    mappedFile = MemoryMappedFile.CreateNew(renderFileName, maximumNumberOfBytes, MemoryMappedFileAccess.ReadWrite);

                    viewAccessor = mappedFile.CreateViewAccessor(0, maximumNumberOfBytes, MemoryMappedFileAccess.Write);
                }

                currentSize = new Size(width, height);
            }

            var usedBytes = width * height * BytesPerPixel;


            //{
            //    Buffer.MemoryCopy(
            //        viewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle().ToPointer(), buffer.ToPointer(),
            //        (uint)usedBytes,
            //        maximumNumberOfBytes);
            //}
            CopyMemory(viewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle(), buffer, (uint)usedBytes);
            viewAccessor.Flush();
            _outofProcessHostRpc.NotifyPaint(Id, type == PaintElementType.Popup, dirtyRectCopy, width, height, IntPtr.Zero, null, renderFileName);
        }

        string renderFileName;

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", ExactSpelling = true)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);


        protected void ReleaseMemoryMappedView(ref MemoryMappedFile mappedFile, ref MemoryMappedViewAccessor stream)
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }

            if (mappedFile != null)
            {
                mappedFile.Dispose();
                mappedFile = null;
            }
        }

        MemoryMappedViewAccessor viewAccessor;
        MemoryMappedFile mappedFile;
        Size currentSize;

        void IRenderWebBrowser.OnCursorChange(IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {
            // throw new NotImplementedException();
        }

        bool IRenderWebBrowser.StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.UpdateDragCursor(DragOperationsMask operation)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnPopupShow(bool show)
        {
            throw new NotImplementedException();
        }

        void IRenderWebBrowser.OnPopupSize(CefSharp.Structs.Rect rect)
        {
            throw new NotImplementedException();
        }

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