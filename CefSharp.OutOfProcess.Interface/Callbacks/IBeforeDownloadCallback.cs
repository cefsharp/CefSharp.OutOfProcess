using System;
using System.Collections.Generic;
using System.Text;

namespace CefSharp.OutOfProcess.Interface.Callbacks
{
    public interface IBeforeDownloadCallback : IDisposable
    {
        bool IsDisposed { get; }
        void Continue(string downloadPath, bool showDialog);
    }
}
