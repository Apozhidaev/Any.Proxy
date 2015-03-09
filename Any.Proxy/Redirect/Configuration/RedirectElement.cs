using System.Configuration;

namespace Any.Proxy.Redirect.Configuration
{
    public class RedirectElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string) this["name"]; }
        }

        [ConfigurationProperty("fromUrl")]
        public string FromUrl
        {
            get { return (string)this["fromUrl"]; }
        }

        [ConfigurationProperty("toUrl")]
        public string ToUrl
        {
            get { return (string)this["toUrl"]; }
        }
    }
}