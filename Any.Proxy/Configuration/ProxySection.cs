using System.Configuration;
using Any.Proxy.Http.Configuration;
using Any.Proxy.HttpAgent.Configuration;
using Any.Proxy.HttpBridgeService.Configuration;
using Any.Proxy.Https.Configuration;
using Any.Proxy.HttpsAgent.Configuration;
using Any.Proxy.PortMap.Configuration;
using Any.Proxy.Redirect.Configuration;

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

        private static readonly ConfigurationProperty HttpsProperty =
            new ConfigurationProperty(
                "https",
                typeof(HttpsElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty HttpAgentProperty =
            new ConfigurationProperty(
                "httpAgent",
                typeof(HttpAgentElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty HttpsAgentProperty =
            new ConfigurationProperty(
                "httpsAgent",
                typeof(HttpsAgentElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty HttpBridgeServiceProperty =
           new ConfigurationProperty(
               "httpBridgeService",
               typeof(HttpBridgeServiceElementCollection),
               null,
               ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty RedirectProperty =
          new ConfigurationProperty(
              "redirect",
              typeof(RedirectElementCollection),
              null,
              ConfigurationPropertyOptions.None);


        public ProxySection()
        {
            base.Properties.Add(PortMapProperty);
            base.Properties.Add(HttpProperty);
            base.Properties.Add(HttpsProperty);
            base.Properties.Add(HttpAgentProperty);
            base.Properties.Add(HttpsAgentProperty);
            base.Properties.Add(HttpBridgeServiceProperty);
            base.Properties.Add(RedirectProperty);
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

        [ConfigurationProperty("https", IsRequired = false)]
        public HttpsElementCollection Https
        {
            get { return (HttpsElementCollection)this[HttpsProperty]; }
        }


        [ConfigurationProperty("httpAgent", IsRequired = false)]
        public HttpAgentElementCollection HttpAgent
        {
            get { return (HttpAgentElementCollection)this[HttpAgentProperty]; }
        }

        [ConfigurationProperty("httpsAgent", IsRequired = false)]
        public HttpsAgentElementCollection HttpsAgent
        {
            get { return (HttpsAgentElementCollection)this[HttpsAgentProperty]; }
        }

        [ConfigurationProperty("httpBridgeService", IsRequired = false)]
        public HttpBridgeServiceElementCollection HttpBridgeService
        {
            get { return (HttpBridgeServiceElementCollection)this[HttpBridgeServiceProperty]; }
        }

        [ConfigurationProperty("redirect", IsRequired = false)]
        public RedirectElementCollection Redirect
        {
            get { return (RedirectElementCollection)this[RedirectProperty]; }
        }
    }
}