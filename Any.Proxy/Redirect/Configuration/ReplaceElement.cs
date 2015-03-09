using System.Configuration;

namespace Any.Proxy.Redirect.Configuration
{
    public class ReplaceElement : ConfigurationElement
    {
        [ConfigurationProperty("mediaType", IsRequired = true)]
        public string MediaType
        {
            get { return (string)this["mediaType"]; }
        }

        [ConfigurationProperty("oldValue", IsRequired = true)]
        public string OldValue
        {
            get { return (string)this["oldValue"]; }
        }

        [ConfigurationProperty("newValue", IsRequired = true)]
        public string NewValue
        {
            get { return (string)this["newValue"]; }
        }
    }
}