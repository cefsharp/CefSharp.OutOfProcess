// Copyright © 2014 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;

namespace CefSharp.OutOfProcess
{
    /// <summary>
    /// Event arguments to the LoadingStateChanged event handler set up in IWebBrowser.
    /// </summary>
    public class LoadingStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Returns true if the browser can navigate forwards. 
        /// </summary>
        public bool CanGoForward { get; private set; }
        /// <summary>
        /// Returns true if the browser can navigate backwards. 
        /// </summary>
        public bool CanGoBack { get; private set; }
        /// <summary>
        /// Returns true if the browser is loading. 
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// LoadingStateChangedEventArgs
        /// </summary>
        /// <param name="browser">browser</param>
        /// <param name="canGoBack">can go back</param>
        /// <param name="canGoForward">can go forward</param>
        /// <param name="isLoading">is loading</param>
        public LoadingStateChangedEventArgs(bool canGoBack, bool canGoForward, bool isLoading)
        {
            CanGoBack = canGoBack;
            CanGoForward = canGoForward;
            IsLoading = isLoading;
        }
    }
}
