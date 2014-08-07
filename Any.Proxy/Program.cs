using System;
using System.Net;
using Any.Proxy.Http;
using Any.Proxy.Https;
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

            var ms = new HttpsModule(IPAddress.Any, 51111);
            ms.Start();

            var m = new HttpModule(IPAddress.Any, 50000);
            m.Start();
            Console.ReadKey();
            m.Dispose();
            ms.Dispose();
        } 
    }
}