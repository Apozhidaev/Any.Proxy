using Topshelf;

namespace Ap.Proxy.Api
{
    public class Program
    {
        public static void Main()
        {
            HostFactory.Run(x =>
            {
                x.Service<App>(s =>
                {
                    s.ConstructUsing(name => new App());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Ap.Proxy");
                x.SetDisplayName("Ap.Proxy");
                x.SetServiceName("Ap.Proxy");
            });
        }
    }
}