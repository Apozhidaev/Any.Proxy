using System.Configuration;

namespace Any.Proxy.HttpsAgent.Configuration
{
    [ConfigurationCollection(typeof (HttpsAgentElement), AddItemName = "module",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class HttpsAgentElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new HttpsAgentElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HttpsAgentElement) element).Name;
        }
    }
}