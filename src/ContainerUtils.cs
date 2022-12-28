using NP.DependencyInjection.Attributes;
using NP.IoC.CommonImplementations;
using System.Reflection;

namespace NP.IoC.CommonImplementations
{
    public static class ContainerUtils
    {
        public static void CheckTypeDerivation(this Type resolvingType, Type typeToResolve)
        {
            if (!resolvingType.IsAssignableFrom(typeToResolve))
            {
                throw new Exception($"Type to resolve '{typeToResolve.FullName}' does not derive from the resolving type: '{resolvingType.FullName}'");
            }
        }

        public static FullContainerItemResolvingKey<TKey> ToKey<TKey>(this Type typeToResolve, TKey resolutionKey)
        {
            FullContainerItemResolvingKey<TKey> typeToResolveKey =
                new FullContainerItemResolvingKey<TKey>(typeToResolve, resolutionKey);

            return typeToResolveKey;
        }

        public static Type GetMethodType(this MethodBase methodBase)
        {
            if (methodBase is MethodInfo methodInfo)
                return methodInfo.ReturnType;
            else if (methodBase is ConstructorInfo constrInfo)
            {
                return constrInfo.DeclaringType;
            }

            return null;
        }

        public static Type GetAndCheckResolvingType(this MethodBase factoryMethodBase, Type? resolvingType = null)
        {


            Type typeToResolve = factoryMethodBase.GetMethodType();

            if (resolvingType == null)
            {
                resolvingType = factoryMethodBase.GetMethodType();
            }
            else
            {
                resolvingType.CheckTypeDerivation(typeToResolve);
            }

            return resolvingType;
        }

        public static FullContainerItemResolvingKey<TKey>? GetTypeToResolveKey<TKey>
        (
            this ICustomAttributeProvider propOrParam,
            Type propOrParamType,
            bool returnNullIfNoPartAttr = true)
        {
            InjectAttribute injectAttr =
                propOrParam.GetAttr<InjectAttribute>();

            if (injectAttr == null)
            {
                if (returnNullIfNoPartAttr)
                {
                    return null;
                }
                else
                {
                    injectAttr = new InjectAttribute(propOrParamType);
                }
            }

            if (propOrParamType != null && injectAttr.ResolvingType != null)
            {
                if (!propOrParamType.IsAssignableFrom(injectAttr.ResolvingType))
                {
                    throw new ProgrammingError($"Actual type of a part should be a super-type of the type to resolve");
                }
            }

            Type? realPropOrParamType = injectAttr.ResolvingType ?? propOrParamType;

            return realPropOrParamType?.ToKey((TKey?) injectAttr.ResolutionKey);
        }

        public static FullContainerItemResolvingKey<TKey> GetTypeToResolveKey<TKey>(this PropertyInfo propInfo)
        {
            return GetTypeToResolveKey<TKey>(propInfo, propInfo.PropertyType);
        }

        public static FullContainerItemResolvingKey<TKey> GetTypeToResolveKey<TKey>(this ParameterInfo paramInfo)
        {
            return GetTypeToResolveKey<TKey>(paramInfo, paramInfo.ParameterType, false);
        }

        public static object CreateAndComposeObjFromMethod
        (
            this IObjComposer objectComposer, 
            MethodBase factoryInfo)
        {
            object[] args = objectComposer.GetMethodParamValues(factoryInfo).ToArray()!;

            object? obj = null;

            if (factoryInfo is MethodInfo factoryMethodInfo)
            {
                obj = factoryMethodInfo.Invoke(null, args);
            }
            else if (factoryInfo is ConstructorInfo constrInfo)
            {
                obj = constrInfo.Invoke(args);
            }

            objectComposer.ComposeObject(obj!);

            return obj!;
        }

        public static object CreateAndComposeObjFromType
        (
            this IObjComposer objectComposer, 
            Type resolvingType)
        {
            object? obj;
            ConstructorInfo constructorInfo =
                resolvingType.GetConstructors()
                              .FirstOrDefault(constr => constr.ContainsAttr<CompositeConstructorAttribute>())!;

            if (constructorInfo == null)
            {
                obj = Activator.CreateInstance(resolvingType)!;
            }
            else
            {
                obj =
                    Activator.CreateInstance
                    (
                        resolvingType,
                        objectComposer.GetMethodParamValues(constructorInfo).ToArray())!;
            }

            objectComposer.ComposeObject(obj);

            return obj;
        }
    }
}
