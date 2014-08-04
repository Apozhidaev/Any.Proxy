using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using Any.Proxy.PortMap;
using Any.Logs;
using Any.Logs.Loggers;
using Any.Proxy.PortMap.Configuration;

namespace Any.Proxy
{
    public class Proxy
    {
        private readonly Dictionary<string, PortMapListener> _listeners = new Dictionary<string, PortMapListener>();

        public void Start()
        {
            Log.Out.InitializeDefault();
            var configuration = (ProxySection)ConfigurationManager.GetSection("proxy");

            foreach (var listener in configuration.PortMap.Cast<PortMapElement>())
            {
                _listeners.Add(String.Format("PortMap-{0}", listener.Name), new PortMapListener(new IPEndPoint(GetIP(listener.FromHost), listener.FromPort),
                    new IPEndPoint(GetIP(listener.ToHost), listener.ToPort)));
            }

            foreach (var listener in _listeners)
            {
                listener.Value.Start();
            }
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
