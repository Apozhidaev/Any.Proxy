using System;
using System.Collections.Generic;
using System.Net;
using Ap.Logs;
using Ap.Proxy.Configuration;
using Ap.Proxy.Http;
using Ap.Proxy.HttpAgent;
using Ap.Proxy.HttpBridgeService;
using Ap.Proxy.Loggers;
using Ap.Proxy.PortMap;

namespace Ap.Proxy
{
    public class Proxy
    {
        private readonly Dictionary<string, IProxyModule> _listeners = new Dictionary<string, IProxyModule>();

        public void Start()
        {
            Log.Initialize(new EventLogger());
            var configuration = ProxyConfig.Load();
            if (configuration.PortMap != null)
            {
                foreach (var config in configuration.PortMap)
                {
                    _listeners.Add($"PortMap-{config.Name}", new PortMapModule(config));
                }
            }
            if (configuration.Http != null)
            {
                foreach (var config in configuration.Http)
                {
                    _listeners.Add($"Http-{config.Name}", new HttpModule(config));
                }
            }
            if (configuration.HttpAgent != null)
            {
                foreach (var config in configuration.HttpAgent)
                {
                    _listeners.Add($"HttpAgent-{config.Name}", new HttpAgentModule(config));
                }
            }
            if (configuration.HttpBridgeService != null)
            {
                foreach (var config in configuration.HttpBridgeService)
                {
                    _listeners.Add($"HttpBridgeService-{config.Name}",
                        new HttpBridgeServiceModule(config));
                }
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
            _listeners.Clear();
        }

        public static IPAddress GetIP(string host)
        {
            return !String.IsNullOrEmpty(host) ? Dns.GetHostAddresses(host)[0] : IPAddress.Any;
        }
    }
}