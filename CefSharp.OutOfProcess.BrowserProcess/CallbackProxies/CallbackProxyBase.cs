using System;
using System.Collections.Generic;
using CefSharp.OutOfProcess.Interface;

namespace CefSharp.OutOfProcess.BrowserProcess.CallbackProxies
{
    internal class CallbackProxyBase<T> : IDisposable
    {
        private int id = 0;
        private readonly Dictionary<int, T> callbacks = new Dictionary<int, T>();
        private protected readonly IOutOfProcessHostRpc host;

        public CallbackProxyBase(IOutOfProcessHostRpc host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        protected int CreateCallback(T callback)
        {
            int lid = id++;
            callbacks.Add(lid, callback);
            return lid;
        }

        protected T GetCallback(int id)
        {
            T cb = callbacks[id];
            callbacks.Remove(id);
            return cb;
        }

        public void Dispose()
        {
            foreach (var cbs in callbacks)
            {
                if (cbs.Value is IDisposable d)
                {
                    d.Dispose();
                }
            }

            callbacks.Clear();
        }
    }
}
