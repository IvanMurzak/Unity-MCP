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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_Script
    {
        public const string ScriptExecuteToolId = "script-execute";
        [McpPluginTool
        (
            ScriptExecuteToolId,
            Title = "Script / Execute",
            OpenWorldHint = true
        )]
        [Description("Compiles and executes C# code dynamically using Roslyn. " +
            "Supports two modes: full code mode (default) requires a complete class definition, " +
            "while body-only mode (isMethodBody=true) auto-generates the boilerplate so you only " +
            "provide the method body. Unity objects (GameObject, Component, etc.) can be passed as " +
            "parameters using their Ref types (GameObjectRef, ComponentRef, etc.) or directly by type.")]
        public static SerializedMember? Execute
        (
            [Description("C# code to compile and execute. " +
                "In full code mode (default, isMethodBody=false): must define a complete class with a static method. " +
                "Example: 'using UnityEngine; public class Script { public static void Main() { Debug.Log(\"Hello\"); } }'. " +
                "Do NOT use top-level statements. " +
                "In body-only mode (isMethodBody=true): provide only the method body statements. " +
                "The tool auto-generates usings, class, and method header. " +
                "Example body: 'go.SetActive(false);'. " +
                "Custom helper classes can still be defined inline in the body-only string after the main logic, " +
                "but for complex additional class definitions use full code mode instead.")]
            string csharpCode,
            [Description("The name of the class containing the method to execute. " +
                "In body-only mode this becomes the generated class name.")]
            string className = "Script",
            [Description("The name of the method to execute. Must be a static method. " +
                "In body-only mode this becomes the generated method name.")]
            string methodName = "Main",
            [Description("Serialized parameters to pass to the method. Each entry must specify 'name' and 'typeName'. " +
                "Supported parameter types include primitives, strings, and Unity object references: " +
                "- 'UnityEngine.GameObject': resolves an actual GameObject from value '{\"instanceID\": N}', '{\"name\": \"...\"}', or '{\"path\": \"...\"}'. " +
                "- 'UnityEngine.Component' (or any component subtype): resolves from '{\"instanceID\": N}'. " +
                "- 'com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef': passes a GameObjectRef POCO directly; " +
                "  the method body calls goRef.FindGameObject() to resolve it. " +
                "- 'com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef': passes a ComponentRef POCO. " +
                "- 'com.IvanMurzak.Unity.MCP.Runtime.Data.ObjectRef': passes a base ObjectRef POCO. " +
                "If the method does not require parameters, leave this empty.")]
            SerializedMemberList? parameters = null,
            [Description("When true, 'csharpCode' is treated as just the method body. " +
                "The tool auto-generates standard using directives (System, UnityEngine, " +
                "com.IvanMurzak.Unity.MCP.Runtime.Data, com.IvanMurzak.Unity.MCP.Runtime.Extensions, UnityEditor), " +
                "the class definition, and the method signature (void return type). " +
                "Parameters from the 'parameters' list are automatically added to the method signature using their typeName and name. " +
                "When false (default), 'csharpCode' must be a complete C# compilation unit with class and method definitions.")]
            bool isMethodBody = false
        )
        {
            if (string.IsNullOrEmpty(csharpCode))
                throw new Exception($"'{nameof(csharpCode)}' is null or empty. Please provide valid C# code to execute.");

            if (string.IsNullOrEmpty(className))
                throw new Exception($"'{nameof(className)}' cannot be null or empty.");

            if (string.IsNullOrEmpty(methodName))
                throw new Exception($"'{nameof(methodName)}' cannot be null or empty.");

            string codeToCompile;
            if (isMethodBody)
            {
                codeToCompile = GenerateFullCode(csharpCode, className, methodName, parameters);
            }
            else
            {
                if (csharpCode.Contains(className) == false)
                    throw new Exception($"'{nameof(csharpCode)}' does not contain class '{className}'. Please ensure the class is defined in the provided code.");

                if (csharpCode.Contains(methodName) == false)
                    throw new Exception($"'{nameof(csharpCode)}' does not contain method '{methodName}'. Please ensure the method is defined in the provided code.");

                codeToCompile = csharpCode;
            }

            return MainThread.Instance.Run(() =>
            {
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger("Tool_Script.Execute");

                // Compile C# code using Roslyn and execute it immediately
                if (!ExecuteCSharpCode(
                    className: className,
                    methodName: methodName,
                    code: codeToCompile,
                    parameters: parameters,
                    returnValue: out var result,
                    error: out var error,
                    logger: logger))
                {
                    throw new Exception(error);
                }

                if (result is null)
                    return null;

                if (result is SerializedMember serializedResult)
                    return serializedResult;

                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");

                return reflector.Serialize(
                    obj: result,
                    logger: logger);
            });
        }

        static string GenerateFullCode(
            string methodBody,
            string className,
            string methodName,
            SerializedMemberList? parameters)
        {
            var sb = new StringBuilder();

            // Standard using directives
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using UnityEngine.SceneManagement;");
            sb.AppendLine("using com.IvanMurzak.Unity.MCP.Runtime.Data;");
            sb.AppendLine("using com.IvanMurzak.Unity.MCP.Runtime.Extensions;");
            sb.AppendLine("using UnityEditor;");
            sb.AppendLine();

            // Build method parameter list from the parameters SerializedMemberList
            var methodParams = parameters != null && parameters.Count > 0
                ? string.Join(", ", parameters.Select(p => $"{p.typeName ?? "object"} {p.name ?? "param"}"))
                : "";

            sb.AppendLine($"public class {className}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static void {methodName}({methodParams})");
            sb.AppendLine("    {");

            // Indent each line of the method body
            foreach (var line in methodBody.Split('\n'))
                sb.AppendLine($"        {line.TrimEnd()}");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        static bool ExecuteCSharpCode(
            string className,
            string methodName,
            string code,
            SerializedMemberList? parameters,
            out object? returnValue,
            out string? error,
            ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(className))
            {
                returnValue = null;
                error = $"'{nameof(className)}' cannot be null or empty.";
                return false;
            }
            if (string.IsNullOrEmpty(methodName))
            {
                returnValue = null;
                error = $"'{nameof(methodName)}' cannot be null or empty.";
                return false;
            }

            var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");

            var parsedParameters = parameters
                ?.Select(p => reflector.Deserialize(
                    data: p,
                    logger: logger))
                ?.ToArray();

            var compilation = CSharpCompilation.Create(
                assemblyName: "DynamicAssembly",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(code) },
                references: AssemblyUtils.AllAssemblies
                    .Where(a => !a.IsDynamic) // Exclude dynamic assemblies
                    .Where(a => !string.IsNullOrEmpty(a.Location))
                    .Select(a => MetadataReference.CreateFromFile(a.Location))
                    .ToArray(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    error = $"Compilation failed:\n{string.Join("\n", result.Diagnostics.Select(d => d.ToString()))}";
                    returnValue = null;
                    return false;
                }
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());
                var type = assembly.GetType(className);
                if (type == null)
                {
                    error = $"Class '{className}' not found in the compiled assembly.";
                    returnValue = null;
                    return false;
                }
                var method = type.GetMethod(methodName);
                if (method == null)
                {
                    error = $"Method '{methodName}' not found in class '{className}'.";
                    returnValue = null;
                    return false;
                }
                try
                {
                    returnValue = method.Invoke(null, parsedParameters);
                    error = null;
                    return true;
                }
                catch (TargetInvocationException ex)
                {
                    error = $"Execution failed. TargetInvocationException: {ex.InnerException?.Message ?? ex.Message}\n{ex.InnerException?.StackTrace ?? ex.StackTrace}";
                    returnValue = null;
                    return false;
                }
                catch (Exception ex)
                {
                    error = $"Execution failed: {ex.InnerException?.Message ?? ex.Message}\n{ex.InnerException?.StackTrace ?? ex.StackTrace}";
                    returnValue = null;
                    return false;
                }
            }
        }
    }
}
