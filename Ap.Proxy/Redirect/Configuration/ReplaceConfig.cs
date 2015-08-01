using System.Xml.Serialization;

namespace Ap.Proxy.Redirect.Configuration
{
    public class ReplaceConfig
    {
        [XmlAttribute("mediaType")]
        public string MediaType { get; set; }

        [XmlAttribute("oldValue")]
        public string OldValue { get; set; }

        [XmlAttribute("newValue")]
        public string NewValue { get; set; }
    }
}