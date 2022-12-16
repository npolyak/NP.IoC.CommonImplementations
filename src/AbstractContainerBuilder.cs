using NP.DependencyInjection.Attributes;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NP.IoC.CommonImplementations
{
    public abstract class AbstractContainerBuilder
    {
        public abstract void RegisterType
        (
            Type resolvingType, 
            Type typeToResolve, 
            object? resolutionKey = null);

        public abstract void RegisterSingletonType
        (
            Type resolvingType,
            Type typeToResolve,
            object? resolutionKey = null);

        public abstract void RegisterSingletonFactoryMethodInfo
        (
            MethodBase factoryMethodInfo,
            Type? resolvingType = null,
            object? resolutionKey = null);

        public abstract void RegisterFactoryMethodInfo
        (
            MethodBase factoryMethodInfo,
            Type? resolvingType = null,
            object? resolutionKey = null);

        public abstract void RegisterSingletonInstance(
            Type resolvingType,
            object instance,
            object? resolutionKey = null);


        public void RegisterType<TResolving, TToResolve>(object? resolutionKey = null)
            where TToResolve : TResolving
        {
            RegisterType(typeof(TResolving), typeof(TToResolve), resolutionKey);
        }

        public void RegisterSingletonInstance<TResolving>(object instance, object? resolutionKey = null)
        {
            RegisterSingletonInstance(typeof(TResolving), instance, resolutionKey);
        }

        public void RegisterSingletonType<TResolving, TToResolve>(object? resolutionKey = null)
            where TToResolve : TResolving
        {
            RegisterSingletonType(typeof(TResolving), typeof(TToResolve), resolutionKey);
        }

        public void RegisterFactoryMethodInfo<TResolving>
        (
            MethodBase factoryMethodInfo,
            object? resolutionKey = null)
        {
            RegisterFactoryMethodInfo(factoryMethodInfo, typeof(TResolving), resolutionKey);
        }

        public void RegisterSingletonFactoryMethodInfo<TResolving>
        (
            MethodBase factoryMethodInfo,
            object? resolutionKey = null)
        {
            RegisterSingletonFactoryMethodInfo(factoryMethodInfo, typeof(TResolving), resolutionKey);
        }

        protected abstract void RegisterAttributedType(Type resolvingType, Type typeToResolve, object? resolutionKey = null);

        protected abstract void RegisterAttributedSingletonType(Type resolvingType, Type typeToResolve, object? resolutionKey = null);


        public void RegisterAttributedClass(Type attributedType)
        {
            RegisterTypeAttribute registerTypeAttribute =
                   attributedType.GetCustomAttribute<RegisterTypeAttribute>()!;

            if (registerTypeAttribute == null)
            {
                "Cannot call RegisterAttributedClass method on a type without RegisterTypeAttribute".ThrowProgError();
            }

            if (registerTypeAttribute!.ResolvingType == null)
            {
                registerTypeAttribute.ResolvingType =
                    attributedType.GetBaseTypeOrFirstInterface() ?? throw new Exception($"IoCy Programming Error: Type {attributedType.FullName} has an 'Implements' attribute, but does not have any base type and does not implement any interfaces");
            }

            Type resolvingType = registerTypeAttribute.ResolvingType;
            object? resolutionKeyObj = registerTypeAttribute.ResolutionKey;
            bool isSingleton = registerTypeAttribute.IsSingleton;

            if (isSingleton)
            {
                this.RegisterAttributedSingletonType(resolvingType, attributedType, resolutionKeyObj);
            }
            else
            {
                this.RegisterAttributedType(resolvingType, attributedType, resolutionKeyObj);
            }

        }

        public void RegisterAttributedStaticFactoryMethodsFromClass(Type attributedType)
        {
            HasRegisterMethodsAttribute? hasRegisterMethodAttribute =
                attributedType.GetCustomAttribute<HasRegisterMethodsAttribute>();

            if (hasRegisterMethodAttribute == null)
            {
                "Cannot call RegisterStaticMethodsFromClass method on a type without HasRegisterMethodsAttribute".ThrowProgError();
            }

            foreach (var methodInfo in attributedType.GetMethods().Where(methodInfo => methodInfo.IsStatic))
            {
                RegisterMethodAttribute? factoryMethodAttribute = methodInfo.GetAttr<RegisterMethodAttribute>();

                if (factoryMethodAttribute != null)
                {
                    Type resolvingType = factoryMethodAttribute.ResolvingType ?? methodInfo.ReturnType;
                    object? partKeyObj = factoryMethodAttribute.ResolutionKey;
                    bool isSingleton = factoryMethodAttribute.IsSingleton;

                    if (isSingleton)
                    {
                        this.RegisterSingletonFactoryMethodInfo(methodInfo, resolvingType, partKeyObj);
                    }
                    else
                    {
                        this.RegisterFactoryMethodInfo(methodInfo, resolvingType, partKeyObj);
                    }
                }
            }
        }

        public void RegisterAssembly(Assembly assembly)
        {
            foreach (Type resolvingType in assembly.GetExportedTypes())
            {
                RegisterAttributedClass(resolvingType);
            }
        }

        public void RegisterDynamicAssemblyByFullPath(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
                throw new Exception($"There is no assembly at path '{assemblyPath}'");

            string absoluteAssemblyPath = Path.GetFullPath(assemblyPath);

            Assembly loadedAssembly = Assembly.LoadFile(absoluteAssemblyPath);

            RegisterAssembly(loadedAssembly);
        }


        public void RegisterPluginsFromFolder
        (
            string assemblyFolderPath,
            Regex? matchingFileName = null
        )
        {
            if (!Directory.Exists(assemblyFolderPath))
                throw new Exception($"There is no folder at path '{assemblyFolderPath}'");

            foreach (string filePath in Directory.GetFiles(assemblyFolderPath))
            {
                if (!filePath.ToLower().EndsWith(".dll"))
                    continue;

                if (matchingFileName?.IsMatch(filePath) != false)
                {
                    string absoluteAssemblyPath = Path.GetFullPath(filePath);

                    Assembly assembly = Assembly.LoadFile(absoluteAssemblyPath);

                    RegisterAssembly(assembly);
                }
            }
        }

        // loads and registers assemblies that match the rejex 
        // from all direct sub-folders of folder specified
        // by baseFolderPath argument.
        public void RegisterPluginsFromSubFolders
        (
            string baseFolderPath,
            Regex? matchingFileName = null)
        {
            foreach (string folderPath in Directory.GetDirectories(baseFolderPath))
            {
                if (folderPath == "." || folderPath.StartsWith(".."))
                {
                    continue;
                }
                RegisterPluginsFromFolder(folderPath, matchingFileName);
            }
        }

        private static Assembly ResolveReferencedAssembly(object sender, ResolveEventArgs args)
        {
            AssemblyName? name =
                args.RequestingAssembly
                    ?.GetReferencedAssemblies()
                    .FirstOrDefault(a => a.FullName == args.Name);

            return name?.FindOrLoadAssembly()!;
        }


        protected virtual void SetAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveReferencedAssembly!;
        }

        public AbstractContainerBuilder()
        {
            SetAssemblyResolver();
        }
    }
}
