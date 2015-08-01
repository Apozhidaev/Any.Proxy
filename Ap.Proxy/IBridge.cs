using System;
using System.Threading.Tasks;

namespace Ap.Proxy
{
    public interface IBridge : IDisposable
    {
        Task HandshakeAsync();

        Task RelayAsync();

        Task RelayToAsync();

        Task RelayFromAsync();

        Task WriteAsync(byte[] bytes);
    }
}