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
using System.Globalization;
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
        /// domain: Mono cannot unload assemblies, so identical requests reuse the already-loaded one (its cache), except
        /// code with static state of its own (a <c>static readonly</c> field of an immutable type does not count), which
        /// Code-Exec loads fresh per run (each copy stays until the domain reloads).
        /// Built without Unity APIs, so it can compile off the main thread (the editor's working directory is the project).
        /// </summary>
        static readonly Lazy<CodeRunner> Runner = new Lazy<CodeRunner>(() => new CodeRunner(new RoslynCSharpCompiler(), new CodeRunnerOptions
        {
            DefaultUsings = DefaultUsings(),
            IncludeRoot = Path.GetFullPath(Path.Combine(".ai-game-dev", "snippets")),
            ValueConverter = value => value, // Mono keeps the assembly loaded: the reflector serializes the live value
            LogCaptures = new List<ILogCapture>(), // Debug.Log stays in the Unity console
            DefaultTimeoutMs = MaxSnippetTimeoutMs, // never "no bound": every request also sets its own budget
        }));

        /// <summary>The longest a snippet runs (its loops, calls and awaits) before Code-Exec stops it with Timeout.</summary>
        internal const int MaxSnippetTimeoutMs = 120_000;

        /// <summary>How much earlier than the server's tool timeout the snippet's budget ends, so its Timeout reaches the agent.</summary>
        internal const int ToolTimeoutMarginMs = 10_000;

        /// <summary>
        /// The snippet's time budget: <see cref="MaxSnippetTimeoutMs"/>, or less when the server's tool timeout is shorter, so
        /// the guard's Timeout answers before the server gives up on the call. Never 0, which Code-Exec reads as the default.
        /// </summary>
        internal static int SnippetTimeoutMs(int toolTimeoutMs) =>
            Math.Max(1_000, Math.Min(MaxSnippetTimeoutMs, toolTimeoutMs - ToolTimeoutMarginMs));

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
            "UnityEditor and AIGD are already there (`Object` and `Random` mean UnityEngine's); an unknown type name is imported " +
            "automatically when exactly one namespace has it.\n" +
            "- **Full code**: when `csharpCode` declares class `className` with a static method `methodName`, that method runs. " +
            "Write its `using` lines (`using UnityEngine;`, `using System.Linq;` …): the default usings apply to snippets only.\n" +
            "- `isMethodBody=true` forces snippet mode.\n\n" +
            "Compile errors point at your own lines (`snippet.cs(2,5)`).\n\n" +
            "## Result and output\n\n" +
            "- The result is bounded: a string to 20000 characters, a collection to 1000 items; a result over 131072 " +
            "characters as JSON comes back as the first 20000 characters of that JSON; errors to 20000 characters and 20 " +
            "compile errors (a note says what was cut). Return a selection or a summary instead of everything.\n" +
            "- `Debug.Log` / `Console.WriteLine` output is not part of the result: `Debug.Log` goes to the Unity Console " +
            "(read it with rql-query `console | sort sequence desc | first 10 | select logType message`).\n" +
            "- Code runs on the main thread with a time budget of 120 s (less when the tool timeout is shorter): an endless loop " +
            "or a long `await` stops with `Execution failed (Timeout)` and the Editor carries on; runaway recursion fails with " +
            "`Recursion too deep`. Code blocked inside one Unity call stops only once that call returns.\n" +
            "- A `.cs` file written in a snippet compiles once imported (`AssetDatabase.ImportAsset` / `Refresh`); the scripts " +
            "reload after the call: the reload waits while a snippet runs, so its reply arrives, but the snippet cannot use the new " +
            "types and must not wait for the compile (`EditorApplication.isCompiling` stays true until the reload; a snippet still " +
            "running 10 s after a compile is requested is stopped). A call made while the scripts compile is not run (it would hold " +
            "the reload back) and says so: retry it after a few seconds, or wait with rql-query `editor | select isCompiling`. Use the " +
            "new types in a later call (retry one that fails during the reload).\n" +
            "- Every run starts fresh: static fields of your own types do not keep values from an earlier call, except a " +
            "`static readonly` field of an immutable type (string, number, enum, read-only collection): identical code sets it " +
            "once per domain, so read live Editor state in a method or property, not in such an initializer.\n\n" +
            "## usings / include\n\n" +
            "- `usings`: extra namespaces, e.g. `[\"MyGame.Gameplay\"]`.\n" +
            "- `include`: helper `.cs` files or folders (recursive) inside `<Project>/.ai-game-dev/snippets` (paths relative to it), " +
            "compiled with the snippet.\n\n" +
            "## Passing Unity objects as parameters\n\n" +
            "Parameters become the snippet's variables (or the method's arguments, in order):\n\n" +
            "- `UnityEngine.GameObject` — resolves an actual GameObject from value `{\"instanceID\": N}`, `{\"name\": \"...\"}`, or `{\"path\": \"...\"}`.\n" +
            "- `UnityEngine.Component` (or any component subtype) — resolves from `{\"instanceID\": N}`.\n" +
            "- `AIGD.GameObjectRef` — passes a `GameObjectRef` POCO directly; call `goRef.FindGameObject()` to resolve it.\n" +
            "- `AIGD.ComponentRef` — passes a `ComponentRef` POCO.\n" +
            "- `AIGD.ObjectRef` — passes a base `ObjectRef` POCO.")]
        [Description("Compiles and runs C# in the Unity Editor: plain statements with the last expression as the result " +
            "(own usings, local classes, await allowed), or a declared class 'className' with static method 'methodName' (that code writes its own using lines). " +
            "After writing and importing a .cs file, use its types in a later call (scripts reload after this one).")]
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
                Usings = WithHostAliases(csharpCode, usings ?? Array.Empty<string>()),
                Include = include ?? Array.Empty<string>(),
                Parameters = parameters?.Select((p, i) => new SnippetParameter(ParameterName(p, i), ParameterType(p, i), null)).ToList()
                    ?? (IReadOnlyList<SnippetParameter>)Array.Empty<SnippetParameter>(),
                TimeoutMs = SnippetTimeoutMs(UnityMcpPluginEditor.EffectiveToolTimeoutMs),
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
            if (DeferForReload())
                throw new Exception(ReloadPendingMessage); // run nothing: the reload goes first
            var logger = UnityLoggerFactory.LoggerFactory.CreateLogger("Tool_Script.Execute");
            var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
            if (parameters != null)
                prepared = prepared.WithValues(parameters.Select(p => reflector.Deserialize(data: p, logger: logger)).ToList());

            var (result, stoppedForReload) = await RunHoldingReload(prepared);
            if (stoppedForReload)
                throw new Exception(ReloadStopMessage);
            if (!result.Success)
                throw new Exception(Describe(result));

            if (result.Value is null)
            {
                // Typed null reports the result type (C# name from Code-Exec) with a JSON null (not the string "null");
                // nothing returned is System.Void "Success".
                var isVoid = result.ReturnType == "System.Void";
                return Scalar(result.ReturnType ?? JsonSchema.Object, isVoid ? "\"Success\"" : "null");
            }
            if (result.Value is SerializedMember serialized)
                return serialized;

            // JSON has no NaN / Infinity: serializing one fails the whole call with a JSON writer error.
            if ((result.Value is double d && !double.IsFinite(d)) || (result.Value is float f && !float.IsFinite(f)))
                return Scalar(result.Value.GetType().FullName!, System.Text.Json.JsonSerializer.Serialize(Convert.ToString(result.Value, CultureInfo.InvariantCulture)));

            // Code-Exec clips only text values; the live object Unity's converter passes through is bounded here, as JSON.
            // A collection of large items keeps the share of its items that fits (with the "… more items" note): whole items
            // beat the first characters of the JSON.
            // Only a counted collection is shaped again: a lazy query would run (and repeat its side effects) once more.
            var (serializedResult, json, count) = SerializeShaped(ResultValue.MaxItems);
            if (json.Length > ResultValue.MaxSerializedChars && ResultValue.IsCounted(result.Value) && count > ResultValue.MinItems)
                (serializedResult, json, _) = SerializeShaped(Math.Max(ResultValue.MinItems, (int)(count * 0.9 * ResultValue.MaxSerializedChars / json.Length)));
            return json.Length <= ResultValue.MaxSerializedChars
                ? serializedResult
                : Scalar(serializedResult.typeName ?? JsonSchema.Object, System.Text.Json.JsonSerializer.Serialize(
                    OutputLimits.Clip(json, OutputLimits.DefaultValueChars, "this is the result as JSON; " + OutputLimits.ValueHint)));

            // The shaped result, its JSON and how many items it holds (-1: not a collection).
            (SerializedMember, string, int) SerializeShaped(int maxItems)
            {
                var shaped = ResultValue.Shape(result.Value, maxItems);
                var member = reflector.Serialize(obj: shaped, logger: logger);
                if (string.IsNullOrEmpty(member.name))
                    member.name = JsonSchema.Result;
                return (member, System.Text.Json.JsonSerializer.Serialize(member, Compact(reflector.JsonSerializerOptions)),
                    shaped is System.Collections.ICollection items ? items.Count : -1);
            }
        }

        // The reflector's options without indentation, built once per options instance (System.Text.Json caches its
        // metadata per instance, so a fresh copy per call would rebuild it every time).
        static (System.Text.Json.JsonSerializerOptions Source, System.Text.Json.JsonSerializerOptions Compact)? _compact;

        static System.Text.Json.JsonSerializerOptions Compact(System.Text.Json.JsonSerializerOptions source)
        {
            if (_compact is { } c && ReferenceEquals(c.Source, source)) return c.Compact;
            var compact = new System.Text.Json.JsonSerializerOptions(source) { WriteIndented = false };
            _compact = (source, compact);
            return compact;
        }

        static SerializedMember Scalar(string typeName, string json) =>
            new SerializedMember { name = JsonSchema.Result, typeName = typeName }.SetJsonValue(json);

        /// <summary>
        /// With the default usings System and UnityEngine, <c>Object</c> and <c>Random</c> are ambiguous (CS0104) — the two
        /// names Unity code uses most. The aliases pick UnityEngine's, unless the code or <paramref name="usings"/> declares
        /// that name itself (a second alias would be CS1537).
        /// </summary>
        internal static string[] WithHostAliases(string code, string[] usings)
        {
            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code, ScriptParseOptions).GetRoot();
            var declared = new HashSet<string>(root.Usings.Where(u => u.Alias != null).Select(u => u.Alias!.Name.Identifier.Text));
            declared.UnionWith(root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Select(t => t.Identifier.Text));
            foreach (var u in usings)
                if (u.IndexOf('=') is var eq && eq > 0)
                {
                    // "Random = System.Random" or, as Code-Exec also accepts, "using Random = System.Random;".
                    var name = u.Substring(0, eq).Trim();
                    declared.Add(name.StartsWith("using ", StringComparison.Ordinal) ? name.Substring("using ".Length).Trim() : name);
                }
            return usings.Concat(HostAliases.Where(a => !declared.Contains(a.Name)).Select(a => $"{a.Name} = {a.Target}")).ToArray();
        }

        static readonly (string Name, string Target)[] HostAliases = { ("Object", "UnityEngine.Object"), ("Random", "UnityEngine.Random") };

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
            // Code-Exec bounds both: at most 20 diagnostics (plus a CE0002 count of the rest) and a clipped error.
            var text = result.ErrorKind == ErrorKind.Compile
                ? "Compilation failed:\n" + string.Join("\n", result.Diagnostics.Select(d => d.ToString()))
                : $"Execution failed ({result.ErrorKind}): {result.Error}";
            if (result.AutoImported.Count > 0)
                text += "\nAuto-imported: " + string.Join(", ", result.AutoImported);
            if (result.Truncated.Count > 0)
                text += "\nTruncated: " + string.Join(", ", result.Truncated);
            return text;
        }
    }
}
