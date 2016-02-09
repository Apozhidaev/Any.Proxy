using System.Xml.Serialization;
using Ap.Proxy.Http.Configuration;
using Ap.Proxy.HttpAgent.Configuration;
using Ap.Proxy.HttpBridgeService.Configuration;
using Ap.Proxy.PortMap.Configuration;

namespace Ap.Proxy.Configuration
{
    [XmlRoot("proxy")]
    public class ProxyConfig
    {
        public static ProxyConfig Load()
        {
            return XmlHelper.Deserialize<ProxyConfig>("proxy.xml");
        }

        [XmlArray("portMap")]
        [XmlArrayItem("module")]
        public PortMapConfig[] PortMap { get; set; }

        [XmlArray("http")]
        [XmlArrayItem("module")]
        public HttpConfig[] Http { get; set; }

        [XmlArray("httpAgent")]
        [XmlArrayItem("module")]
        public HttpAgentConfig[] HttpAgent { get; set; }

        [XmlArray("httpBridgeService")]
        [XmlArrayItem("module")]
        public HttpBridgeServiceConfig[] HttpBridgeService { get; set; }


        public static void CreateTemplate()
        {
            var config = new ProxyConfig
            {
                Http = new[]
                {
                    new HttpConfig
                    {
                        Name = "test",
                        Host = "",
                        Port = 50001
                    }
                },
                HttpAgent = new[]
                {
                    new HttpAgentConfig
                    {
                        Name = "test",
                        Host = "",
                        Port = 50001,
                        HttpBridge = new HttpBridgeConfig
                        {
                            Url = "http://hashabc.com/",
                            UseProxy = false,
                            Proxy = "inet-proxy-b.sputnik.loadb.ubs.net:8085",
                            UseDefaultCredentials = true,
                            UserName = "",
                            Password = "",
                        }
                    }
                },
                HttpBridgeService = new[]
                {
                    new HttpBridgeServiceConfig
                    {
                        Name = "test",
                        Prefixes = "http://hashabc.com/"
                    }
                },
                PortMap = new[]
                {
                    new PortMapConfig
                    {
                        Name = "test",
                        FromHost = "localhost",
                        FromPort = 52271,
                        ToHost = "localhost",
                        ToPort = 1923
                    }
                }
            };

            XmlHelper.Serialize("proxy.xml", config);
        }
    }
}