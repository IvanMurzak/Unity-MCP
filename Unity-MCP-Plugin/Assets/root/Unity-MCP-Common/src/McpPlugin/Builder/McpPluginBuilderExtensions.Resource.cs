/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class McpPluginBuilderExtensions
    {
        public static IMcpPluginBuilder WithResources(this IMcpPluginBuilder builder, params Type[] targetTypes)
            => WithResources(builder, targetTypes.ToArray());

        public static IMcpPluginBuilder WithResources(this IMcpPluginBuilder builder, IEnumerable<Type> targetTypes)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (targetTypes == null)
                throw new ArgumentNullException(nameof(targetTypes));

            foreach (var targetType in targetTypes)
                WithResources(builder, targetType);

            return builder;
        }

        public static IMcpPluginBuilder WithResources<T>(this IMcpPluginBuilder builder)
            => WithResources(builder, typeof(T));

        public static IMcpPluginBuilder WithResources(this IMcpPluginBuilder builder, Type classType)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (classType == null)
                throw new ArgumentNullException(nameof(classType));

            foreach (var method in classType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                var attribute = method.GetCustomAttribute<McpPluginResourceAttribute>();
                if (attribute == null)
                    continue;

                builder.WithResource(classType: classType, getContentMethod: method);
            }
            return builder;
        }

        public static IMcpPluginBuilder WithResourcesFromAssembly(this IMcpPluginBuilder builder, IEnumerable<Assembly> assemblies)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            foreach (var assembly in assemblies)
                WithResourcesFromAssembly(builder, assembly);

            return builder;
        }
        public static IMcpPluginBuilder WithResourcesFromAssembly(this IMcpPluginBuilder builder, Assembly? assembly = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            assembly ??= Assembly.GetCallingAssembly();

            return builder.WithResources(
                from t in assembly.GetTypes()
                where t.GetCustomAttribute<McpPluginResourceTypeAttribute>() is not null
                select t);
        }
    }
}
