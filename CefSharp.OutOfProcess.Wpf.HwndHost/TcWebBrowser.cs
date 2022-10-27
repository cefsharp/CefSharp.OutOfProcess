// Copyright © 2022 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp.Dom;
using System;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess.Wpf.HwndHost
{
    /// <summary>
    /// ChromiumWebBrowser is the WPF web browser control
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Control" />
    /// <seealso cref="CefSharp.Wpf.HwndHost.IWpfWebBrowser" />
    /// based on https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf
    /// and https://stackoverflow.com/questions/6500336/custom-dwm-drawn-window-frame-flickers-on-resizing-if-the-window-contains-a-hwnd/17471534#17471534
    public sealed class TcWebBrowser : ChromiumWebBrowser2
    {
        public TcWebBrowser()
        {
            base.DevToolsContextAvailable += TcWebBrowser_DevToolsContextAvailable;
        }

        internal override void OnFrameLoadStart(string frameName, string url)
        {
            base.OnFrameLoadStart(frameName, url);
        }

        internal override void OnFrameLoadEnd(string frameName, string url, int httpStatusCode)
        {
            //   DevToolsContext.ExposeFunctionAsync("foo", () => throw new InvalidOperationException()).Wait();
            //   DevToolsContext.EvaluateFunctionAsync("foo()").Wait();
        }

        protected override void OnInitializeDevContext(DevToolsContext context)
        {
            base.OnInitializeDevContext(context);

            context.FrameAttached += Context_FrameAttached;
            context.FrameDetached += Context_FrameDetached;
            context.FrameNavigated += Context_FrameNavigated;
        }

        private void Context_FrameNavigated(object sender, FrameEventArgs e)
        {
            ;
        }

        private void Context_FrameDetached(object sender, FrameEventArgs e)
        {
            ;
        }

        private void Context_FrameAttached(object sender, FrameEventArgs e)
        {
            ;
        }

        private async void TcWebBrowser_DevToolsContextAvailable(object sender, EventArgs e)
        {
            await Task.Delay(5500); // TODO 500 was not reliable enough
            Task t = DevToolsContext.ExposeFunctionAsync("Foo", Foo);
            t.Wait();

            Task tt = DevToolsContext.EvaluateExpressionAsync("Foo()");
            tt.Wait();

            DevToolsContext.GoToAsync("http://www.sz.de");
        }

        private void Foo()
        {
            ;
        }

        protected override void OnIsBrowserInitializedChanged(bool oldValue, bool newValue)
        {
            base.OnIsBrowserInitializedChanged(oldValue, newValue);
        }
    }
}
