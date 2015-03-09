using System.Configuration;

namespace Any.Proxy.Redirect.Configuration
{
    [ConfigurationCollection(typeof (RedirectElement), AddItemName = "module",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class RedirectElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new RedirectElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((RedirectElement) element).Name;
        }
    }
}