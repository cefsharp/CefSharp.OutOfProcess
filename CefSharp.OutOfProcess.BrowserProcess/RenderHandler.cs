using System;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;

namespace CefSharp.OutOfProcess.BrowserProcess
{
    internal sealed class RenderHandler : IDisposable
    {
        private readonly string renderFileNameTemplate;
        private string renderFileName;
        private MemoryMappedViewAccessor viewAccessor;
        private MemoryMappedFile mappedFile;
        private int currentAvailableBytes = 0;

        public RenderHandler(string fileName)
        {
            renderFileNameTemplate = fileName;
        }

        public string OnPaint(IntPtr buffer, int width, int height)
        {
            const int bytesPerPixel = 32 / 8;
            const int reserverdSizeBits = 2 * sizeof(int);
            int maximumPixels = width * height;
            int requiredfBytes = (maximumPixels * bytesPerPixel) + reserverdSizeBits;

            bool createNewBitmap = mappedFile == null || currentAvailableBytes < requiredfBytes;

            if (createNewBitmap)
            {
                currentAvailableBytes = requiredfBytes;

                if (mappedFile != null)
                {
                    mappedFile.SafeMemoryMappedFileHandle.Close();
                    mappedFile.Dispose();

                    viewAccessor.SafeMemoryMappedViewHandle.Close();
                    viewAccessor.Dispose();
                }

                renderFileName = renderFileNameTemplate + Guid.NewGuid();

                mappedFile = MemoryMappedFile.CreateNew(renderFileName, requiredfBytes, MemoryMappedFileAccess.ReadWrite);
                viewAccessor = mappedFile.CreateViewAccessor(0, requiredfBytes, MemoryMappedFileAccess.Write);
            }

            var ptr = viewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle();

            Marshal.WriteInt32(ptr, width);
            Marshal.WriteInt32(ptr + sizeof(int), height);
            CopyMemory(ptr + reserverdSizeBits, buffer, (uint)requiredfBytes - reserverdSizeBits);

            viewAccessor.Flush();

            return renderFileName;
        }

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", ExactSpelling = true)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        public void Dispose()
        {
            viewAccessor?.Dispose();
            mappedFile?.Dispose();
        }
    }
}