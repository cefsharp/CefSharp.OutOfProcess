namespace CefSharp.OutOfProcess.Interface.Callbacks
{
    public sealed class DownloadCallbackDetails : CallbackDetails
    {
        public bool Cancel { get; set; }

        public bool Pause { get; set; }

        public bool Resume { get; set; }
    }
}
