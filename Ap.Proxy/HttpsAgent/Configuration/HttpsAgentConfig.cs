using System.Xml.Serialization;
using Ap.Proxy.Configuration;

namespace Ap.Proxy.HttpsAgent.Configuration
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