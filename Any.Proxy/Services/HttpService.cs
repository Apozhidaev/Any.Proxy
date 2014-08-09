﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Any.Proxy.Services.Https;

namespace Any.Proxy.Services
{
    public class HttpService
    {
        private readonly HttpListener _listener;

        public HttpService(string pref)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(pref);
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
                    case "hr":
                        HttpReceive(context);
                        break;
                    case "hsc":
                        HttpsConnect(context);
                        break;
                    case "hsr":
                        HttpsReceive(context);
                        break;
                    case "hss":
                        HttpsSend(context);
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

        public void HttpReceive(HttpListenerContext context)
        {
            var strRequest = ReadAsString(context.Request);
            byte[] httpRequest = Encoding.ASCII.GetBytes(strRequest);

            // ищем хост и порт
            var myReg = new Regex(@"Host: (((?<host>.+?):(?<port>\d+?))|(?<host>.+?))\s+", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match m = myReg.Match(strRequest);
            string host = m.Groups["host"].Value;
            int port = 0;
            // если порта нет, то используем 80 по умолчанию
            if (!int.TryParse(m.Groups["port"].Value, out port)) { port = 80; }

            // получаем апишник по хосту
            IPHostEntry myIPHostEntry = Dns.GetHostEntry(host);

            // создаем точку доступа
            var myIPEndPoint = new IPEndPoint(myIPHostEntry.AddressList[0], port);

            // создаем сокет и передаем ему запрос
            using (var myRerouting = new Socket(myIPHostEntry.AddressList[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                myRerouting.Connect(myIPEndPoint);
                if (myRerouting.Send(httpRequest, httpRequest.Length, SocketFlags.None) != httpRequest.Length)
                {
                    CreateResponse(context.Response, HttpStatusCode.BadRequest);
                    myRerouting.Shutdown(SocketShutdown.Both);
                    return;
                }
                byte[] httpResponse = myRerouting.ReadToEnd(1000000);
                // передаем ответ обратно клиенту
                if (httpResponse != null && httpResponse.Length > 0)
                {

                    CreateResponse(context.Response, HttpStatusCode.OK, httpResponse);
                    myRerouting.Shutdown(SocketShutdown.Both);
                    return;
                }
                CreateResponse(context.Response, HttpStatusCode.BadRequest);
                myRerouting.Shutdown(SocketShutdown.Both);
            }
        }

        public void HttpsConnect(HttpListenerContext context)
        {
            var httpRequest = ReadAsString(context.Request);
            var sp = httpRequest.Split(':');
            var connection = HttpsConnectionManager.Instance.New(sp[0], Int32.Parse(sp[1]));
            connection.HandshakeAsync().Wait();
            CreateResponse(context.Response, HttpStatusCode.OK, connection.Id);
        }

        public void HttpsReceive(HttpListenerContext context)
        {
            var id = ReadAsString(context.Request);
            var connection = HttpsConnectionManager.Instance.Get(id);
            if (connection == null)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest);
                return;
            }
            var length = connection.RelayFromAsync().Result;
            if (length > 0)
            {
                CreateResponse(context.Response, HttpStatusCode.OK, Convert.ToBase64String(connection.RemoteBuffer, 0, length));
                return;
            }
            connection.Dispose();
            CreateResponse(context.Response, HttpStatusCode.BadRequest);
        }

        public void HttpsSend(HttpListenerContext context)
        {
            var httpRequest = ReadAsString(context.Request);
            var sp = httpRequest.Split(':');
            var id = sp[0];
            byte[] httpResponse = Convert.FromBase64String(sp[1]);
            var connection = HttpsConnectionManager.Instance.Get(id);
            if (connection == null)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest);
                return;
            }
            var length = connection.RelayToAsync(httpResponse).Result;
            if (length > 0)
            {
                CreateResponse(context.Response, HttpStatusCode.OK);
                return;
            }
            connection.Dispose();
            CreateResponse(context.Response, HttpStatusCode.BadRequest);
        }

        private string ReadAsString(HttpListenerRequest context)
        {
            using (var stream = context.InputStream)
            {
                return new StreamReader(stream).ReadToEnd();
            }
        }

        private void CreateResponse(HttpListenerResponse context, HttpStatusCode status, string response)
        {
            try
            {
                var responseData = Encoding.ASCII.GetBytes(response);
                context.ContentLength64 = responseData.Length;
                context.StatusCode = (int)status;
                using (var stream = context.OutputStream)
                {
                    stream.Write(responseData, 0, responseData.Length);
                }
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

        private void CreateResponse(HttpListenerResponse context, HttpStatusCode status, byte[] responseData)
        {
            try
            {
                var responseData64 = Encoding.ASCII.GetBytes(Convert.ToBase64String(responseData));
                context.ContentLength64 = responseData64.Length;
                context.StatusCode = (int)status;
                using (var stream = context.OutputStream)
                {
                    stream.Write(responseData64, 0, responseData64.Length);
                }
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

        private void CreateResponse(HttpListenerResponse context, HttpStatusCode status)
        {
            try
            {
                context.ContentLength64 = 0;
                context.StatusCode = (int)status;
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