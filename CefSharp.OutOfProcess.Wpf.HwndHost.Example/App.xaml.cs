// Copyright © 2019 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Windows;

namespace CefSharp.OutOfProcess.Wpf.HwndHost.Example
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
#if DEBUG
        private string _buildType = "Debug";
#else
        private string _buildType = "Release";
#endif

#if NETCOREAPP3_1_OR_GREATER
        private string _targetFramework = "netcoreapp3.1";
#else
        private string _targetFramework = "net462";
#endif

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var outOfProcessHostPath = Path.GetFullPath($"..\\..\\..\\..\\CefSharp.OutOfProcess.BrowserProcess\\bin\\{_buildType}\\{_targetFramework}");
            outOfProcessHostPath = Path.Combine(outOfProcessHostPath, OutOfProcessHost.HostExeName);

            ChromiumWebBrowser.SetHostProcessPath(outOfProcessHostPath);
        }
    }
}
