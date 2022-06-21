using System;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess.Interface
{
    public interface IBrowserProcessServer
    {
        Task CloseBrowser(int browserId);
        Task SendDevToolsMessage(int browserId, string message);
        Task CloseHost();
        Task CreateBrowser(IntPtr parentHwnd, string url, int id);
    }
}
