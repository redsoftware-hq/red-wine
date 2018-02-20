using System;
using System.Reflection;

namespace Red.Wine
{
    public static class Extensions
    {
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static bool IsNormalProperty(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType.Equals(typeof(string)))
            {
                return true;
            }

            return false;
        }

        public static bool IsEnumProperty(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsEnum)
            {
                return true;
            }

            return false;
        }

        public static bool IsObjectProperty(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsClass && !propertyInfo.PropertyType.IsGenericType)
            {
                return true;
            }

            return false;
        }

        public static bool IsCollectionProperty(this PropertyInfo propertyInfo)
        {
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) && propertyInfo.PropertyType.IsGenericType)
            {
                return true;
            }

            return false;
        }
    }
}
