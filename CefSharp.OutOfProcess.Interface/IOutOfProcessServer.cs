using System;

namespace CefSharp.OutOfProcess.Interface
{
    public interface IOutOfProcessServer
    {
        /// <summary>
        /// Invoked after the CEF browser has been created
        /// </summary>
        /// <param name="browserId">browser Id</param>
        /// <param name="browserHwnd">HWND</param>
        /// <returns>Task</returns>
        void NotifyBrowserCreated(int browserId, IntPtr browserHwnd);
        void NotifyStatusMessage(int browserId, string statusMessage);
        void NotifyDevToolsAgentDetached(int browserId);
        void NotifyDevToolsMessage(int browserId, string devToolsMessage);
        void NotifyDevToolsReady(int browserId);
        void NotifyAddressChanged(int browserId, string address);
        void NotifyLoadingStateChange(int browserId, bool canGoBack, bool canGoForward, bool isLoading);
        void NotifyTitleChanged(int browserId, string title);
        void NotifyContextInitialized(int threadId, string cefSharpVersion, string cefVersion, string chromiumVersion);
    }
}
