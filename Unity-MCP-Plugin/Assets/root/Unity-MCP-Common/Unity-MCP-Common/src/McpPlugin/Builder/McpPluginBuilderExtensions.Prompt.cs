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
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class McpPluginBuilderExtensions
    {
        public static IMcpPluginBuilder WithPrompts(this IMcpPluginBuilder builder, params Type[] targetTypes)
            => WithPrompts(builder, targetTypes.ToArray());

        public static IMcpPluginBuilder WithPrompts(this IMcpPluginBuilder builder, IEnumerable<Type> targetTypes)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (targetTypes == null)
                throw new ArgumentNullException(nameof(targetTypes));

            foreach (var targetType in targetTypes)
                WithPrompts(builder, targetType);

            return builder;
        }

        public static IMcpPluginBuilder WithPrompts<T>(this IMcpPluginBuilder builder)
            => WithPrompts(builder, typeof(T));

        public static IMcpPluginBuilder WithPrompts(this IMcpPluginBuilder builder, Type classType)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (classType == null)
                throw new ArgumentNullException(nameof(classType));

            foreach (var method in classType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                var attribute = method.GetCustomAttribute<McpPluginPromptAttribute>();
                if (attribute == null)
                    continue;

                if (string.IsNullOrEmpty(attribute.Name))
                    throw new ArgumentException($"Prompt name cannot be null or empty. Type: {classType.Name}, Method: {method.Name}");

                builder.WithPrompt(name: attribute.Name, classType: classType, method: method);
            }
            return builder;
        }

        public static IMcpPluginBuilder WithPromptsFromAssembly(this IMcpPluginBuilder builder, IEnumerable<Assembly> assemblies)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            foreach (var assembly in assemblies)
                WithPromptsFromAssembly(builder, assembly);

            return builder;
        }
        public static IMcpPluginBuilder WithPromptsFromAssembly(this IMcpPluginBuilder builder, Assembly? assembly = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            assembly ??= Assembly.GetCallingAssembly();

            return builder.WithPrompts(
                from t in assembly.GetTypes()
                where t.GetCustomAttribute<McpPluginPromptTypeAttribute>() is not null
                select t);
        }

        // ---------------------------

        public static List<ResponsePromptArgument>? ToResponsePromptArguments(this JsonNode? node)
        {
            if (node == null)
                return null;

            if (node is not JsonObject obj)
                return null;

            if (!obj.TryGetPropertyValue(JsonSchema.Properties, out var propertiesNode))
                return null;

            if (propertiesNode is not JsonObject propertiesObj)
                return null;

            return propertiesObj
                .Select(input =>
                {
                    if (input.Value is not JsonObject inputObj)
                        return null;

                    inputObj.TryGetPropertyValue(JsonSchema.Description, out var descriptionNode);
                    inputObj.TryGetPropertyValue(JsonSchema.Required, out var requiredNode);

                    var requiredSet = requiredNode is JsonArray
                        ? requiredNode.AsArray()
                            .Select(v => v?.GetValue<string>())
                            .Where(v => !string.IsNullOrEmpty(v))
                            .Select(v => v!)
                            .ToHashSet()
                        : null;

                    return new ResponsePromptArgument()
                    {
                        Name = input.Key,
                        Description = descriptionNode?.GetValue<string>(),
                        Required = requiredSet?.Contains(input.Key) ?? false,
                    };
                })
                .Where(arg => arg != null)
                .Select(arg => arg!)
                .ToList();
        }
    }
}
