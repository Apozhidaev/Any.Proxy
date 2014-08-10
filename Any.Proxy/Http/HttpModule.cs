using Any.Proxy.Http.Configuration;

namespace Any.Proxy.Http
{
    public class HttpModule : IProxyModule
    {
        private readonly HttpUnit _httpUnit;
        private readonly HttpsUnit _httpsUnit;

        public HttpModule(HttpElement config)
        {
            var ip = Proxy.GetIP(config.Host);
            _httpUnit = new HttpUnit(ip, config.Port);
            _httpsUnit = new HttpsUnit(ip, config.SslPort);
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