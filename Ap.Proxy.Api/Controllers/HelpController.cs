using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Description;
using Newtonsoft.Json.Linq;

namespace Ap.Proxy.Api.Controllers
{
    [RoutePrefix("api/help")]
    public class HelpController : ApiController
    {
        [HttpGet]
        [Route("")]
        public HttpResponseMessage Get()
        {
            var res = new JObject
            {
                {"Hello", "Welcome to the Ap.Proxy service"},
                {"Source", "https://github.com/Apozhidaev/Ap.Proxy"},
                {"Description", GetControllerInfo(typeof(ProxyController))}
            };
            return Request.CreateResponse(HttpStatusCode.OK, res);
        }

        public static JObject GetControllerInfo(params Type[] controllerTypes)
        {
            var res = new JObject();

            foreach (var type in controllerTypes)
            {
                var routePrefix = type.GetCustomAttributes<RoutePrefixAttribute>().First().Prefix;
                var controller = new JObject();
                foreach (var methodInfo in type.GetMethods())
                {
                    var verbs = GetMethods(methodInfo);
                    if (verbs.Length > 0)
                    {
                        var method = new JObject
                        {
                            {"Method",new JValue(verbs[0])},
                            {"Route", new JValue($"{routePrefix}/{methodInfo.GetCustomAttributes<RouteAttribute>().First().Template}")}
                        };
                        var body = new JObject();
                        var parameters = new JObject();
                        foreach (var parameterInfo in methodInfo.GetParameters())
                        {
                            if (parameterInfo.GetCustomAttributes<FromBodyAttribute>().FirstOrDefault() == null)
                            {
                                parameters.SetParams(parameterInfo);
                            }
                            else
                            {
                                body.SetParams(parameterInfo);
                            }
                        }
                        if (parameters.Count > 0)
                        {
                            method.Add("Params", parameters);
                        }
                        if (body.Count > 0)
                        {
                            method.Add("Body", body);
                        }
                        var resAttr = methodInfo.GetCustomAttributes<ResponseTypeAttribute>().FirstOrDefault();
                        if (resAttr != null)
                        {
                            var response = new JObject();
                            response.SetProps(resAttr.ResponseType);
                            if (response.Count > 0)
                            {
                                method.Add("Response", response);
                            }
                        }
                        
                        
                        controller.Add(methodInfo.Name, method);
                    }
                }
                res.Add(Regex.Replace(type.Name, "Controller$", ""), controller);
            }

            return res;
        }

        private static string[] GetMethods(MethodInfo methodInfo)
        {
            var list = new List<string>();
            list.AddRange(from customAttribute in methodInfo.GetCustomAttributes<HttpGetAttribute>() from httpMethod in customAttribute.HttpMethods select httpMethod.Method);
            list.AddRange(from customAttribute in methodInfo.GetCustomAttributes<HttpPostAttribute>() from httpMethod in customAttribute.HttpMethods select httpMethod.Method);
            list.AddRange(from customAttribute in methodInfo.GetCustomAttributes<HttpPutAttribute>() from httpMethod in customAttribute.HttpMethods select httpMethod.Method);
            list.AddRange(from customAttribute in methodInfo.GetCustomAttributes<HttpDeleteAttribute>() from httpMethod in customAttribute.HttpMethods select httpMethod.Method);
            return list.Distinct().ToArray();
        }

        
    }
}