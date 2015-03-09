using System.Configuration;
using Any.Proxy.Configuration;

namespace Any.Proxy.HttpsAgent.Configuration
{
    public class HttpsAgentElement : ConfigurationElement
    {
        private static readonly ConfigurationProperty HttpBridgeProperty =
           new ConfigurationProperty(
               "httpBridge",
               typeof(HttpBridgeElement),
               null,
               ConfigurationPropertyOptions.None);

        public HttpsAgentElement()
        {
            base.Properties.Add(HttpBridgeProperty);
        }

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string) this["name"]; }
        }

        [ConfigurationProperty("host")]
        public string Host
        {
            get { return (string) this["host"]; }
        }

        [ConfigurationProperty("port")]
        public int Port
        {
            get { return (int) this["port"]; }
        }

        [ConfigurationProperty("httpBridge")]
        public HttpBridgeElement HttpBridge
        {
            get { return (HttpBridgeElement)this["httpBridge"]; }
        }
    }
}