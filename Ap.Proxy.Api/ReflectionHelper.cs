using System;

namespace Ap.Proxy.Api
{
    public static class ReflectionHelper
    {
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }
    }
}