using System.Reflection;

namespace NP.IoC.CommonImplementations
{
    public abstract class AbstractContainer
    {
        protected abstract object? ResolveKey(FullContainerItemResolvingKey fullResolvingKey);

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

                FullContainerItemResolvingKey? propTypeToResolveKey = 
                    propInfo.GetTypeToResolveKey();

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

        protected internal IEnumerable<object?> GetMethodParamValues(MethodBase methodInfo)
        {
            foreach (var paramInfo in methodInfo.GetParameters())
            {
                FullContainerItemResolvingKey? propTypeToResolveKey = 
                    paramInfo.GetTypeToResolveKey();

                if (propTypeToResolveKey == null)
                {
                    yield return null;
                }

                yield return ResolveKey(propTypeToResolveKey!);
            }
        }


        public object Resolve(Type resolvingType, object? resolutionKey = null)
        {
            FullContainerItemResolvingKey resolvingTypeKey = resolvingType.ToKey(resolutionKey);

            return ResolveKey(resolvingTypeKey);
        }

        private object ResolveImpl<TResolving>(object? resolutionKey)
        {
            Type resolvingType = typeof(TResolving);

            return Resolve(resolvingType, resolutionKey);
        }

        public TToResolve Resolve<TToResolve>(object? resolutionKey = null)
        {
            return (TToResolve)ResolveImpl<TToResolve>(resolutionKey);
        }
    }
}