using CefSharp.OutOfProcess.Interface;
using System;

namespace CefSharp.OutOfProcess.Internal
{
    public interface IRenderHandler
    {
        void OnPaint(bool isPopup, Rect dirtyRect, int width, int height, IntPtr buffer, byte[] data, string file);
    }
}