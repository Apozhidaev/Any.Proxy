using System;
using System.Threading.Tasks;

namespace Any.Proxy
{
    public interface IBridge : IDisposable
    {
        Task HandshakeAsync();

        Task RelayAsync();

        Task RelayToAsync();

        Task RelayFromAsync();
    }
}