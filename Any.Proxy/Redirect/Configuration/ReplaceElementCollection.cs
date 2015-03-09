using System;
using System.Configuration;

namespace Any.Proxy.Redirect.Configuration
{
    [ConfigurationCollection(typeof(ReplaceElement), AddItemName = "replace",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class ReplaceElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ReplaceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var replace = (ReplaceElement) element;
            return String.Format("{0}{1}{0}", replace.MediaType, replace.OldValue);
        }
    }
}