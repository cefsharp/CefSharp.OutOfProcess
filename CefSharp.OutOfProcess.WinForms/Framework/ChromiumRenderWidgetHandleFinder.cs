using PInvoke;
using System;
using System.Runtime.InteropServices;

namespace CefSharp.OutOfProcess.WinForms.Framework
{
    /// <summary>
    /// ChromiumWidgetHandleFinder
    /// </summary>
    public static class ChromiumRenderWidgetHandleFinder
    {
        /// <summary>
        /// EnumWindowProc delegate used by <see cref="EnumChildWindows(IntPtr, EnumWindowProc, IntPtr)"/>
        /// </summary>
        /// <param name="hwnd">A handle to a child window of the parent window specified in EnumChildWindows</param>
        /// <param name="lParam">The application-defined value given in EnumChildWindows</param>
        /// <returns>To continue enumeration, the callback function must return true; to stop enumeration, it must return false.</returns>
        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        /// <summary>
        /// Helper function used to find the child HWND with the ClassName matching <paramref name="chromeRenderWidgetHostClassName"/>
        /// Chromium's message-loop Window isn't created synchronously, so this may not find it.
        /// If so, you need to wait and try again later.
        /// In most cases you should use the <see cref="TryFindHandle(IWebBrowser, out IntPtr)"/> overload.
        /// </summary>
        /// <param name="parent">Parent  control Handle</param>
        /// <param name="chromeRenderWidgetHostClassName">class name used to match</param>
        /// <param name="chromerRenderWidgetHostHandle">Handle of the child HWND with the name</param>
        /// <returns>returns true if the HWND was found otherwise false.</returns>
        public static bool TryFindHandle(IntPtr parent, string chromeRenderWidgetHostClassName, out IntPtr chromerRenderWidgetHostHandle)
        {
            var chromeRenderWidgetHostHwnd = IntPtr.Zero;

            EnumWindowProc childProc = (IntPtr hWnd, IntPtr lParam) =>
            {
                var className = User32.GetClassName(hWnd);

                if (className == chromeRenderWidgetHostClassName)
                {
                    chromeRenderWidgetHostHwnd = hWnd;
                    return false;
                }

                return true;
            };

            EnumChildWindows(parent, childProc, IntPtr.Zero);

            chromerRenderWidgetHostHandle = chromeRenderWidgetHostHwnd;

            return chromerRenderWidgetHostHandle != IntPtr.Zero;
        }
    }
}
