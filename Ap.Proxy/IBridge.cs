using System;
using System.Threading.Tasks;

namespace Ap.Proxy
{
    public interface IBridge : IDisposable
    {
        bool Connected { get; }

        Task HandshakeAsync(string connectionId, string host, int port);

        Task<byte[]> ReadToAsync(Func<string, bool> end);

        Task RelayAsync();

        Task RelayToAsync();

        Task RelayFromAsync();

        Task RelayFromAsync(Func<string, bool> end);

        Task WriteFromAsync(byte[] bytes);

        Task WriteToAsync(byte[] bytes);
    }
}