using Topshelf;

namespace Ap.Proxy.Api
{
    public class Program
    {
        public static void Main()
        {
            HostFactory.Run(x =>
            {
                x.Service<RemoteControl>(s =>
                {
                    s.ConstructUsing(name => new RemoteControl());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Any Proxy");
                x.SetDisplayName("Any Proxy");
                x.SetServiceName("AnyProxy");
            });
        }
    }
}