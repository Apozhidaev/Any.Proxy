using System.Configuration;

namespace Any.Proxy.HttpBridgeService.Configuration
{
    public class HttpBridgeServiceElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string) this["name"]; }
        }

        [ConfigurationProperty("prefixes", IsRequired = true)]
        public string Prefixes
        {
            get { return (string)this["prefixes"]; }
        }
    }
}