using System.Configuration;
using Any.Proxy.Http.Configuration;
using Any.Proxy.HttpAgent.Configuration;
using Any.Proxy.HttpService.Configuration;
using Any.Proxy.PortMap.Configuration;

namespace Any.Proxy.Configuration
{
    public class ProxySection : ConfigurationSection
    {
        private static readonly ConfigurationProperty PortMapProperty =
            new ConfigurationProperty(
                "portMap",
                typeof (PortMapElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty HttpProperty =
            new ConfigurationProperty(
                "http",
                typeof(HttpElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty HttpAgentProperty =
            new ConfigurationProperty(
                "httpAgent",
                typeof(HttpAgentElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty HttpServiceProperty =
            new ConfigurationProperty(
                "httpService",
                typeof(HttpServiceElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        public ProxySection()
        {
            base.Properties.Add(PortMapProperty);
            base.Properties.Add(HttpProperty);
            base.Properties.Add(HttpAgentProperty);
            base.Properties.Add(HttpServiceProperty);
        }


        [ConfigurationProperty("portMap", IsRequired = false)]
        public PortMapElementCollection PortMap
        {
            get { return (PortMapElementCollection) this[PortMapProperty]; }
        }


        [ConfigurationProperty("http", IsRequired = false)]
        public HttpElementCollection Http
        {
            get { return (HttpElementCollection)this[HttpProperty]; }
        }


        [ConfigurationProperty("httpAgent", IsRequired = false)]
        public HttpAgentElementCollection HttpAgent
        {
            get { return (HttpAgentElementCollection)this[HttpAgentProperty]; }
        }


        [ConfigurationProperty("httpService", IsRequired = false)]
        public HttpServiceElementCollection HttpService
        {
            get { return (HttpServiceElementCollection)this[HttpServiceProperty]; }
        }
    }
}