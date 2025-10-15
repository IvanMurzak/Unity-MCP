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
        public static IMcpPluginBuilder WithTools(this IMcpPluginBuilder builder, params Type[] targetTypes)
            => WithTools(builder, targetTypes.ToArray());

        public static IMcpPluginBuilder WithTools(this IMcpPluginBuilder builder, IEnumerable<Type> targetTypes)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (targetTypes == null)
                throw new ArgumentNullException(nameof(targetTypes));

            foreach (var targetType in targetTypes)
                WithTools(builder, targetType);

            return builder;
        }

        public static IMcpPluginBuilder WithTools<T>(this IMcpPluginBuilder builder)
            => WithTools(builder, typeof(T));

        public static IMcpPluginBuilder WithTools(this IMcpPluginBuilder builder, Type classType)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (classType == null)
                throw new ArgumentNullException(nameof(classType));

            foreach (var method in classType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                var attribute = method.GetCustomAttribute<McpPluginToolAttribute>();
                if (attribute == null)
                    continue;

                if (string.IsNullOrEmpty(attribute.Name))
                    throw new ArgumentException($"Tool name cannot be null or empty. Type: {classType.Name}, Method: {method.Name}");

                builder.WithTool(attribute, classType: classType, method: method);
            }
            return builder;
        }

        public static IMcpPluginBuilder WithToolsFromAssembly(this IMcpPluginBuilder builder, IEnumerable<Assembly> assemblies)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            foreach (var assembly in assemblies)
                WithToolsFromAssembly(builder, assembly);

            return builder;
        }
        public static IMcpPluginBuilder WithToolsFromAssembly(this IMcpPluginBuilder builder, Assembly? assembly = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            assembly ??= Assembly.GetCallingAssembly();

            return builder.WithTools(
                from t in assembly.GetTypes()
                where t.GetCustomAttribute<McpPluginToolTypeAttribute>() is not null
                select t);
        }
    }
}
