namespace CefSharp.OutOfProcess.Interface.Callbacks
{
    public class FileDialogCallbackDetails : CallbackDetails
    {
        public string[] Files { get; set; }

        public bool Continue { get; set; }
    }
}
