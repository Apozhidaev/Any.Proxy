using System;

namespace Any.Proxy
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                new Proxy().Start();
            }
            catch
            {
                Console.WriteLine("The program ended abnormally!");
            }
        } 
    }
}