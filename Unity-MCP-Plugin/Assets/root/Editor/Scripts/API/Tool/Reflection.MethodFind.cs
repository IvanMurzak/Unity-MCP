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
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Reflection
    {
        public const string ReflectionMethodFindToolId = "reflection-method-find";
        [McpPluginTool
        (
            ReflectionMethodFindToolId,
            Title = "Method C# / Find"
        )]
        [Description("Find method in the project using C# Reflection. " +
            "It looks for all assemblies in the project and finds method by its name, class name and parameters. " +
            "Even private methods are available. " +
            "Use '" + ReflectionMethodCallToolId + "' to call the method after finding it.")]
        public string MethodFind
        (
            MethodRef filter,

            [Description("Set to true if 'Namespace' is known and full namespace name is specified in the 'filter.Namespace' property. Otherwise, set to false.")]
            bool knownNamespace = false,

            [Description("Minimal match level for 'typeName'. " +
                "0 - ignore 'filter.typeName', " +
                "1 - contains ignoring case (default value), " +
                "2 - contains case sensitive, " +
                "3 - starts with ignoring case, " +
                "4 - starts with case sensitive, " +
                "5 - equals ignoring case, " +
                "6 - equals case sensitive.")]
            int typeNameMatchLevel = 1,

            [Description("Minimal match level for 'MethodName'. " +
                "0 - ignore 'filter.MethodName', " +
                "1 - contains ignoring case (default value), " +
                "2 - contains case sensitive, " +
                "3 - starts with ignoring case, " +
                "4 - starts with case sensitive, " +
                "5 - equals ignoring case, " +
                "6 - equals case sensitive.")]
            int methodNameMatchLevel = 1,

            [Description("Minimal match level for 'Parameters'. " +
                "0 - ignore 'filter.Parameters' (default value), " +
                "1 - parameters count is the same, " +
                "2 - equals.")]
            int parametersMatchLevel = 0
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var methodEnumerable = FindMethods(
                    filter: filter,
                    knownNamespace: knownNamespace,
                    typeNameMatchLevel: typeNameMatchLevel,
                    methodNameMatchLevel: methodNameMatchLevel,
                    parametersMatchLevel: parametersMatchLevel);

                var methods = methodEnumerable.ToList();
                if (methods.Count == 0)
                    return $"[Success] Method not found. With request:\n{filter}";

                var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

                var methodRefs = methods
                    .Select(method => new MethodData(reflector, method, justRef: false))
                    .ToList();

                return $@"[Success] Found {methods.Count} method(s):
```json
{methodRefs.ToJson(reflector)}
```";
            });
        }
    }
}
