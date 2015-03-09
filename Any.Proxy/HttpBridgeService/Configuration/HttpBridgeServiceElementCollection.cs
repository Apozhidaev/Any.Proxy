using System.Configuration;

namespace Any.Proxy.HttpBridgeService.Configuration
{
    [ConfigurationCollection(typeof (HttpBridgeServiceElement), AddItemName = "module",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class HttpBridgeServiceElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new HttpBridgeServiceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HttpBridgeServiceElement) element).Name;
        }
    }
}