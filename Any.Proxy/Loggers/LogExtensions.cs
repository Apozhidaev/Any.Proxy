using System;
using System.Diagnostics;
using Any.Logs;
using Any.Logs.Extentions;

namespace Any.Proxy.Loggers
{
    public static class LogExtensions
    {
        public static void Error(this Log log, string summary, Guid transactionId, params object[] values)
        {
            summary = summary.Format(values);
            var description = new StackTrace(1).ToString();
            log.WriteAsync<EventLogger>(logger => logger.WriteAsync(summary, description, EventType.Error, transactionId));
        }

        public static void Error(this Log log, Exception e, string summary, Guid transactionId, params object[] values)
        {
            summary = summary.Format(values);
            var description = e.GetFullMessage();
            log.WriteAsync<EventLogger>(logger => logger.WriteAsync(summary, description, EventType.Error, transactionId));
        }

        public static void Info(this Log log, string description, string summary, Guid transactionId, params object[] values)
        {
            summary = summary.Format(values);
            log.WriteAsync<EventLogger>(logger => logger.WriteAsync(summary, description, EventType.Info, transactionId));
        }

        public static void Info(this Log log, string summary, Guid transactionId, params object[] values)
        {
            log.Info(String.Empty, summary, transactionId, values);
        }

        public static void BeginInfo(this Log log, string summary, Guid transactionId, params object[] values)
        {
            log.Info(String.Format("Begin {0}", summary), transactionId, values);
        }

        public static void BeginInfo(this Log log, string description, string summary, Guid transactionId, params object[] values)
        {
            log.Info(description, String.Format("Begin {0}", summary), transactionId, values);
        }

        public static void EndInfo(this Log log, string summary, Guid transactionId, params object[] values)
        {
            log.Info(String.Format("End {0}", summary), transactionId, values);
        }

        public static void EndInfo(this Log log, string description, string summary, Guid transactionId, params object[] values)
        {
            log.Info(description, String.Format("End {0}", summary), transactionId, values);
        }
    }
}