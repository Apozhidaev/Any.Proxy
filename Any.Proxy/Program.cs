using Any.Logs;
using Any.Logs.Loggers;
using System;
using Topshelf;

namespace Any.Proxy
{
    public class Program
    {
        public static void Main()
        {
            HostFactory.Run(x =>
            {
                x.Service<Proxy>(s =>
                {
                    s.ConstructUsing(name => new Proxy());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Any Proxy");
                x.SetDisplayName("AnyProxy");
                x.SetServiceName("AnyProxy");
            });

            Console.ReadKey();
        } 
    }
}