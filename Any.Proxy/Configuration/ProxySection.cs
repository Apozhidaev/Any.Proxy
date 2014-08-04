using System.Configuration;

namespace Any.Proxy.Configuration
{
    public class ProxySection : ConfigurationSection
    {
        private static readonly ConfigurationProperty PortMapProperty =
            new ConfigurationProperty(
                "portMap",
                typeof(PortMapElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        public ProxySection()
        {
            base.Properties.Add(PortMapProperty);
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