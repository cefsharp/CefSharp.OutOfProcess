namespace CefSharp.OutOfProcess
{
    using System;
    using CefSharp.OutOfProcess.Internal;

    internal class CallbackProxyBase : IDisposable
    {
        private protected readonly OutOfProcessHost outOfProcessHost;
        private protected readonly int callback;
        private protected readonly IChromiumWebBrowserInternal chromiumWebBrowser;
        private bool disposedValue;

        public CallbackProxyBase(OutOfProcessHost outOfProcessHost, int callback, IChromiumWebBrowserInternal chromiumWebBrowser)
        {
            this.chromiumWebBrowser = chromiumWebBrowser;
            this.outOfProcessHost = outOfProcessHost;
            this.callback = callback;
        }

        public bool IsDisposed => disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
