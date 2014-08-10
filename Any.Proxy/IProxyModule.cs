using System;

namespace Any.Proxy
{
    public interface IProxyModule : IDisposable
    {
        void Start();
    }
}