using System;
using System.Threading.Tasks;

namespace Any.Proxy
{
    public interface IBridge : IDisposable
    {
        Task HandshakeAsync();

        Task RelayAsync();

        void Relay();

        Task RelayToAsync();

        Task RelayFromAsync();
    }
}