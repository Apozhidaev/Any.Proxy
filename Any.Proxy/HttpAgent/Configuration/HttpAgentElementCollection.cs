using System.Configuration;

namespace Any.Proxy.HttpAgent.Configuration
{
    [ConfigurationCollection(typeof (HttpAgentElement), AddItemName = "module",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class HttpAgentElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new HttpAgentElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HttpAgentElement) element).Name;
        }
    }
}