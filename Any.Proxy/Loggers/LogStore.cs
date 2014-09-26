using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Any.Logs.Extentions;

namespace Any.Proxy.Loggers
{
    public class LogStore
    {
        private static readonly object _sync = new object();

        private readonly static string _filename = Path.Combine(Environment.CurrentDirectory, "log.xml");

        public static void Push(string summary, string description, DateTime time, EventType type, string context)
        {
            lock (_sync)
            {
                var log = new StringBuilder();
                log.AppendFormat("{0}<log time=\"{1}\" type=\"{2}\" context=\"{3}\">", Environment.NewLine, time.ToString(CultureInfo.InvariantCulture), (int)type, context);
                log.AppendFormat("{0}{1}", Environment.NewLine, new XElement("summary", summary));
                log.AppendFormat("{0}{1}", Environment.NewLine, new XElement("description", description));
                log.AppendFormat("{0}</log>", Environment.NewLine);
                try
                {
                    File.AppendAllText(_filename, log.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.GetFullMessage());
                }
            }
        }
    }
}