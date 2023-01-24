using System;
using System.Collections.Generic;
using System.Text;

namespace CefSharp.OutOfProcess.Interface.Callbacks
{
    public interface IDownloadItemCallback : IDisposable
    {
        bool IsDisposed { get; }
        void Cancel();
        void Pause();
        void Resume();
    }
}
