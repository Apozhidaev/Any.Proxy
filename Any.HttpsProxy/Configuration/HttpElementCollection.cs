using System.Configuration;

namespace Any.Proxy.Configuration
{
    [ConfigurationCollection(typeof(HttpElement), AddItemName = "listener",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class HttpElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new HttpElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HttpElement) element).Name;
        }
    }
}