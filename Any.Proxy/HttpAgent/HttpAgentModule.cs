using System.Net;

namespace Any.Proxy.HttpAgent
{
    public class HttpAgentModule : IProxyModule
    {
        private readonly HttpAgentUnit _httpUnit;
        private readonly HttpsAgentUnit _httpsUnit;

        public HttpAgentModule()
        {
            _httpUnit = new HttpAgentUnit(IPAddress.Any, 50000);
            _httpsUnit = new HttpsAgentUnit(IPAddress.Any, 51111);
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