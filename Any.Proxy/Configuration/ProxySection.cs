using System.Configuration;

namespace Any.Proxy.Configuration
{
    public class ProxySection : ConfigurationSection
    {
          private static readonly ConfigurationProperty HttpProperty =
            new ConfigurationProperty(
                "http",
                typeof (HttpElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty PortMapProperty =
            new ConfigurationProperty(
                "portMap",
                typeof(PortMapElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        public ProxySection()
        {
            base.Properties.Add(HttpProperty);
            base.Properties.Add(PortMapProperty);
        }

        [ConfigurationProperty("http", IsRequired = false)]
        public HttpElementCollection Http
        {
            get
            {
                return (HttpElementCollection) this[HttpProperty];
            }
        }

        [ConfigurationProperty("portMap", IsRequired = false)]
        public PortMapElementCollection PortMap
        {
            get
            {
                return (PortMapElementCollection)this[PortMapProperty];
            }
        }
    }
}