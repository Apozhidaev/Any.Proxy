using System;
using System.Configuration;

namespace Any.Proxy.Remote
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = (RemoteSection)ConfigurationManager.GetSection("remote");
            var rc = new RemoteControl(configuration);
            rc.Start();
            Console.ReadKey();
            rc.Stop();
        }
    }
}
