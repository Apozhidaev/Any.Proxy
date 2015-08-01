using System.Xml.Serialization;

namespace Ap.Proxy.PortMap.Configuration
{
    public class PortMapConfig
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("fromHost")]
        public string FromHost { get; set; }

        [XmlAttribute("fromPort")]
        public int FromPort { get; set; }

        [XmlAttribute("toHost")]
        public string ToHost { get; set; }

        [XmlAttribute("toPort")]
        public int ToPort { get; set; }
    }
}