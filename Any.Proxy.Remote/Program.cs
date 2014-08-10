using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Any.Proxy.Remote
{
    class Program
    {
        static void Main(string[] args)
        {
            var rc = new RemoteControl("http://lifehttp.com/");
            rc.Start();
            Console.ReadKey();
            rc.Stop();
        }
    }
}
