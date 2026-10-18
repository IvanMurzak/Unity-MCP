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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using com.IvanMurzak.CodeExec;
using com.IvanMurzak.CodeExec.Roslyn;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_Script
    {
        public const string ScriptExecuteToolId = "script-execute";

        /// <summary>
        /// Compiles and runs agent C# through the vendored Code-Exec runtime (Editor/Scripts/CodeExec). One runner per
        /// domain: Mono cannot unload assemblies, so identical requests reuse the already-loaded one (its cache).
        /// </summary>
        static readonly Lazy<CodeRunner> Runner = new Lazy<CodeRunner>(() => new CodeRunner(new RoslynCSharpCompiler(), new CodeRunnerOptions
        {
            DefaultUsings = DefaultUsings(),
            IncludeRoot = Path.Combine(UnityMcpPluginEditor.ProjectRootPath, ".ai-game-dev", "snippets"),
            Host = new DefaultAssemblyHost(),
            ValueConverter = value => value, // Mono keeps the assembly loaded: the reflector serializes the live value
            LogCaptures = new List<ILogCapture>(), // Debug.Log stays in the Unity console
        }));

        [AiTool
        (
            ScriptExecuteToolId,
            Title = "Script / Execute",
            OpenWorldHint = true
        )]
        [AiSkillDescription("Compiles and runs C# in the Unity Editor (Roslyn): plain statements with the last expression as the result, " +
            "or a full class with a static method. See the skill body for the modes, usings/include and Unity object parameters.")]
        [AiSkillBody(
            "## Modes\n\n" +
            "- **Snippet** (default): plain statements. The last expression is the result (`GameObject.Find(\"Player\").name`). " +
            "Own `using` lines, local classes and functions are allowed; `await` works. Usings for System, LINQ, UnityEngine, " +
            "UnityEditor and AIGD are already there; an unknown type name is imported automatically when exactly one namespace has it.\n" +
            "- **Full code**: when `csharpCode` declares class `className` with a static method `methodName`, that method runs.\n" +
            "- `isMethodBody=true` forces snippet mode.\n\n" +
            "Compile errors point at your own lines (`snippet.cs(2,5)`).\n\n" +
            "## usings / include\n\n" +
            "- `usings`: extra namespaces, e.g. `[\"MyGame.Gameplay\"]`.\n" +
            "- `include`: helper `.cs` files or folders (recursive), relative to `<Project>/.ai-game-dev/snippets`, compiled with the snippet.\n\n" +
            "## Passing Unity objects as parameters\n\n" +
            "Parameters become the snippet's variables (or the method's arguments, in order):\n\n" +
            "- `UnityEngine.GameObject` — resolves an actual GameObject from value `{\"instanceID\": N}`, `{\"name\": \"...\"}`, or `{\"path\": \"...\"}`.\n" +
            "- `UnityEngine.Component` (or any component subtype) — resolves from `{\"instanceID\": N}`.\n" +
            "- `AIGD.GameObjectRef` — passes a `GameObjectRef` POCO directly; call `goRef.FindGameObject()` to resolve it.\n" +
            "- `AIGD.ComponentRef` — passes a `ComponentRef` POCO.\n" +
            "- `AIGD.ObjectRef` — passes a base `ObjectRef` POCO.")]
        [Description("Compiles and runs C# in the Unity Editor. Snippet mode (default): plain statements, the last expression is " +
            "the result, own usings / local classes / await allowed, errors point at snippet lines. Full mode: when the code " +
            "declares class 'className' with static method 'methodName', that method runs. Unity objects can be passed as parameters.")]
        public static Task<SerializedMember?> Execute
        (
            [Description("C# code. Snippet: 'var go = GameObject.Find(\"Player\");\\ngo.transform.position'. " +
                "Full: 'public class Script { public static int Main() { return 42; } }'.")]
            string csharpCode,
            [Description("Full mode: the class whose static method runs (namespace-qualified or not).")]
            string className = "Script",
            [Description("Full mode: the static method to run.")]
            string methodName = "Main",
            [Description("Parameters, each with 'name' and 'typeName'; snippet variables or the method's arguments in order. " +
                "Unity references: 'UnityEngine.GameObject' from '{\"instanceID\": N}', '{\"name\": \"...\"}' or '{\"path\": \"...\"}'; " +
                "'UnityEngine.Component' (or a subtype) from '{\"instanceID\": N}'; 'AIGD.GameObjectRef' / 'AIGD.ComponentRef' / " +
                "'AIGD.ObjectRef' as POCOs. Leave empty when none.")]
            [Rql.ToolFormat.RqlArgument("Values for the snippet's / method's parameters by name, e.g. {go: ref(\"GameObject#12\"), n: 6}. " +
                "Leave empty if there are none.")]
            SerializedMemberList? parameters = null,
            [Description("Force snippet mode even when the code declares 'className'.")]
            bool isMethodBody = false,
            [Description("Extra namespaces for snippet mode, e.g. [\"MyGame.Gameplay\"].")]
            string[]? usings = null,
            [Description("Helper .cs files or folders (recursive) compiled with the code, relative to <Project>/.ai-game-dev/snippets.")]
            string[]? include = null
        )
        {
            if (string.IsNullOrEmpty(csharpCode))
                throw new Exception($"'{nameof(csharpCode)}' is null or empty. Please provide valid C# code to execute.");
            if (string.IsNullOrEmpty(className))
                throw new Exception($"'{nameof(className)}' cannot be null or empty.");
            if (string.IsNullOrEmpty(methodName))
                throw new Exception($"'{nameof(methodName)}' cannot be null or empty.");

            // On the main thread the lambda runs inline; awaits inside resume on Unity's main-thread context.
            return MainThread.Instance.RunAsync(() => ExecuteOnMainThread(csharpCode, className, methodName, parameters, isMethodBody, usings, include)).Unwrap();
        }

        static async Task<SerializedMember?> ExecuteOnMainThread(string csharpCode, string className, string methodName,
            SerializedMemberList? parameters, bool isMethodBody, string[]? usings, string[]? include)
        {
            var logger = UnityLoggerFactory.LoggerFactory.CreateLogger("Tool_Script.Execute");
            var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");

            var request = new ExecuteRequest
            {
                Code = csharpCode,
                Usings = usings ?? Array.Empty<string>(),
                Include = include ?? Array.Empty<string>(),
                Parameters = ToSnippetParameters(parameters, reflector, logger),
                TimeoutMs = 120_000,
            };
            if (!isMethodBody)
            {
                request.EntryType = FullSourceEntryType(csharpCode, className, methodName);
                request.EntryMethod = request.EntryType == null ? null : methodName;
            }

            var result = await Runner.Value.ExecuteAsync(request);
            if (!result.Success)
                throw new Exception(Describe(result));

            if (result.Value is null)
            {
                var isVoid = result.ReturnType == "System.Void";
                var member = new SerializedMember
                {
                    name = JsonSchema.Result,
                    typeName = isVoid ? "System.Void" : (result.ReturnType ?? JsonSchema.Object)
                };
                return member.SetJsonValue(isVoid ? "\"Success\"" : "\"null\"");
            }
            if (result.Value is SerializedMember serialized)
                return serialized;

            var serializedResult = reflector.Serialize(obj: result.Value, logger: logger);
            if (string.IsNullOrEmpty(serializedResult.name))
                serializedResult.name = JsonSchema.Result;
            return serializedResult;
        }

        static string[] DefaultUsings()
        {
            var usings = new List<string>
            {
                "System", "System.Collections", "System.Collections.Generic", "System.Linq", "System.Threading.Tasks",
                "UnityEngine", "UnityEngine.SceneManagement", "AIGD", "com.IvanMurzak.Unity.MCP.Runtime.Extensions", "UnityEditor",
            };
            // UnityEngine.UI ships in com.unity.ugui: only when its assembly is loaded, or Roslyn fails with CS0234.
            if (AssemblyUtils.AllAssemblies.Any(a => string.Equals(a.GetName().Name, "UnityEngine.UI", StringComparison.Ordinal)))
                usings.Add("UnityEngine.UI");
            return usings.ToArray();
        }

        static IReadOnlyList<SnippetParameter> ToSnippetParameters(SerializedMemberList? parameters, Reflector reflector, ILogger logger)
        {
            if (parameters == null || parameters.Count == 0)
                return Array.Empty<SnippetParameter>();
            return parameters.Select((p, i) =>
            {
                var value = reflector.Deserialize(data: p, logger: logger);
                var type = TypeUtils.GetType(p.typeName) ?? value?.GetType() ?? typeof(object);
                return new SnippetParameter(p.name ?? $"param{i}", type, value);
            }).ToList();
        }

        /// <summary>
        /// Full mode when the code declares class <paramref name="className"/> (namespace-qualified or not) with a static
        /// method <paramref name="methodName"/>: returns its runtime type name. Null = run the code as a snippet.
        /// </summary>
        internal static string? FullSourceEntryType(string code, string className, string methodName)
        {
            var root = CSharpSyntaxTree.ParseText(code).GetRoot();
            foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var fullName = RuntimeName(type);
                if (type.Identifier.Text != className && fullName != className)
                    continue;
                if (type.Members.OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.Text == methodName && m.Modifiers.Any(SyntaxKind.StaticKeyword)))
                    return fullName;
                throw new Exception($"Class '{className}' is declared, but it has no static method '{methodName}'.");
            }
            return null;
        }

        static string RuntimeName(TypeDeclarationSyntax type)
        {
            var name = type.Identifier.Text;
            for (var parent = type.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is TypeDeclarationSyntax outer)
                    name = outer.Identifier.Text + "+" + name;
                else if (parent is BaseNamespaceDeclarationSyntax ns)
                    name = ns.Name + "." + name;
            }
            return name;
        }

        static string Describe(ExecuteResult result)
        {
            var text = result.ErrorKind == ErrorKind.Compile
                ? "Compilation failed:\n" + string.Join("\n", result.Diagnostics.Select(d => d.ToString()))
                : $"Execution failed ({result.ErrorKind}): {result.Error}";
            if (result.AutoImported.Count > 0)
                text += "\nAuto-imported: " + string.Join(", ", result.AutoImported);
            return text;
        }
    }
}
