using System;
using System.Threading.Tasks;
using Any.Logs;

namespace Any.Proxy.Loggers
{
    public class EventLogger : ILogger
    {
        public EventLogger()
        {
            LogDB.Initialize();
        }

        public Task WriteAsync(string summary, string description, EventType type, Guid transactionId)
        {
            return Task.Run(() => LogDB.Push(summary, description, DateTime.Now, type, transactionId));
        }

        public void Flush() { }

        public bool IsEnabledFor(string method) { return true; }
    }
}
