namespace CefSharp.OutOfProcess.Interface.Callbacks
{
    public sealed class BeforeDownloadCallbackDetails : CallbackDetails
    {
        public string DownloadPath { get; set; }

        public bool ShowDialog { get; set; }
    }
}
