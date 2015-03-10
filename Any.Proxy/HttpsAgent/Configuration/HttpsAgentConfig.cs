using System.Xml.Serialization;
using Any.Proxy.Configuration;
using Any.Proxy.HttpBridgeService.Configuration;

namespace Any.Proxy.HttpsAgent.Configuration
{
    public class HttpsAgentConfig
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("host")]
        public string Host { get; set; }

        [XmlAttribute("port")]
        public int Port { get; set; }

        [XmlElement("httpBridge")]
        public HttpBridgeConfig HttpBridge { get; set; }
    }
}