using System.Xml.Serialization;

namespace Any.Proxy.Redirect.Configuration
{
    public class RedirectConfig
    {

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("fromUrl")]
        public string FromUrl{ get; set; }

        [XmlAttribute("toUrl")]
        public string ToUrl { get; set; }

        [XmlElement("replace")]
        public ReplaceConfig[] Replace { get; set; }
    }
}