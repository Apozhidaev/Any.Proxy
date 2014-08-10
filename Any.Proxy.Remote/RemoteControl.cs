using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Any.Proxy.Remote
{
    public class RemoteControl
    {
        private readonly HttpListener _listener;

        public RemoteControl(string prefixes)
        {
            _listener = new HttpListener();
            foreach (var prefix in prefixes.Split(','))
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
                switch (context.Request.QueryString["a"])
                {
                    case "reboot":
                        Reboot(context);
                        break;
                    default:
                        CreateResponse(context.Response, HttpStatusCode.BadRequest);
                        break;
                }
            }
            catch (Exception)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest);
            }
        }

        private void Reboot(HttpListenerContext context)
        {
            var processes = Process.GetProcessesByName("Any.Proxy");
            foreach (var process in processes)
            {
                process.Kill();
            }
            Process.Start(@"C:\Proxy\Any.Proxy.exe");
            CreateResponse(context.Response, HttpStatusCode.OK);
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