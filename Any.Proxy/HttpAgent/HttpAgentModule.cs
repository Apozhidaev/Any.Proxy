using System.Net;
using Any.Proxy.HttpAgent.Configuration;

namespace Any.Proxy.HttpAgent
{
    public class HttpAgentModule : IProxyModule
    {
        private readonly HttpAgentUnit _httpUnit;
        private readonly HttpsAgentUnit _httpsUnit;

        public HttpAgentModule(HttpAgentElement config)
        {
            var ip = Proxy.GetIP(config.Host);
            _httpUnit = new HttpAgentUnit(ip, config.Port, config.Url);
            _httpsUnit = new HttpsAgentUnit(ip, config.SslPort, config.Url);
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