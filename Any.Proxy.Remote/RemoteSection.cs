using System.Configuration;

namespace Any.Proxy.Remote
{
    public class RemoteSection : ConfigurationSection
    {
        [ConfigurationProperty("password", IsRequired = true)]
        public string Password
        {
            get { return (string)this["password"]; }
        }

        [ConfigurationProperty("prefixes", IsRequired = true)]
        public string Prefixes
        {
            get { return (string)this["prefixes"]; }
        }

        [ConfigurationProperty("process", IsRequired = true)]
        public string ProcessPath
        {
            get { return (string)this["process"]; }
        }
    }
}