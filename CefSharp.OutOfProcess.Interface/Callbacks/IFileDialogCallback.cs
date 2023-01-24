namespace CefSharp.OutOfProcess.Interface.Callbacks
{
    using System.Collections.Generic;

    public interface IFileDialogCallback
    {
        bool IsDisposed { get; }
        void Cancel();
        void Continue(List<string> filePaths);
    }
}
