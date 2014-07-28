using Any.Logs;
using Any.Logs.Loggers;

namespace Any.Proxy
{
    public class Program
    {
        public static void Main()
        {
            Log.Out.InitializeDefault();
            new Proxy().Start();
        } 
    }
}