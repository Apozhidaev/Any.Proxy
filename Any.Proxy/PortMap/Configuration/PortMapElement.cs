using System.Configuration;

namespace Any.Proxy.PortMap.Configuration
{
    public class PortMapElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string) this["name"]; }
        }

        [ConfigurationProperty("fromHost")]
        public string FromHost
        {
            get { return (string) this["fromHost"]; }
        }

        [ConfigurationProperty("fromPort")]
        public int FromPort
        {
            get { return (int) this["fromPort"]; }
        }

        [ConfigurationProperty("toHost")]
        public string ToHost
        {
            get { return (string) this["toHost"]; }
        }

        [ConfigurationProperty("toPort")]
        public int ToPort
        {
            get { return (int) this["toPort"]; }
        }
    }
}