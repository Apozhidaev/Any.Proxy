using System;

namespace Ap.Proxy
{
    public interface IProxyModule : IDisposable
    {
        void Start();
    }
}