using System;
using System.Net;
using Any.Proxy.HttpAgent;
using Any.Proxy.Loggers;
using Topshelf;

namespace Any.Proxy
{
    public class Program
    {
        public static void Main()
        {
            //HostFactory.Run(x =>
            //{
            //    x.Service<Proxy>(s =>
            //    {
            //        s.ConstructUsing(name => new Proxy());
            //        s.WhenStarted(tc => tc.Start());
            //        s.WhenStopped(tc => tc.Stop());
            //    });
            //    x.RunAsLocalSystem();

            //    x.SetDescription("Any Proxy");
            //    x.SetDisplayName("AnyProxy");
            //    x.SetServiceName("AnyProxy");
            //});

            LogDB.Initialize();
            LogDB.Push("dgd", "dgsdg", DateTime.Now, EventType.Error, Guid.NewGuid().ToString());

            //var d = new DbFacade();
            //d.CreateDatabase();

            //var proxy = new Proxy();
            //proxy.Start();
            //Console.ReadKey();
            //proxy.Stop();
        }
    }
}