using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Any.Proxy.Configuration
{
    public class HttpBridgeElement : ConfigurationElement
    {
        [ConfigurationProperty("url")]
        public string Url
        {
            get { return (string)this["url"]; }
        }

        [ConfigurationProperty("useProxy")]
        public bool UseProxy
        {
            get { return (bool)this["useProxy"]; }
        }

        [ConfigurationProperty("proxy")]
        public string Proxy
        {
            get { return (string)this["proxy"]; }
        }

        [ConfigurationProperty("useDefaultCredentials")]
        public bool UseDefaultCredentials
        {
            get { return (bool)this["useDefaultCredentials"]; }
        }

        [ConfigurationProperty("userName")]
        public string UserName
        {
            get { return (string)this["userName"]; }
        }

        [ConfigurationProperty("password")]
        public string Password
        {
            get { return (string)this["password"]; }
        }
    }
}
