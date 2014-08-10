using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Any.Proxy.Remote
{
    public class RemoteControl
    {
        private readonly RemoteSection _config;
        private readonly HttpListener _listener;

        public RemoteControl(RemoteSection config)
        {
            _config = config;
            _listener = new HttpListener();
            foreach (var prefix in config.Prefixes.Split(','))
            {
                _listener.Prefixes.Add(prefix);
            }    
        }

        public async void Start()
        {
            _listener.Start();
            while (true)
            {
                try
                {
                    ProcessRequestAsync(await _listener.GetContextAsync());
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            _listener.Stop();
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private async void ProcessRequestAsync(HttpListenerContext context)
        {
            await Task.Yield();
            try
            {
                if (context.Request.QueryString["p"] != _config.Password)
                {
                    CreateResponse(context.Response, HttpStatusCode.Unauthorized);
                    return;
                }
                switch (context.Request.QueryString["a"])
                {
                    case "reboot":
                        KillProcess();
                        StartProcess();
                        CreateResponse(context.Response, HttpStatusCode.OK);
                        break;
                    case "stop":
                        KillProcess();
                        CreateResponse(context.Response, HttpStatusCode.OK);
                        break;
                    default:
                        CreateResponse(context.Response, HttpStatusCode.NotFound);
                        break;
                }
            }
            catch (Exception)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest);
            }
        }

        private void StartProcess()
        {
            Process.Start(_config.ProcessPath);
        }

        private void KillProcess()
        {
            var processes = Process.GetProcessesByName("Any.Proxy");
            foreach (var process in processes)
            {
                process.Kill();
            }
        }

        private void CreateResponse(HttpListenerResponse context, HttpStatusCode status)
        {
            try
            {
                context.ContentLength64 = 0;
                context.StatusCode = (int) status;
                context.Close();
            }
            catch (Exception)
            {
                try
                {
                    context.Abort();
                }
                catch (Exception)
                {
                }
            }
        } 
    }
}