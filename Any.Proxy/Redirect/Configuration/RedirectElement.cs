using System.Configuration;

namespace Any.Proxy.Redirect.Configuration
{
    public class RedirectElement : ConfigurationElement
    {
        private static readonly ConfigurationProperty ReplaceProperty =
            new ConfigurationProperty(
                "replaces",
                typeof(ReplaceElementCollection),
                null,
                ConfigurationPropertyOptions.None);

        public RedirectElement()
        {
            base.Properties.Add(ReplaceProperty);
        }

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

        [ConfigurationProperty("replaces", IsRequired = false)]
        public ReplaceElementCollection Replace
        {
            get { return (ReplaceElementCollection)this[ReplaceProperty]; }
        }
    }
}