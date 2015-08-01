using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Ap.Proxy.Api.Controllers
{
    [RoutePrefix("proxy")]
    public class ProxyController : ApiController
    {
        [HttpGet]
        [Route("reboot")]
        public HttpResponseMessage Reboot(string p)
        {
            if (RemoteControl.Password == p)
            {
                RemoteControl.Proxy.Stop();
                RemoteControl.Proxy.Start();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            return Request.CreateResponse(HttpStatusCode.Unauthorized);
        }

        [HttpGet]
        [Route("stop")]
        public HttpResponseMessage Stop(string p)
        {
            if (RemoteControl.Password == p)
            {
                RemoteControl.Proxy.Stop();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            return Request.CreateResponse(HttpStatusCode.Unauthorized);
        }
    }
}