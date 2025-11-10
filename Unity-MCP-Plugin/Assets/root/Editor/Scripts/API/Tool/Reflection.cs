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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Reflection
    {
        static IEnumerable<Type> AllTypes => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes());

        static IEnumerable<MethodInfo> AllMethods => AllTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .Where(method => method.DeclaringType != null && !method.DeclaringType.IsAbstract);

        static int Compare(string original, string value)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(value))
                return 0;

            if (original.Equals(value, StringComparison.OrdinalIgnoreCase))
                return original.Equals(value, StringComparison.Ordinal)
                    ? 6
                    : 5;

            if (original.StartsWith(value, StringComparison.OrdinalIgnoreCase))
                return original.StartsWith(value)
                    ? 4
                    : 3;

            if (original.Contains(value, StringComparison.OrdinalIgnoreCase))
                return original.Contains(value)
                    ? 2
                    : 1;

            return 0;
        }

        static int Compare(ParameterInfo[] original, List<MethodRef.Parameter> value)
        {
            if (original == null && value == null)
                return 2;

            if (original == null || value == null)
                return 0;

            if (original.Length != value.Count)
                return 0;

            for (int i = 0; i < original.Length; i++)
            {
                var parameter = original[i];
                var methodRefParameter = value[i];

                if (parameter.Name != methodRefParameter.Name)
                    return 1;

                if (parameter.ParameterType.IsMatch(methodRefParameter.TypeName) == false)
                    return 1;
            }

            return 2;
        }

        static IEnumerable<MethodInfo> FindMethods(
            MethodRef filter,
            bool knownNamespace = false,
            int typeNameMatchLevel = 1,
            int methodNameMatchLevel = 1,
            int parametersMatchLevel = 2,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        {
            // Prepare Namespace
            filter.Namespace = filter.Namespace?.Trim()?.Replace("null", string.Empty);
            if (string.IsNullOrEmpty(filter.TypeName))
                filter.TypeName = null!;

            var typesEnumerable = AllTypes
                .Where(type => type.IsVisible)
                .Where(type => !type.IsInterface)
                .Where(type => !type.IsAbstract || type.IsSealed)
                .Where(type => !type.IsGenericTypeDefinition); // ignore generic types (class Foo<T>)

            if (knownNamespace)
                typesEnumerable = typesEnumerable.Where(type => type.Namespace == filter.Namespace);

            if (typeNameMatchLevel > 0 && !string.IsNullOrEmpty(filter.TypeName))
                typesEnumerable = typesEnumerable
                    .Select(type => new
                    {
                        Type = type,
                        MatchLevel = Compare(type.Name, filter.TypeName)
                    })
                    .Where(entry => entry.MatchLevel >= typeNameMatchLevel)
                    .OrderByDescending(entry => entry.MatchLevel)
                    .Select(entry => entry.Type);

            var types = typesEnumerable.ToList();

            var methodEnumerable = types
                .SelectMany(type => type.GetMethods(bindingFlags)
                    // Is declared in the class
                    .Where(method => method.DeclaringType == type))
                .Where(method => method.DeclaringType != null)
                .Where(method => !method.DeclaringType.IsAbstract || method.DeclaringType.IsSealed) // ignore abstract non static classes
                .Where(method => !method.IsGenericMethodDefinition); // ignore generic methods (void Foo<T>)

            if (methodNameMatchLevel > 0 && !string.IsNullOrEmpty(filter.MethodName))
                methodEnumerable = methodEnumerable
                    .Select(method => new
                    {
                        Method = method,
                        MatchLevel = Compare(method.Name, filter.MethodName)
                    })
                    .Where(entry => entry.MatchLevel >= methodNameMatchLevel)
                    .OrderByDescending(entry => entry.MatchLevel)
                    .Select(entry => entry.Method);

            if (parametersMatchLevel > 0)
                methodEnumerable = methodEnumerable
                    .Select(method => new
                    {
                        Method = method,
                        MatchLevel = Compare(method.GetParameters(), filter.InputParameters!)
                    })
                    .Where(entry => entry.MatchLevel >= parametersMatchLevel)
                    .OrderByDescending(entry => entry.MatchLevel)
                    .Select(entry => entry.Method);

            return methodEnumerable;
        }

        public static class Error
        {
            public static string MoreThanOneMethodFound(List<MethodInfo> methods)
            {
                var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;
                var methodsString = methods
                    .Select(method => new MethodData(reflector, method, justRef: false))
                    .ToJson(reflector);

                return @$"[Error] Found more than one method. Only single method should be targeted. Please specify the method name more precisely.
Found {methods.Count} method(s):
```json
{methodsString}
```";
            }
        }
    }
}
