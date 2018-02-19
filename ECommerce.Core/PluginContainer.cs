﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ECommerce.Core
{
    public static class PluginContainer
    {
        public static T GetInstance<T>(string assemblyName, params object[] parameters)
        {
            var path = "";
            var entryAssembly = Assembly.GetEntryAssembly();

            path = AppDomain.CurrentDomain.BaseDirectory ?? Path.GetDirectoryName(entryAssembly.Location);

            var files = new DirectoryInfo(path ?? throw new InvalidOperationException("path is null")).GetFiles($"{assemblyName}.dll", SearchOption.AllDirectories);
            var typeAssignable = typeof(T);

            foreach (var file in files)
            {
                try
                {
                    var assembly = AppDomain.CurrentDomain.GetAssemblies()
                                       .FirstOrDefault(x => !x.IsDynamic && x.Location == file.FullName) ??
                                   Assembly.LoadFile(file.FullName);

                    var type = assembly.ExportedTypes.FirstOrDefault(t => IsAsignable(t, typeAssignable));

                    if (type == null) continue;

                    if (type.IsGenericType == false) return parameters.Any() ? (T)Activator.CreateInstance(type, parameters) : (T)Activator.CreateInstance(type);

                    var constructedType = type.MakeGenericType(typeof(T).GenericTypeArguments);

                    return parameters.Any() ? (T)Activator.CreateInstance(constructedType, parameters) : (T)Activator.CreateInstance(constructedType);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.Write(ex.ToString());
                }
            }

            return default(T);
        }

        private static bool IsAsignable(Type givenType, Type typeAssignable)
        {
            if (givenType.IsGenericType == false && typeAssignable.IsAssignableFrom(givenType))
                return true;

            if (givenType.IsGenericType && (typeAssignable.IsGenericType
                    ? givenType.GetGenericTypeDefinition() ==
                      typeAssignable.GetGenericTypeDefinition()
                    : typeAssignable.IsAssignableFrom(givenType)))
            {
                return true;

            }

            var interfaceTypes = givenType.GetInterfaces();

            if (interfaceTypes.Any(it => it.IsGenericType && (typeAssignable.IsGenericType ?
                it.GetGenericTypeDefinition() == typeAssignable.GetGenericTypeDefinition() : typeAssignable.IsAssignableFrom(givenType))))
            {
                return true;
            }

            var baseType = givenType.BaseType;

            return baseType != null && IsAsignable(baseType, typeAssignable);
        }
    }
}
