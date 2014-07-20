using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using Any.Proxy.Configuration;
using Any.Proxy.PortMap;
using HttpListener = Any.Proxy.Http.HttpListener;

namespace Any.Proxy
{
    public class Proxy
    {
        private readonly Dictionary<string, ListenerBase> _listeners = new Dictionary<string, ListenerBase>();

        public void Start()
        {
            var configuration = (ProxySection)ConfigurationManager.GetSection("proxy");

            foreach (var listener in configuration.Http.Cast<HttpElement>())
            {
                _listeners.Add(String.Format("Http-{0}", listener.Name), new HttpListener(GetIP(listener.Host), listener.Port));
            }

            foreach (var listener in configuration.PortMap.Cast<PortMapElement>())
            {
                _listeners.Add(String.Format("PortMap-{0}", listener.Name), new PortMapListener(GetIP(listener.FromHost), listener.FromPort, GetIP(listener.ToHost), listener.ToPort));
            }

            foreach (var listener in _listeners)
            {
                listener.Value.Start();
            }

            Console.WriteLine("Hi I'm AnyProxy");
            while (Console.ReadLine() != "exit") { }
            Stop();
        }

        public void Stop()
        {
            foreach (var listener in _listeners)
            {
                listener.Value.Dispose();
            }
        }

        private static IPAddress GetIP(string host)
        {
            return !String.IsNullOrEmpty(host) ? Dns.GetHostAddresses(host)[0] : IPAddress.Any;
        }
    }
}
