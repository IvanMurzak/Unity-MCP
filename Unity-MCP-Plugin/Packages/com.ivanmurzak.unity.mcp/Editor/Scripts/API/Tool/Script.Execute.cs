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
        /// Built without Unity APIs, so it can compile off the main thread (the editor's working directory is the project).
        /// </summary>
        static readonly Lazy<CodeRunner> Runner = new Lazy<CodeRunner>(() => new CodeRunner(new RoslynCSharpCompiler(), new CodeRunnerOptions
        {
            DefaultUsings = DefaultUsings(),
            IncludeRoot = Path.GetFullPath(Path.Combine(".ai-game-dev", "snippets")),
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
        [Description("Compiles and runs C# in the Unity Editor: plain statements with the last expression as the result " +
            "(own usings, local classes, await allowed), or a declared class 'className' with static method 'methodName'.")]
        public static async Task<SerializedMember?> Execute
        (
            [Description("C# code. Snippet: 'var go = GameObject.Find(\"Player\");\\ngo.transform.position'. " +
                "Full: 'public class Script { public static int Main() { return 42; } }'.")]
            string csharpCode,
            [Description("Full mode: the class whose static method runs (namespace-qualified or not).")]
            string className = "Script",
            [Description("Full mode: the static method to run.")]
            string methodName = "Main",
            [Description("Parameters, each with 'name' and 'typeName': the snippet's variables or the method's arguments in order. " +
                "Unity object types and value formats: see the skill. Leave empty when none.")]
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

            // Compile on the calling thread (MCP calls arrive off the main thread): only the parameter TYPES are needed
            // here. On the main thread (e.g. tests) everything simply runs inline, as before.
            var request = new ExecuteRequest
            {
                Code = csharpCode,
                Usings = usings ?? Array.Empty<string>(),
                Include = include ?? Array.Empty<string>(),
                Parameters = parameters?.Select((p, i) => new SnippetParameter(ParameterName(p, i), ParameterType(p, i), null)).ToList()
                    ?? (IReadOnlyList<SnippetParameter>)Array.Empty<SnippetParameter>(),
                TimeoutMs = 120_000,
            };
            if (!isMethodBody && FullSourceEntryType(csharpCode, className, methodName) is { } entryType)
            {
                request.EntryType = entryType;
                request.EntryMethod = methodName;
            }
            var prepared = Runner.Value.Prepare(request);
            if (!prepared.Succeeded)
                throw new Exception(Describe(Runner.Value.Run(prepared))); // Run only returns the failure: no main-thread hop

            // Unity object references deserialize and the snippet runs on the main thread; awaits inside resume there.
            return await MainThread.Instance.RunAsync(() => RunOnMainThread(prepared, parameters)).Unwrap();
        }

        static async Task<SerializedMember?> RunOnMainThread(PreparedSnippet prepared, SerializedMemberList? parameters)
        {
            var logger = UnityLoggerFactory.LoggerFactory.CreateLogger("Tool_Script.Execute");
            var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
            if (parameters != null)
                prepared = prepared.WithValues(parameters.Select(p => reflector.Deserialize(data: p, logger: logger)).ToList());

            var result = await Runner.Value.RunAsync(prepared);
            if (!result.Success)
                throw new Exception(Describe(result));

            if (result.Value is null)
            {
                // Typed null reports the result type (C# name from Code-Exec); nothing returned is System.Void "Success".
                var isVoid = result.ReturnType == "System.Void";
                var member = new SerializedMember
                {
                    name = JsonSchema.Result,
                    typeName = result.ReturnType ?? JsonSchema.Object
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

        static string ParameterName(SerializedMember parameter, int index) => parameter.name ?? $"param{index}";

        /// <summary>An unknown type name fails here, by name, instead of surfacing as an untyped <c>object</c> variable.</summary>
        static Type ParameterType(SerializedMember parameter, int index) =>
            string.IsNullOrEmpty(parameter.typeName)
                ? typeof(object)
                : TypeUtils.GetType(parameter.typeName)
                    ?? throw new Exception($"Parameter '{ParameterName(parameter, index)}': type '{parameter.typeName}' not found.");

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

        /// <summary>
        /// Full mode when the code declares class <paramref name="className"/> (namespace-qualified or not) with a static
        /// method <paramref name="methodName"/>: returns its runtime type name. Null = run the code as a snippet.
        /// Code with top-level statements is always a snippet (its own helper types may reuse the name); code with only
        /// type declarations and none of them the entry point is an error, since as a snippet nothing would run.
        /// </summary>
        internal static string? FullSourceEntryType(string code, string className, string methodName)
        {
            // Parsed as a script, the way the snippet writer reads it: anything but a type or namespace is snippet code.
            var script = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code, ScriptParseOptions).GetRoot();
            if (script.Members.Any(m => !(m is BaseNamespaceDeclarationSyntax || m is BaseTypeDeclarationSyntax || m is DelegateDeclarationSyntax)))
                return null;
            var root = CSharpSyntaxTree.ParseText(code).GetRoot();
            var types = root.DescendantNodes(n => n is CompilationUnitSyntax || n is BaseNamespaceDeclarationSyntax || n is TypeDeclarationSyntax)
                .OfType<TypeDeclarationSyntax>()
                .ToList();
            if (types.Count == 0)
                return null;
            foreach (var type in types)
            {
                var fullName = RuntimeName(type);
                if (type.Identifier.Text != className && fullName != className)
                    continue;
                if (type.Members.OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.Text == methodName && m.Modifiers.Any(SyntaxKind.StaticKeyword)))
                    return fullName;
                throw new Exception($"Class '{className}' is declared, but it has no static method '{methodName}'.");
            }
            throw new Exception($"'csharpCode' declares no class '{className}' and has no statements to run. " +
                "Set 'className' / 'methodName' to the entry point, or write plain statements.");
        }

        static readonly CSharpParseOptions ScriptParseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.None, SourceCodeKind.Script);

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
