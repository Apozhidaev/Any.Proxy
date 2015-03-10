using System.Xml.Serialization;

namespace Any.Proxy.Configuration
{
    public class HttpBridgeConfig
    {
        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlAttribute("useProxy")]
        public bool UseProxy { get; set; }

        [XmlAttribute("proxy")]
        public string Proxy { get; set; }

        [XmlAttribute("useDefaultCredentials")]
        public bool UseDefaultCredentials { get; set; }

        [XmlAttribute("userName")]
        public string UserName { get; set; }

        [XmlAttribute("password")]
        public string Password { get; set; }
    }
}
