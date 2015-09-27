using System.Web.Http;
using Ap.Express;
using Newtonsoft.Json.Serialization;
using Owin;

namespace Ap.Proxy.Api
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();

            config.UseStatic(new ContentOptions("..\\..\\wwwroot").UseWeb());

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            appBuilder.UseWebApi(config);
        } 

    }
}
