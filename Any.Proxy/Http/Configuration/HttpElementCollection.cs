using System.Configuration;

namespace Any.Proxy.Http.Configuration
{
    [ConfigurationCollection(typeof (HttpElement), AddItemName = "module",
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