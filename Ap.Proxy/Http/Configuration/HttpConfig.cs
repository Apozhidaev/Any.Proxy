using System.Xml.Serialization;

namespace Ap.Proxy.Http.Configuration
{
    public class HttpConfig
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("host")]
        public string Host { get; set; }

        [XmlAttribute("port")]
        public int Port { get; set; }
    }
}