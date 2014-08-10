using System.Configuration;

namespace Any.Proxy.HttpService.Configuration
{
    public class HttpServiceElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string) this["name"]; }
        }

        [ConfigurationProperty("prefixes")]
        public string Prefixes
        {
            get { return (string)this["prefixes"]; }
        }
    }
}