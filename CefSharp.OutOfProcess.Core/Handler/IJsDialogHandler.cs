namespace CefSharp.OutOfProcess.Handler
{

    // Summary:
    //     Implement this interface to handle events related to JavaScript dialogs. The
    //     methods of this class will be called on the CEF UI thread.
    public interface IJsDialogHandler
    {
        //
        // Summary:
        //     Called to run a JavaScript dialog.
        //
        // Parameters:
        //   chromiumWebBrowser:
        //     the ChromiumWebBrowser control
        //
        //   browser:
        //     the browser object
        //
        //   originUrl:
        //     originating url
        //
        //   dialogType:
        //     Dialog Type
        //
        //   messageText:
        //     Message Text
        //
        //   defaultPromptText:
        //     value will be specified for prompt dialogs only
        //
        //   callback:
        //     Callback can be executed inline or in an async fashion
        //
        //   suppressMessage:
        //     Set suppressMessage to true and return false to suppress the message (suppressing
        //     messages is preferable to immediately executing the callback as this is used
        //     to detect presumably malicious behavior like spamming alert messages in onbeforeunload).
        //     Set suppressMessage to false and return false to use the default implementation
        //     (the default implementation will show one modal dialog at a time and suppress
        //     any additional dialog requests until the displayed dialog is dismissed).
        //
        // Returns:
        //     Return true if the application will use a custom dialog or if the callback has
        //     been executed immediately. Custom dialogs may be either modal or modeless. If
        //     a custom dialog is used the application must execute |callback| once the custom
        //     dialog is dismissed.
        bool OnJSDialog(IChromiumWebBrowser chromiumWebBrowser, string originUrl, CefJsDialogType dialogType, string messageText, string defaultPromptText, IJsDialogCallback callback, ref bool suppressMessage);

        //
        // Summary:
        //     Called to run a dialog asking the user if they want to leave a page. Return false
        //     to use the default dialog implementation. Return true if the application will
        //     use a custom dialog or if the callback has been executed immediately. Custom
        //     dialogs may be either modal or modeless. If a custom dialog is used the application
        //     must execute callback once the custom dialog is dismissed.
        //
        // Parameters:
        //   chromiumWebBrowser:
        //     the ChromiumWebBrowser control
        //
        //   browser:
        //     the browser object
        //
        //   messageText:
        //     message text (optional)
        //
        //   isReload:
        //     indicates a page reload
        //
        //   callback:
        //     Callback can be executed inline or in an async fashion
        //
        // Returns:
        //     Return false to use the default dialog implementation otherwise return true to
        //     handle with your own custom implementation.
        bool OnBeforeUnloadDialog(IChromiumWebBrowser chromiumWebBrowser, string messageText, bool isReload, IJsDialogCallback callback);

        //
        // Summary:
        //     Called to cancel any pending dialogs and reset any saved dialog state. Will be
        //     called due to events like page navigation irregardless of whether any dialogs
        //     are currently pending.
        //
        // Parameters:
        //   chromiumWebBrowser:
        //     the ChromiumWebBrowser control
        //
        //   browser:
        //     the browser object
        void OnResetDialogState(IChromiumWebBrowser chromiumWebBrowser);

        //
        // Summary:
        //     Called when the dialog is closed.
        //
        // Parameters:
        //   chromiumWebBrowser:
        //     the ChromiumWebBrowser control
        //
        //   browser:
        //     the browser object
        void OnDialogClosed(IChromiumWebBrowser chromiumWebBrowser);
    }
}