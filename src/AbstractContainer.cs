using System.Reflection;

namespace NP.IoC.CommonImplementations
{
    public abstract class AbstractContainer<TKey> : IObjComposer
    {
        protected abstract object? ResolveKey(FullContainerItemResolvingKey<TKey> fullResolvingKey);

        // compose an object based in its properties' attributes
        public void ComposeObject(object obj)
        {
            Type objType = obj.GetType();

            foreach (PropertyInfo propInfo in
                        objType.GetProperties
                        (
                            BindingFlags.Instance |
                            BindingFlags.NonPublic |
                            BindingFlags.Public))
            {
                if (propInfo.SetMethod == null)
                    continue;

                FullContainerItemResolvingKey<TKey>? propTypeToResolveKey = 
                    propInfo.GetTypeToResolveKey<TKey>();

                if (propTypeToResolveKey == null)
                {
                    continue;
                }

                object? subObj = ResolveKey(propTypeToResolveKey);

                if (subObj != null)
                {
                    propInfo.SetMethod.Invoke(obj, new[] { subObj });
                }
            }
        }

        public IEnumerable<object?> GetMethodParamValues(MethodBase methodInfo)
        {
            foreach (var paramInfo in methodInfo.GetParameters())
            {
                FullContainerItemResolvingKey<TKey>? propTypeToResolveKey = 
                    paramInfo.GetTypeToResolveKey<TKey>();

                if (propTypeToResolveKey == null)
                {
                    yield return null;
                }

                yield return ResolveKey(propTypeToResolveKey!);
            }
        }


        public object Resolve(Type resolvingType, TKey resolutionKey = default)
        {
            FullContainerItemResolvingKey<TKey> resolvingTypeKey = resolvingType.ToKey(resolutionKey);

            return ResolveKey(resolvingTypeKey);
        }

        private object ResolveImpl<TResolving>(TKey resolutionKey)
        {
            Type resolvingType = typeof(TResolving);

            return Resolve(resolvingType, resolutionKey);
        }

        public TToResolve Resolve<TToResolve>(TKey resolutionKey = default)
        {
            return (TToResolve)ResolveImpl<TToResolve>(resolutionKey);
        }
    }
}