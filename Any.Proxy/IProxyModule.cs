using System;
using System.Collections;

namespace Any.Proxy
{
    public interface IProxyModule : IDisposable
    {
        void Start();
    }
}