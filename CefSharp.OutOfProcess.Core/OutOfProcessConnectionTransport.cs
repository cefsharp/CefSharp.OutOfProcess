using CefSharp.Dom.Transport;
using System;
using System.Threading.Tasks;

namespace CefSharp.OutOfProcess
{
    public class OutOfProcessConnectionTransport : IConnectionTransport
    {
        public int BrowserId { get; }
        /// <inheritdoc/>
        public bool IsClosed { get; private set; }
        public OutOfProcessHost OutOfProcessHost { get; }

        /// <inheritdoc/>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        /// <inheritdoc/>
        public event EventHandler<MessageErrorEventArgs> MessageError;
        /// <inheritdoc/>
        public event EventHandler Disconnected;

        public OutOfProcessConnectionTransport(int browserId, OutOfProcessHost outOfProcessHost)
        {
            BrowserId = browserId;
            OutOfProcessHost = outOfProcessHost;
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            MessageReceived = null;
            MessageError = null;
            Disconnected = null;
        }

        /// <inheritdoc/>
        public void InvokeMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }

        /// <inheritdoc/>
        public Task SendAsync(string message)
        {
            return OutOfProcessHost.SendDevToolsMessageAsync(BrowserId, message);
        }

        /// <inheritdoc/>
        public void StopReading()
        {
            
        }
    }
}
