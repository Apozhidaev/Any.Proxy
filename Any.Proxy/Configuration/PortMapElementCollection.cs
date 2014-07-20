using System.Configuration;

namespace Any.Proxy.Configuration
{
    [ConfigurationCollection(typeof(HttpElement), AddItemName = "listener",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class PortMapElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new PortMapElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PortMapElement)element).Name;
        }
    }
}