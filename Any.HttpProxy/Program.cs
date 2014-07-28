using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Any.HttpProxy
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
