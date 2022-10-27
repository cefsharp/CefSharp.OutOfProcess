using CefSharp.Dom.Transport;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess.Core
{
    public class OutOfProcessConnectionTransport : IConnectionTransport
    {
        public bool IsClosed { get; private set; }

        private int BrowserId { get; }

        private OutOfProcessHost OutOfProcessHost { get; }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<MessageErrorEventArgs> MessageError;
        public event EventHandler Disconnected;

        public OutOfProcessConnectionTransport(int browserId, OutOfProcessHost outOfProcessHost)
        {
            BrowserId = browserId;
            OutOfProcessHost = outOfProcessHost;
        }

        void IDisposable.Dispose()
        {

        }

        public void InvokeMessageReceived(string message)
        {
            Debug.WriteLine("<   " + message);
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }

        Task IConnectionTransport.SendAsync(string message)
        {
            Debug.WriteLine(">> " + message);
            return OutOfProcessHost.SendDevToolsMessageAsync(BrowserId, message);
        }

        void IConnectionTransport.StopReading()
        {
            ;
        }
    }
}
