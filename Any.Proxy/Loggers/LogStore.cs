using System;
using System.IO;
using Any.Logs.Extentions;

namespace Any.Proxy.Loggers
{
    public class LogStore
    {
        private static readonly object _sync = new object();

        private readonly static string _filename = Path.Combine(Environment.CurrentDirectory, "log.xml");

        public static void Initialize()
        {
            if (File.Exists(_filename)) return;
            try
            {
                File.AppendAllText(_filename, "<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetFullMessage());
            }
            
        }

        public static void Push(string summary, string description, DateTime time, EventType type, string connectionId)
        {
            const string log = "{0}<log summary=\"{1}\" description=\"{2}\" time=\"{3}\" type=\"{4}\" connectionId=\"{5}\"/>";
            lock (_sync)
            {
                try
                {
                    File.AppendAllText(_filename, String.Format(log, Environment.NewLine, summary, description, time, type, connectionId));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.GetFullMessage());
                }
            }
        }
    }
}