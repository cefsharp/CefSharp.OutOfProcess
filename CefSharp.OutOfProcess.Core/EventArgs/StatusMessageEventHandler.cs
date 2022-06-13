// Copyright © 2014 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;

namespace CefSharp.OutOfProcess
{
    /// <summary>
    /// Event arguments to the StatusMessage event handler.
    /// </summary>
    public class StatusMessageEventArgs : EventArgs
    {
        /// <summary>
        /// StatusMessageEventArgs
        /// </summary>
        /// <param name="value">status message value</param>
        public StatusMessageEventArgs(string value)
        {
            Value = value;
        }

        /// <summary>
        /// The value of the status message.
        /// </summary>
        public string Value { get; private set; }
    }
}
