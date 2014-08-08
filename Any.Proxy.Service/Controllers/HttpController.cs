using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Web.Http;
using Any.Proxy.Com;

namespace Any.Proxy.Service.Controllers
{
    [RoutePrefix("h")]
    public class HttpController : ApiController
    {
        [HttpPost]
        [Route("r")]
        public HttpResponseMessage Receive()
        {
            var httpRequest = Request.Content.ReadAsByteArrayAsync().Result;
            // ищем хост и порт
            var myReg = new Regex(@"Host: (((?<host>.+?):(?<port>\d+?))|(?<host>.+?))\s+", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match m = myReg.Match(System.Text.Encoding.ASCII.GetString(httpRequest));
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
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }
                byte[] httpResponse = myRerouting.ReadToEnd(1000000);
                // передаем ответ обратно клиенту
                if (httpResponse != null && httpResponse.Length > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, httpResponse);
                }
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }
    }
}
