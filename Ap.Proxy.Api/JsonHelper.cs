using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Ap.Proxy.Api
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerSettings jsonSerializerSettings =
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        public static void Serialize<T>(string path, T obj)
        {
            string directory = Path.GetDirectoryName(path);
            if (!String.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(path, obj.ToJson());
        }

        public static T Deserialize<T>(string path)
        {
            var json = File.ReadAllText(path);
            return json.FromJson<T>();
        }

        public static string ToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
        }

        public static T FromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings);
        }

        public static string ToCamelCase(this string value)
        {
            if (String.IsNullOrEmpty(value)) return value;
            if (value.Length == 1) return value.ToLower();
            return String.Concat(value.Substring(0, 1).ToLower(), value.Substring(1));
        }

        public static JObject SetProps(this JObject properties, Type type)
        {
            foreach (var propertyInfo in type.GetProperties())
            {
                properties.Add(propertyInfo.Name.ToCamelCase(), new JValue(
                    propertyInfo.PropertyType.IsNullable()
                        ? propertyInfo.PropertyType.GenericTypeArguments[0].Name
                        : propertyInfo.PropertyType.Name));
            }
            return properties;
        }

        public static void SetParams(this JObject properties, ParameterInfo parameterInfo)
        {
            if (parameterInfo.ParameterType.IsPrimitive
                || parameterInfo.ParameterType == typeof(String)
                || parameterInfo.ParameterType == typeof(Guid))
            {
                properties.Add(parameterInfo.Name, parameterInfo.ParameterType.Name);
            }
            else if (parameterInfo.ParameterType.IsNullable())
            {
                properties.Add(parameterInfo.Name, parameterInfo.ParameterType.GenericTypeArguments[0].Name);
            }
            else
            {
                properties.SetProps(parameterInfo.ParameterType);
            }
        }
    }
}