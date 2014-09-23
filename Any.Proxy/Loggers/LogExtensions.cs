using System;
using System.Diagnostics;
using Any.Logs;
using Any.Logs.Extentions;

namespace Any.Proxy.Loggers
{
    public static class LogExtensions
    {
        public static void Error(this Log log, string context, string summary, params object[] values)
        {
            summary = summary.Format(values);
            var description = new StackTrace(1).ToString();
            log.WriteAsync<EventLogger>(logger => logger.WriteAsync(summary, description, EventType.Error, context));
        }

        public static void Error(this Log log, Exception e, string context, string summary, params object[] values)
        {
            summary = summary.Format(values);
            var description = e.GetFullMessage();
            log.WriteAsync<EventLogger>(logger => logger.WriteAsync(summary, description, EventType.Error, context));
        }

        public static void Info(this Log log, string context, string description, string summary, params object[] values)
        {
            summary = summary.Format(values);
            log.WriteAsync<EventLogger>(logger => logger.WriteAsync(summary, description, EventType.Info, context));
        }
    }
}