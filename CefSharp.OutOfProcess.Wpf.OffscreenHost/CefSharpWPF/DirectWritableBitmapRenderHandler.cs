// Copyright © 2020 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

//NOTE:Classes in the CefSharp.Core namespace have been hidden from intellisnse so users don't use them directly

namespace CefSharp.Wpf.Rendering
{
    using System;
    using System.IO.MemoryMappedFiles;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using Rect = CefSharp.OutOfProcess.Interface.Rect;

    /// <summary>
    /// DirectWritableBitmapRenderHandler - directly copyies the buffer
    /// into writeableBitmap.BackBuffer. No additional copies or locking are used.
    /// Can only be used when CEF UI thread and WPF UI thread are the same (MultiThreadedMessageLoop = false).
    /// </summary>
    public sealed class DirectWritableBitmapRenderHandler : IDisposable
    {
        private readonly double dpiX;
        private readonly double dpiY;
        private readonly bool invalidateDirtyRect;

        private MemoryMappedFile mappedFile;
        private MemoryMappedViewAccessor viewAccessor;
        private string currentFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectWritableBitmapRenderHandler"/> class.
        /// </summary>
        /// <param name="dpiX">The dpi x.</param>
        /// <param name="dpiY">The dpi y.</param>
        /// <param name="invalidateDirtyRect">if true then only the direct rectangle will be updated, otherwise the whole bitmap will be redrawn.</param>
        /// <param name="dispatcherPriority">priority at which the bitmap will be updated on the UI thread.</param>
        public DirectWritableBitmapRenderHandler(double dpiX, double dpiY, bool invalidateDirtyRect = false, DispatcherPriority dispatcherPriority = DispatcherPriority.Render)
        {
            this.dpiX = dpiX;
            this.dpiY = dpiY;
            this.invalidateDirtyRect = invalidateDirtyRect;
        }

        /// <summary>
        /// Is called when source bitmap updates and transfers the content to the image control.
        /// </summary>
        /// <param name="dirtyRect">if only a part has been rerendered, this rect defines the rerendered subpart.</param>
        /// <param name="width">width of the bitmap.</param>
        /// <param name="height">height of the bitmap.</param>
        /// <param name="image">image to be paint on.</param>
        /// <param name="file">filename of a memorymappedfile containing the bitmap to be paint.</param>
        public void OnPaint(Rect dirtyRect, int width, int height, Image image, string file)
        {
            const int sizeInfoOffset = 2 * sizeof(int);
            var stride = width * 4;
            var noOfBytes = stride * height;
            if (currentFile != file)
            {
                viewAccessor?.Dispose();
                mappedFile?.Dispose();
                try
                {
                    mappedFile = MemoryMappedFile.OpenExisting(file);
                    viewAccessor = mappedFile.CreateViewAccessor(0, noOfBytes + sizeInfoOffset, MemoryMappedFileAccess.Read);
                    currentFile = file;
                }
                catch (Exception)
                {
                }
            }

            if (viewAccessor == null)
            {
                return;
            }

            var ptr = viewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
            if (ptr == IntPtr.Zero)
            {
                return;
            }

            int data_width = Marshal.ReadInt32(ptr);
            int data_height = Marshal.ReadInt32(ptr + sizeof(int));

            stride = data_width * 4;
            noOfBytes = stride * data_height;

            if (!(image.Source is WriteableBitmap writeableBitmap) || writeableBitmap.PixelWidth != data_width || writeableBitmap.PixelHeight != data_height)
            {
                image.SetCurrentValue(Image.SourceProperty, writeableBitmap = new WriteableBitmap(data_width, data_height, dpiX, dpiY, PixelFormats.Pbgra32, null));
            }

            if (writeableBitmap != null)
            {
                writeableBitmap.Lock();

                if (invalidateDirtyRect)
                {
                    // Update the dirty region
                    var sourceRect = new Int32Rect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

                    writeableBitmap.Lock();
                    writeableBitmap.WritePixels(sourceRect, ptr + sizeInfoOffset, noOfBytes, writeableBitmap.BackBufferStride, dirtyRect.X, dirtyRect.Y);
                    writeableBitmap.Unlock();
                }
                else
                {
                    // Update whole bitmap
                    var sourceRect = new Int32Rect(0, 0, data_width, data_height);

                    writeableBitmap.Lock();
                    writeableBitmap.WritePixels(sourceRect, ptr + sizeInfoOffset, noOfBytes, writeableBitmap.BackBufferStride);
                    writeableBitmap.Unlock();
                }

                writeableBitmap.Unlock();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            viewAccessor?.Dispose();
            mappedFile?.Dispose();
        }
    }
}
