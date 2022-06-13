// Copyright © 2014 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;

namespace CefSharp.OutOfProcess
{
    /// <summary>
    /// Event arguments for the AddressChanged event handler.
    /// </summary>
    public class AddressChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The new address
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// Creates a new AddressChangedEventArgs event argument.
        /// </summary>
        /// <param name="address">the address</param>
        public AddressChangedEventArgs(string address)
        {
            Address = address;
        }
    }
}
