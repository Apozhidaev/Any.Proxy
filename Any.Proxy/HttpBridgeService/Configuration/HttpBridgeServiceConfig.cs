using System.Xml.Serialization;

namespace Any.Proxy.HttpBridgeService.Configuration
{
    public class HttpBridgeServiceConfig
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("prefixes")]
        public string Prefixes { get; set; }
    }
}