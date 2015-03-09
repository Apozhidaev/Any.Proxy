using System.Configuration;

namespace Any.Proxy.Https.Configuration
{
    [ConfigurationCollection(typeof (HttpsElement), AddItemName = "module",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class HttpsElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new HttpsElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HttpsElement) element).Name;
        }
    }
}