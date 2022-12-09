using NP.DependencyInjection.Attributes;
using NP.IoC.CommonImplementations;
using NP.Utilities;
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

        public static FullContainerItemResolvingKey ToKey(this Type typeToResolve, object? resolutionKey)
        {
            FullContainerItemResolvingKey typeToResolveKey =
                new FullContainerItemResolvingKey(typeToResolve, resolutionKey);

            return typeToResolveKey;
        }

        public static Type GetAndCheckResolvingType(this MethodInfo factoryMethodInfo, Type? resolvingType = null)
        {
            Type typeToResolve = factoryMethodInfo.ReturnType;

            if (resolvingType == null)
            {
                resolvingType = factoryMethodInfo.ReturnType;
            }
            else
            {
                resolvingType.CheckTypeDerivation(typeToResolve);
            }

            return resolvingType;
        }

        public static FullContainerItemResolvingKey? GetTypeToResolveKey
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

            return realPropOrParamType?.ToKey(injectAttr.ResolutionKey);
        }

        public static FullContainerItemResolvingKey? GetTypeToResolveKey(this PropertyInfo propInfo)
        {
            return GetTypeToResolveKey(propInfo, propInfo.PropertyType);
        }

        public static FullContainerItemResolvingKey? GetTypeToResolveKey(this ParameterInfo paramInfo)
        {
            return GetTypeToResolveKey(paramInfo, paramInfo.ParameterType, false);
        }

        public static object CreateAndComposeObjFromMethod(this AbstractContainer objectComposer, MethodInfo factoryMethodInfo)
        {
            object[] args = objectComposer.GetMethodParamValues(factoryMethodInfo).ToArray()!;

            object obj = factoryMethodInfo.Invoke(null, args)!;

            objectComposer.ComposeObject(obj);

            return obj;
        }

        public static object CreateAndComposeObjFromType(this AbstractContainer objectComposer, Type resolvingType)
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
