using System;
using System.Linq;
using System.Reflection;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine.Load
{
    sealed class AssemblyDescriber : MarshalByRefObject
    {
        readonly Type _iextensionObjectType;

        public AssemblyDescriber()
        {
            _iextensionObjectType = Type.ReflectionOnlyGetType(typeof (IExtensionObject).AssemblyQualifiedName, true, false);
        }

        public AssemblyName LoadFrom(string assemblyPath)
        {
            try
            {
                return Assembly.ReflectionOnlyLoadFrom(assemblyPath).GetName();
            }
            catch (Exception)
            {
            }

            return null;
        }

        public AssemblyName[] GetReferencedAssemblies(AssemblyName assemblyName)
        {
            var assembly = FindAssembly(assemblyName);

            if (assembly == null)
            {
                return null;
            }

            return assembly.GetReferencedAssemblies();
        }

        public string GetAssemblyTitle(AssemblyName assemblyName)
        {
            var assembly = FindAssembly(assemblyName);

            if (assembly == null)
            {
                return null;
            }

            var title = assembly.GetReflectionOnlyCustomAttribute<AssemblyTitleAttribute>();

            return title == null ? null : title.Title;
        }

        public TypeDescriptor[] GetTypes(AssemblyName assemblyName)
        {
            var assembly = FindAssembly(assemblyName);

            if (assembly == null)
            {
                return null;
            }

            return assembly
                .GetTypes()
                .Where(type => _iextensionObjectType.IsAssignableFrom(type))
                .Select(TypeDescriptor.GetOrAdd)
                .ToArray();
        }

        static Assembly FindAssembly(AssemblyName assemblyName)
        {
            var str = assemblyName.FullName;

            return AppDomain.CurrentDomain
                .ReflectionOnlyGetAssemblies()
                .Where(x => x.GetName().FullName == str)
                .FirstOrDefault();
        }

        public MethodDescriptor[] GetPublicMethods(TypeDescriptor classType)
        {
            var type = Type.ReflectionOnlyGetType(classType.AssemblyQualifiedName, false, false);

            if (type == null)
            {
                return null;
            }

            var query = from method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        let parameters = method.GetParameters().Select(CreateParameterDescriptor)
                        select new MethodDescriptor(classType, method.Name, parameters, method.GetReflectionOnlyCustomAttributes<APIAttribute>());

            return query.ToArray();
        }

        public APIAttribute[] GetClassAttributes(TypeDescriptor classType)
        {
            var type = Type.ReflectionOnlyGetType(classType.AssemblyQualifiedName, false, false);

            if (type == null)
            {
                return null;
            }

            return type.GetReflectionOnlyCustomAttributes<APIAttribute>().ToArray();
        }

        static ParameterDescriptor CreateParameterDescriptor(ParameterInfo info)
        {
            var paramType = Type.ReflectionOnlyGetType(info.ParameterType.AssemblyQualifiedName, true, false);

            return new ParameterDescriptor(info.Name, TypeDescriptor.GetOrAdd(paramType), info.GetReflectionOnlyCustomAttributes<APIAttribute>());
        }
    }
}

