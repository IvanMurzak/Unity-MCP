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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_Script
    {
        [McpPluginTool
        (
            "Script_Execute",
            Title = "Execute C# code immediately"
        )]
        [Description("Compiles and executes C# code dynamically using Roslyn. The provided code must define a class with a static method to execute.")]
        public static string Execute
        (
            [Description(@"C# code that compiles and executes immediately. It won't be stored as a script in the project. It is temporary one shot C# code execution using Roslyn.
IMPORTANT: The code must define a class (e.g., 'public class Script') with a static method (e.g., 'public static object Main()').
Do NOT use top-level statements or code outside a class. Top-level statements are not supported and will cause compilation errors.")]
            string csharpCode,
            [Description("The name of the class containing the method to execute.")]
            string className = "Script",
            [Description("The name of the method to execute. It must be a static method in the class provided above.")]
            string methodName = "Main",
            [Description("Serialized parameters to pass to the method. If the method does not require parameters, leave this empty.")]
            SerializedMemberList? parameters = null
        )
        {
            if (string.IsNullOrEmpty(csharpCode))
                return $"[Error] '{nameof(csharpCode)}' is null or empty. Please provide valid C# code to execute.";

            if (string.IsNullOrEmpty(className))
                return $"[Error] '{nameof(className)}' cannot be null or empty.";

            if (string.IsNullOrEmpty(methodName))
                return $"[Error] '{nameof(methodName)}' cannot be null or empty.";

            if (csharpCode.Contains(className) == false)
                return $"[Error] '{nameof(csharpCode)}' does not contain class '{className}'. Please ensure the class is defined in the provided code.";

            if (csharpCode.Contains(methodName) == false)
                return $"[Error] '{nameof(csharpCode)}' does not contain method '{methodName}'. Please ensure the method is defined in the provided code.";

            return MainThread.Instance.Run(() =>
            {
                // Compile C# code using Roslyn and execute it immediately
                if (ExecuteCSharpCode(className, methodName, csharpCode, parameters, out var result, out var error) == false)
                    return $"[Error] {error}";

                return $"[Success] {result}";
            });
        }
        static bool ExecuteCSharpCode(string className, string methodName, string code, SerializedMemberList? parameters, out object? returnValue, out string? error)
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

            var parsedParameters = parameters
                ?.Select(p => McpPlugin.Instance!.McpRunner.Reflector.Deserialize(p, logger: McpPlugin.Instance.Logger))
                ?.ToArray();

            var compilation = CSharpCompilation.Create(
                assemblyName: "DynamicAssembly",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(code) },
                references: AppDomain.CurrentDomain.GetAssemblies()
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
