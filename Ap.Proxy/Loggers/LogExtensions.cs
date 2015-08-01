using System;
using System.Diagnostics;
using System.Text;
using Ap.Logs;

namespace Ap.Proxy.Loggers
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

        public static void BeginInfo(this Log log, string context, string description, string summary, params object[] values)
        {
            log.Info(context, description, String.Format("Begin {0}", summary), values);
        }

        public static void EndInfo(this Log log, string context, string description, string summary, params object[] values)
        {
            log.Info(context, description, String.Format("End {0}", summary), values);
        }

        public static string GetFullMessage(this Exception exception)
        {
            var fullMessage = new StringBuilder();
            var aggr = exception as AggregateException;
            if (aggr != null)
            {
                foreach (Exception innerException in aggr.InnerExceptions)
                {
                    fullMessage.Append(innerException.GetFullMessage());
                }
            }
            else
            {
                while (exception != null)
                {
                    fullMessage.Append(String.Format("{2}---{0}---{2}{1}{2}", exception.Message, exception.StackTrace,
                        Environment.NewLine));
                    exception = exception.InnerException;
                }
            }
            return fullMessage.ToString();
        }

        public static string Format(this string str, object[] values)
        {
            return values.Length > 0 ? String.Format(str, values) : str;
        }
    }
}