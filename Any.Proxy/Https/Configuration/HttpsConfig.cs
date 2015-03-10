using System.Xml.Serialization;

namespace Any.Proxy.Https.Configuration
{
    public class HttpsConfig
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("host")]
        public string Host { get; set; }

        [XmlAttribute("port")]
        public int Port { get; set; }
    }
}