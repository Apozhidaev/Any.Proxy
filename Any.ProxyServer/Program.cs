using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Any.ProxyServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new Listener();
            listener.Start();
            Console.ReadKey();
            listener.Stop();
        }
    }
}
