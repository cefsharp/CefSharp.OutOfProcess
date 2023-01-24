namespace CefSharp.OutOfProcess.Handler
{
    using System.Collections.Generic;
    using CefSharp.OutOfProcess.Interface.Callbacks;

    public interface IDialogHandler
    {
        //
        // Summary:
        //     Runs a file chooser dialog.
        //
        // Parameters:
        //   chromiumWebBrowser:
        //     the ChromiumWebBrowser control
        //
        //   browser:
        //     the browser object
        //
        //   mode:
        //     represents the type of dialog to display
        //
        //   title:
        //     the title to be used for the dialog. It may be empty to show the default title
        //     ("Open" or "Save" depending on the mode).
        //
        //   defaultFilePath:
        //     is the path with optional directory and/or file name component that should be
        //     initially selected in the dialog.
        //
        //   acceptFilters:
        //     are used to restrict the selectable file types and may any combination of (a)
        //     valid lower-cased MIME types (e.g. "text/*" or "image/*"), (b) individual file
        //     extensions (e.g. ".txt" or ".png"), (c) combined description and file extension
        //     delimited using "|" and ";" (e.g. "Image Types|.png;.gif;.jpg").
        //
        //   callback:
        //     Callback interface for asynchronous continuation of file dialog requests.
        //
        // Returns:
        //     To display a custom dialog return true. To display the default dialog return
        //     false.
        bool OnFileDialog(IChromiumWebBrowser chromiumWebBrowser, string mode, string title, string defaultFilePath, IEnumerable<string> acceptFilters, IFileDialogCallback callback);
    }
}