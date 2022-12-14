using System.Collections;
using System.Reflection;

namespace NP.IoC.CommonImplementations
{
    public static class Utils
    {
        public static Type? GetBaseTypeOrFirstInterface(this Type type)
        {
            Type? result = type.BaseType;

            if (result == typeof(object))
            {
                result = type.GetInterfaces().FirstOrDefault();
            }

            return result;
        }


        public static TAttr? GetAttr<TAttr>(this ICustomAttributeProvider memberInfo)
            where TAttr : Attribute
        {
            return memberInfo.GetAttrs<TAttr>().FirstOrDefault();
        }

        public static IEnumerable<T> GetAttrs<T>(this ICustomAttributeProvider memberInfo)
            where T : Attribute
        {
            return (memberInfo.GetCustomAttributes(typeof(T), false) as IEnumerable<T>)!;
        }

        public static bool ContainsAttr<TAttr>(this ICustomAttributeProvider memberInfo)
            where TAttr : Attribute
        {
            return memberInfo.GetAttr<TAttr>() != null;
        }

        public static Assembly FindOrLoadAssembly(this AssemblyName assemblyName)
        {
            Assembly result =
                AppDomain.CurrentDomain
                         .GetAssemblies()
                         .LastOrDefault(assembly => assembly.FullName == assemblyName.FullName);

            if (result == null)
                result = Assembly.Load(assemblyName);

            return result;
        }


        public static bool ObjEquals(this object obj1, object obj2)
        {
            if (obj1 == obj2)
                return true;

            if ((obj1 != null) && (obj1.Equals(obj2)))
                return true;

            return false;
        }

        public static int GetHashCodeExtension(this object obj)
        {
            if (obj == null)
                return 0;

            return obj.GetHashCode();
        }

        public static string ToStr(this object obj)
        {
            if (obj == null)
                return string.Empty;

            return obj.ToString();
        }


        public static void AddAll
        (
            this IList collection,
            IEnumerable collectionToAdd,
            bool noDuplicates = false
        )
        {
            if ((collection == null) || (collectionToAdd == null))
                return;

            foreach (object el in collectionToAdd)
            {
                if (noDuplicates)
                {
                    if (collection.Contains(el))
                        continue;
                }

                collection.Add(el);
            }
        }


        public static void AddAll<T>
        (
            this ICollection<T> collection,
            IEnumerable<T> collectionToAdd,
            bool noDuplicates = false
        )
        {
            if ((collection == null) || (collectionToAdd == null))
                return;

            foreach (T el in collectionToAdd)
            {
                if (noDuplicates)
                {
                    if (collection.Contains(el))
                        continue;
                }

                collection.Add(el);
            }
        }


        /// <summary>
        /// single quotes
        /// </summary>
        public static string Sq(this object obj)
        {
            return $"'{obj.ToStr()}'";
        }

        public static void Throw(this string str) => throw new Exception(str);

        public static void ThrowProgError(this string str) => throw new ProgrammingError(str);

    }
}
