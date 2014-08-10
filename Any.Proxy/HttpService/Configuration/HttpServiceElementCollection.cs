using System.Configuration;

namespace Any.Proxy.HttpService.Configuration
{
    [ConfigurationCollection(typeof (HttpServiceElement), AddItemName = "module",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class HttpServiceElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new HttpServiceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HttpServiceElement) element).Name;
        }
    }
}