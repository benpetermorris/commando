using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Threading;

namespace twomindseye.Commando.Util
{
    public static class UtilExtensions
    {
        public static void BeginInvoke(this Dispatcher dispatcher, Action action)
        {
            dispatcher.BeginInvoke((Delegate) action);
        }

        public static int RemoveWhere<T>(this IList<T> list, Func<T, bool> predicate, Action<T> onRemoved = null)
        {
            var count = list.Count;
            var removed = 0;

            for (var i = 0; i < count; i++)
            {
                if (!predicate(list[i]))
                {
                    continue;
                }

                if (onRemoved != null)
                {
                    onRemoved(list[i]);
                }

                list.RemoveAt(i);
                --i;
                --count;
                ++removed;
            }

            return removed;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue @default = default(TValue))
        {
            TValue rvl;
            return !dictionary.TryGetValue(key, out rvl) ? @default : rvl;
        }

        public static T ValOrDefault<T>(this IDataRecord record, int ordinal, T @default = default(T))
        {
            return record.IsDBNull(ordinal) ? @default : (T)record[ordinal];
        }

        public static T ValOrDefault<T>(this IDataRecord record, string columnName, T @default = default(T))
        {
            return ValOrDefault(record, record.GetOrdinal(columnName), @default);
        }

        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader, bool dispose = true)
        {
            try
            {
                while (reader.Read())
                {
                    yield return reader;
                }
            }
            finally
            {
                if (dispose)
                {
                    reader.Dispose();
                }
            }
        }

        public static bool IsDBNull(this IDataRecord record, string columnName)
        {
            return record.IsDBNull(record.GetOrdinal(columnName));
        }

        public static string ToHexString(this IList<byte> sequence)
        {
            return sequence.Aggregate(new StringBuilder(sequence.Count * 2), (sb, ch) => sb.AppendFormat("{0:x2}", ch)).ToString();
        }

        static object ConvertAttributeArgument(object argument, Type argumentType)
        {
            if (argumentType.IsArray)
            {
                var src = (IEnumerable) argument;
                var dest = Array.CreateInstance(argumentType.GetElementType(), src.Cast<object>().Count());
                var index = 0;

                foreach (CustomAttributeTypedArgument arg in src)
                {
                    dest.SetValue(ConvertAttributeArgument(arg.Value, arg.ArgumentType), index++);
                }

                return dest;
            }

            return argument;
        }

        public static T CreateAttributeInstance<T>(this CustomAttributeData data) where T : Attribute
        {
            var realType = Type.GetType(data.Constructor.DeclaringType.AssemblyQualifiedName);

            if (realType == null)
            {
                return null;
            }

            var constructor = realType.GetConstructor(data.ConstructorArguments.Select(x => x.ArgumentType).ToArray());

            if (constructor == null)
            {
                return null;
            }

            var t = (T) constructor.Invoke(data.ConstructorArguments.Select(x => ConvertAttributeArgument(x.Value, x.ArgumentType)).ToArray());

            foreach (var pos in data.NamedArguments)
            {
                var prop = realType.GetProperty(pos.MemberInfo.Name);

                if (prop == null)
                {
                    return null;
                }

                prop.SetValue(t, ConvertAttributeArgument(pos.TypedValue.Value, pos.TypedValue.ArgumentType), null);
            }

            return t;
        }

        static public IEnumerable<Type> EnumerateBaseTypes(this Type type)
        {
            var baseType = type.BaseType;

            while (baseType != null)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        static IEnumerable<CustomAttributeData> FindAttributeData(IEnumerable<CustomAttributeData> dataSet, Type attributeType)
        {
            // TODO: wow, this is ugly!

            return dataSet
                .Where(x => x.Constructor.DeclaringType.AssemblyQualifiedName == attributeType.AssemblyQualifiedName || 
                    x.Constructor.DeclaringType.EnumerateBaseTypes().Any(y => y.AssemblyQualifiedName == attributeType.AssemblyQualifiedName));
        }

        public static T GetReflectionOnlyCustomAttribute<T>(this MemberInfo info) where T : Attribute
        {
            return FindAttributeData(CustomAttributeData.GetCustomAttributes(info), typeof(T))
                .Select(x => x.CreateAttributeInstance<T>())
                .FirstOrDefault();
        }

        public static T GetReflectionOnlyCustomAttribute<T>(this ParameterInfo info) where T : Attribute
        {
            return FindAttributeData(CustomAttributeData.GetCustomAttributes(info), typeof(T))
                .Select(x => x.CreateAttributeInstance<T>())
                .FirstOrDefault();
        }

        public static T GetReflectionOnlyCustomAttribute<T>(this Assembly assembly) where T : Attribute
        {
            return FindAttributeData(CustomAttributeData.GetCustomAttributes(assembly), typeof(T))
                .Select(x => x.CreateAttributeInstance<T>())
                .FirstOrDefault();
        }

        public static IEnumerable<T> GetReflectionOnlyCustomAttributes<T>(this MemberInfo info) where T : Attribute
        {
            return FindAttributeData(CustomAttributeData.GetCustomAttributes(info), typeof (T))
                .Select(x => x.CreateAttributeInstance<T>());
        }

        public static IEnumerable<T> GetReflectionOnlyCustomAttributes<T>(this ParameterInfo info) where T : Attribute
        {
            return FindAttributeData(CustomAttributeData.GetCustomAttributes(info), typeof(T))
                .Select(x => x.CreateAttributeInstance<T>());
        }

        public static IEnumerable<T> GetReflectionOnlyCustomAttributes<T>(this Assembly assembly) where T : Attribute
        {
            return FindAttributeData(CustomAttributeData.GetCustomAttributes(assembly), typeof(T))
                .Select(x => x.CreateAttributeInstance<T>());
        }

        public static T GetCustomAttribute<T>(this ICustomAttributeProvider r) where T : Attribute
        {
            return r.GetCustomAttributes(typeof(T), false).Cast<T>().FirstOrDefault();
        }
    }
}
