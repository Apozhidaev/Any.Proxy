using System.Configuration;

namespace Any.Proxy.PortMaps.Configuration
{
    [ConfigurationCollection(typeof(PortMapElement), AddItemName = "listener",
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