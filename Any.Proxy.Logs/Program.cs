using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace Any.Proxy.Logs
{
    class Program
    {
        static void Main(string[] args)
        {
            string logFile = Path.Combine(Environment.CurrentDirectory, "log.xml");
            if (File.Exists(logFile))
            {
                var logs = File.ReadAllText(logFile);
                var logsXml = String.Format("<?xml version=\"1.0\" encoding=\"utf-8\"?><logs>{0}</logs>", logs);
                var doc = XDocument.Parse(logsXml);
                if (doc.Root != null)
                {
                    using (var context = new PLogsEntities())
                    {
                        foreach (var xElement in doc.Root.Elements("log"))
                        {
                            var log = new SysEvent();
                            log.Type = Int32.Parse(xElement.Attribute("type").Value);
                            log.Time = DateTime.Parse(xElement.Attribute("time").Value, CultureInfo.InvariantCulture);
                            log.Context = xElement.Attribute("context").Value;
                            log.Summary = xElement.Element("summary").Value;
                            log.Description = xElement.Element("description").Value;
                            context.SysEvents.Add(log);
                        }
                        context.SaveChanges();
                    }
                    Console.WriteLine("Ok");
                }
                else
                {
                    Console.WriteLine("doc.Root is null");
                }
            }
            else
            {
                Console.WriteLine("File is not exist");
            }
            Console.ReadKey();
        }
    }
}
