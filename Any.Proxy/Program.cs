using System;

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

            

            var proxy = new Proxy();
            proxy.Start();
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => proxy.Stop();
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => proxy.Stop();
            Console.WriteLine("Working...");
            Console.ReadKey();
        }
    }
}