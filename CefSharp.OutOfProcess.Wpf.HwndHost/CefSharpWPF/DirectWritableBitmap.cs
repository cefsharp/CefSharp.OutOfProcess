// Copyright © 2019 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.IO.MemoryMappedFiles;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Rect = Copy.CefSharp.Structs.Rect;

namespace CefSharp.Wpf.Rendering
{
    /// <summary>
    /// DirectWritableBitmapRenderHandler - directly copyies the buffer
    /// into writeableBitmap.BackBuffer. No additional copies or locking are used.
    /// Can only be used when CEF UI thread and WPF UI thread are the same (MultiThreadedMessageLoop = false)
    /// </summary>
    public sealed class DirectWritableBitmapRenderHandler : IDisposable
    {
        private readonly double dpiX;
        private readonly double dpiY;
        private readonly bool invalidateDirtyRect;

        /// <summary>
        /// Initializes a new instance of the <see cref="WritableBitmapRenderHandler"/> class.
        /// </summary>
        /// <param name="dpiX">The dpi x.</param>
        /// <param name="dpiY">The dpi y.</param>
        /// <param name="invalidateDirtyRect">if true then only the direct rectangle will be updated, otherwise the whole bitmap will be redrawn</param>
        /// <param name="dispatcherPriority">priority at which the bitmap will be updated on the UI thread</param>
        public DirectWritableBitmapRenderHandler(double dpiX, double dpiY, bool invalidateDirtyRect = true, DispatcherPriority dispatcherPriority = DispatcherPriority.Render)
        {
            this.dpiX = dpiX;
            this.dpiY = dpiY;
            this.invalidateDirtyRect = invalidateDirtyRect;
        }

        MemoryMappedFile mappedFile;
        MemoryMappedViewAccessor viewAccessor;

        public void OnPaint(bool isPopup, Rect dirtyRect, IntPtr buffer, byte[] data, int width, int height, Image image, string file)
        {
            var stride = width * 4;
            var noOfBytes = stride * height;
            if (mappedFile == null)
            {
                mappedFile = MemoryMappedFile.OpenExisting(file);
                viewAccessor = mappedFile.CreateViewAccessor(0, noOfBytes, MemoryMappedFileAccess.Read);
            }

            var writeableBitmap = image.Source as WriteableBitmap;
            if (writeableBitmap == null || writeableBitmap.PixelWidth != width || writeableBitmap.PixelHeight != height)
            {
                image.Source = writeableBitmap = new WriteableBitmap(width, height, dpiX, dpiY, AbstractRenderHandler.PixelFormat, null);
                viewAccessor = mappedFile.CreateViewAccessor(0, noOfBytes, MemoryMappedFileAccess.Read);
            }

            if (writeableBitmap != null)
            {
                writeableBitmap.Lock();

                if (invalidateDirtyRect)
                {
                    // Update the dirty region
                    var sourceRect = new Int32Rect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

                    writeableBitmap.Lock();
                    writeableBitmap.WritePixels(sourceRect, viewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle(), noOfBytes, writeableBitmap.BackBufferStride, dirtyRect.X, dirtyRect.Y);
                    writeableBitmap.Unlock();
                }
                else
                {
                    // Update whole bitmap
                    var sourceRect = new Int32Rect(0, 0, width, height);

                    writeableBitmap.Lock();
                    writeableBitmap.WritePixels(sourceRect, viewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle(), noOfBytes, writeableBitmap.BackBufferStride);
                    writeableBitmap.Unlock();
                }

                writeableBitmap.Unlock();
            }
        }

        public void Dispose()
        {
            if (mappedFile != null)
            {
                mappedFile.Dispose();
                viewAccessor.Dispose();
            }
        }
    }
}
