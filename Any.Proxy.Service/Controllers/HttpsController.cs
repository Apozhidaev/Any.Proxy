using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Any.Proxy.Service.Https;

namespace Any.Proxy.Service.Controllers
{
    [RoutePrefix("hs")]
    public class HttpsController : ApiController
    {
        [HttpPost]
        [Route("c")]
        public HttpResponseMessage Connect()
        {
            var httpRequest = Request.Content.ReadAsStringAsync().Result;
            var sp = httpRequest.Split(':');
            var connection = HttpsConnectionManager.Instance.New(sp[0], Int32.Parse(sp[1]));
            connection.HandshakeAsync().Wait();
            return Request.CreateResponse(HttpStatusCode.OK, connection.Id);
        }

        [HttpPost]
        [Route("r")]
        public HttpResponseMessage Receive()
        {
            var id = Request.Content.ReadAsStringAsync().Result;
            var connection = HttpsConnectionManager.Instance.Get(id);
            if (connection == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            var length = connection.RelayFromAsync().Result;
            if (length > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK, Convert.ToBase64String(connection.RemoteBuffer, 0, length));
            }
            connection.Dispose();
            return Request.CreateResponse(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        [Route("s")]
        public HttpResponseMessage Send()
        {
            var httpRequest = Request.Content.ReadAsStringAsync().Result;
            var sp = httpRequest.Split(':');
            var id = sp[0];
            byte[] httpResponse = Convert.FromBase64String(sp[1]);
            var connection = HttpsConnectionManager.Instance.Get(id);
            if (connection == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            var length = connection.RelayToAsync(httpResponse).Result;
            if (length > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            connection.Dispose();
            return Request.CreateResponse(HttpStatusCode.BadRequest);
        }
    }
}
