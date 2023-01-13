namespace CefSharp.OutOfProcess.Interface.Callbacks
{
    public class JsDialogCallbackDetails : CallbackDetails
    {
        public string UserInput { get; set; }

        public bool Success { get; set; }
    }
}
