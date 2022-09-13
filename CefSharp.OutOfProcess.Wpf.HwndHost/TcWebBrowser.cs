// Copyright © 2022 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp.OutOfProcess.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Window = System.Windows.Window;
using System.Windows.Controls;
using CefSharp.Wpf.Rendering;
using System.Windows.Media;
using CefSharp.Wpf.Internals;
using Copy.CefSharp;
using Application = System.Windows.Application;
using CefSharp.Wpf;
using System.Threading.Tasks;
using CefSharp.Dom;
using CefSharp.OutOfProcess.Core;

namespace CefSharp.OutOfProcess.Wpf.HwndHost
{
    /// <summary>
    /// ChromiumWebBrowser is the WPF web browser control
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Control" />
    /// <seealso cref="CefSharp.Wpf.HwndHost.IWpfWebBrowser" />
    /// based on https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf
    /// and https://stackoverflow.com/questions/6500336/custom-dwm-drawn-window-frame-flickers-on-resizing-if-the-window-contains-a-hwnd/17471534#17471534
    public class TcWebBrowser  : ChromiumWebBrowser2
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

        private async void TcWebBrowser_DevToolsContextAvailable(object sender, EventArgs e)
        {
            await Task.Delay(5000);
            // DevToolsContext.GoToAsync("http://www.iota.org").Wait();

            DevToolsContext.EvaluateFunctionAsync(@"alert(""I am an alert box!"");");
            DevToolsContext.ExposeFunctionAsync("foo", Foo);
            
            DevToolsContext.EvaluateFunctionAsync("foo()");

            Dispatcher.Invoke(() =>
            {
           
            });
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
