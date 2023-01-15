using NP.DependencyInjection.Attributes;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NP.IoC.CommonImplementations
{
    public abstract class AbstractContainerBuilder<TKey>
    {
        public abstract void RegisterType
        (
            Type resolvingType,
            Type typeToResolve,
            TKey resolutionKey = default);

        public abstract void RegisterSingletonType
        (
            Type resolvingType,
            Type typeToResolve,
            TKey resolutionKey = default);

        public abstract void RegisterFactoryMethodInfo
        (
            MethodBase factoryMethodInfo,
            Type? resolvingType = null,
            TKey resolutionKey = default);

        public abstract void RegisterSingletonInstance(
            Type resolvingType,
            object instance,
            TKey resolutionKey = default);

        public abstract void RegisterSingletonFactoryMethodInfo
        (
            MethodBase factoryMethodInfo,
            Type? resolvingType = null,
            TKey resolutionKey = default);

        public virtual void RegisterMultiCellFactoryMethodInfo
        (
            MethodBase factoryMethodInfo,
            Type cellType,
            TKey resolutionKey = default)
        {

        }

        public void RegisterType<TResolving, TToResolve>(TKey resolutionKey = default)
            where TToResolve : TResolving
        {
            RegisterType(typeof(TResolving), typeof(TToResolve), resolutionKey);
        }

        public void RegisterSingletonInstance<TResolving>(object instance, TKey resolutionKey = default)
        {
            RegisterSingletonInstance(typeof(TResolving), instance, resolutionKey);
        }

        public void RegisterSingletonType<TResolving, TToResolve>(TKey resolutionKey = default)
            where TToResolve : TResolving
        {
            RegisterSingletonType(typeof(TResolving), typeof(TToResolve), resolutionKey);
        }

        public void RegisterFactoryMethodInfo<TResolving>
        (
            MethodBase factoryMethodInfo,
            TKey resolutionKey = default)
        {
            RegisterFactoryMethodInfo(factoryMethodInfo, typeof(TResolving), resolutionKey);
        }

        public void RegisterSingletonFactoryMethodInfo<TResolving>
        (
            MethodBase factoryMethodInfo,
            TKey resolutionKey = default)
        {
            RegisterSingletonFactoryMethodInfo
            (
                factoryMethodInfo, 
                typeof(TResolving), 
                resolutionKey);
        }

        protected abstract void RegisterAttributedType
        (
            Type resolvingType, 
            Type typeToResolve, 
            TKey resolutionKey = default);

        protected abstract void RegisterAttributedSingletonType
        (
            Type resolvingType, 
            Type typeToResolve, 
            TKey resolutionKey = default);

        protected virtual void RegisterAttributedMultiCellType
        (
            Type cellType,
            Type typeToResolve,
            TKey resolutionKey = default)
        {

        }


        private void RegisterAttributedClassImpl(Type attributedClass, bool exceptionIfNull)
        {
            RegisterTypeAttribute registerTypeAttribute =
                   attributedClass.GetCustomAttribute<RegisterTypeAttribute>()!;

            if (registerTypeAttribute == null)
            {
                if (exceptionIfNull)
                {
                    "Cannot call RegisterAttributedClass method on a type without RegisterTypeAttribute".ThrowProgError();
                }

                return;
            }

            Type? resolvingType = registerTypeAttribute.ResolvingType;
            if (resolvingType == null)
            {
                resolvingType =
                    attributedClass.GetBaseTypeOrFirstInterface() ?? throw new Exception($"IoCy Programming Error: Type {attributedClass.FullName} has an 'Implements' attribute, but does not have any base type and does not implement any interfaces");
            }

            TKey? resolutionKeyObj = (TKey?) registerTypeAttribute.ResolutionKey;
            bool isMultiCell = registerTypeAttribute.IsMultiCell;
            bool isSingleton = registerTypeAttribute.IsSingleton;
            
            if (isMultiCell)
            {
                this.RegisterAttributedMultiCellType
                (
                    registerTypeAttribute.CellType,
                    attributedClass,
                    resolutionKeyObj
                );
            }
            else if (isSingleton)
            {
                this.RegisterAttributedSingletonType
                (
                    resolvingType, 
                    attributedClass, 
                    resolutionKeyObj);
            }
            else
            {
                this.RegisterAttributedType(resolvingType, attributedClass, resolutionKeyObj);
            }
        }

        public void RegisterAttributedClassNoException(Type attributedClass)
        {
            RegisterAttributedClassImpl(attributedClass, false);
        }

        public void RegisterAttributedClass(Type attributedClass)
        {
            RegisterAttributedClassImpl(attributedClass, true);
        }


        private void RegisterAttributedStaticFactoryMethodsFromClassImpl(Type classWithStaticFactoryMethods, bool exceptionIfNull)
        {
            HasRegisterMethodsAttribute? hasRegisterMethodAttribute =
                classWithStaticFactoryMethods.GetCustomAttribute<HasRegisterMethodsAttribute>();

            if (hasRegisterMethodAttribute == null)
            {
                if (exceptionIfNull)
                {
                    "Cannot call RegisterStaticMethodsFromClass method on a type without HasRegisterMethodsAttribute".ThrowProgError();
                }

                return;
            }

            foreach (var methodInfo in classWithStaticFactoryMethods.GetMethods().Where(methodInfo => methodInfo.IsStatic))
            {
                RegisterMethodAttribute? factoryMethodAttribute = methodInfo.GetAttr<RegisterMethodAttribute>();

                if (factoryMethodAttribute != null)
                {
                    Type resolvingType = factoryMethodAttribute.ResolvingType ?? methodInfo.ReturnType;
                    TKey? partKeyObj = (TKey?) factoryMethodAttribute.ResolutionKey;
                    bool isSingleton = factoryMethodAttribute.IsSingleton;
                    bool isMultiCell = factoryMethodAttribute.IsMultiCell;
                    Type cellType = factoryMethodAttribute.CellType!;

                    if (isMultiCell)
                    {
                        this.RegisterMultiCellFactoryMethodInfo
                        (
                            methodInfo,
                            cellType,
                            partKeyObj);
                    }
                    else if (isSingleton)
                    {
                        this.RegisterSingletonFactoryMethodInfo
                        (
                            methodInfo, 
                            resolvingType, 
                            partKeyObj);
                    }
                    else
                    {
                        this.RegisterFactoryMethodInfo
                        (
                            methodInfo, 
                            resolvingType, 
                            partKeyObj);
                    }
                }
            }
        }

        protected void RegisterAttributedStaticFactoryMethodsFromClassNoException(Type classWithStaticFactoryMethods)
        {

            RegisterAttributedStaticFactoryMethodsFromClassImpl
            (
                classWithStaticFactoryMethods, 
                false);
        }

        public void RegisterAttributedStaticFactoryMethodsFromClass
        (
            Type classWithStaticFactoryMethods)
        {
            RegisterAttributedStaticFactoryMethodsFromClassImpl
            (
                classWithStaticFactoryMethods, 
                true);
        }

        public void RegisterAssembly(Assembly assembly)
        {
            foreach (Type resolvingType in assembly.GetExportedTypes())
            {
                RegisterAttributedClassNoException(resolvingType);
                RegisterAttributedStaticFactoryMethodsFromClassNoException(resolvingType);
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
