// Copyright © 2020 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

//NOTE:Classes in the CefSharp.Core namespace have been hidden from intellisnse so users don't use them directly

using System;

namespace CefSharp
{
    /// <summary>
    /// Native static methods for low level operations, memory copy
    /// Avoids having to P/Invoke as we can call the C++ API directly.
    /// </summary>
    public static class NativeMethodWrapper
    {
        public static void MemoryCopy(IntPtr dest, IntPtr src, int numberOfBytes)
        {
          //  CefSharp.Core.NativeMethodWrapper.MemoryCopy(dest, src, numberOfBytes);
            PInvoke.Kernel32.CopyMemory(dest, src, new IntPtr(numberOfBytes));
        }

        ////public static bool IsFocused(IntPtr handle)
        ////{
        ////    return PInvoke.User32.GetFoc
        ////    return CefSharp.Core.NativeMethodWrapper.IsFocused(handle);
        ////}

        public static void SetWindowPosition(IntPtr handle, int x, int y, int width, int height)
        {
            PInvoke.User32.SetWindowPos(handle, handle -2 , x, y, width, height, 0);
        }

        public static void SetWindowParent(IntPtr child, IntPtr newParent)
        {
            PInvoke.User32.SetParent(child, newParent);
        }

        ////public static void RemoveExNoActivateStyle(IntPtr browserHwnd)
        ////{
        ////    PInvoke.User32.
        ////    CefSharp.Core.NativeMethodWrapper.RemoveExNoActivateStyle(browserHwnd);
        ////}

        ////public static IntPtr LoadCursorFromLibCef(int resourceIdentifier)
        ////{
        ////    return CefSharp.Core.NativeMethodWrapper.LoadCursorFromLibCef(resourceIdentifier);
        ////}
    }
}