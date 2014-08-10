using System.Net;
using Any.Proxy.Http.Configuration;

namespace Any.Proxy.Http
{
    public class HttpModule : IProxyModule
    {
        private readonly HttpUnit _httpUnit;
        private readonly HttpsUnit _httpsUnit;

        public HttpModule(HttpElement config)
        {
            _httpUnit = new HttpUnit(IPAddress.Any, 50000);
            _httpsUnit = new HttpsUnit(IPAddress.Any, 51111);
        }

        public void Dispose()
        {
            _httpUnit.Dispose();
            _httpsUnit.Dispose();
        }

        public void Start()
        {
            _httpUnit.Start();
            _httpsUnit.Start();
        }
    }
}