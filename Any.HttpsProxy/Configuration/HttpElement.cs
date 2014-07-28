using System.Configuration;

namespace Any.Proxy.Configuration
{
    public class HttpElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
        }

        [ConfigurationProperty("host")]
        public string Host
        {
            get { return (string)this["host"]; }
        }

        [ConfigurationProperty("port")]
        public int Port
        {
            get { return (int)this["port"]; }
        }

    }
}