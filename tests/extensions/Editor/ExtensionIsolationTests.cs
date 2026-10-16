/*
+------------------------------------------------------------------+
| Author: Ivan Murzak (https://github.com/IvanMurzak)                 |
| Repository: https://github.com/IvanMurzak/Unity-MCP                 |
| Copyright (c) 2026 Ivan Murzak. Licensed under Apache-2.0.         |
+------------------------------------------------------------------+
*/
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
using Assembly = UnityEditor.Compilation.Assembly;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace UnityMcp.Monorepo.Tests
{
    public sealed class ExtensionCompatibilityTests
    {
        static readonly object[][] Cases = {
            new object[] { "Navigation", "com.unity.ai.navigation", "2.0.10" },
            new object[] { "ProBuilder", "com.unity.probuilder", "6.1.3" },
            new object[] { "Tilemap", "com.unity.2d.tilemap.extras", "7.0.1" }
        };

        static string Dependency(string file, string name) => System.Text.RegularExpressions.Regex.Match(
            File.ReadAllText(file), "\"" + System.Text.RegularExpressions.Regex.Escape(name)
            + "\"\\s*:\\s*\"([^\"]+)\"").Groups[1].Value;

        [TestCaseSource(nameof(Cases))]
        public void EditorAndSelfContainedInstallerSelectCompatibleDependencies(string feature, string dependency, string minimum)
        {
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ExtensionCompatibility", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "manifest.json");
            void Seed(string version) => File.WriteAllText(file, "{\"dependencies\":{\"" + dependency + "\":\"" + version + "\"}}");
            var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            try
            {
                var editor = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(
                    "com.IvanMurzak.Unity.MCP.Editor.UI.ExtensionPanel")).First(t => t != null)!;
                var add = editor.GetMethod("AddToManifest", flags)!;
                Seed("0.0.1");
                add.Invoke(null, new object[] { file, "com.ivanmurzak.unity.mcp." + feature.ToLowerInvariant(), "2.0.1", "6000.5.0f1" });
                Assert.That(Dependency(file, dependency), Is.EqualTo("0.0.1"));
                add.Invoke(null, new object[] { file, "com.ivanmurzak.unity.mcp." + feature.ToLowerInvariant(), "2.0.1", "6000.6.3f1" });
                Assert.That(Dependency(file, dependency), Is.EqualTo(minimum));
                foreach (var preserved in new[] { "99.0.0", "https://example.invalid/custom.git#pinned" })
                {
                    Seed(preserved);
                    add.Invoke(null, new object[] { file, "com.ivanmurzak.unity.mcp." + feature.ToLowerInvariant(), "2.0.1", "6000.6.3f1" });
                    Assert.That(Dependency(file, dependency), Is.EqualTo(preserved));
                }

                var type = "com.IvanMurzak.Unity.MCP." + feature + ".Installer.Installer";
                var installer = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(type)).First(t => t != null)!;
                var ensure = installer.GetMethod("EnsureUnityPackageCompatibility", flags)!;
                Seed("0.0.1");
                Assert.That(ensure.Invoke(null, new object[] { file, "6000.5.0f1" }), Is.False);
                Assert.That(Dependency(file, dependency), Is.EqualTo("0.0.1"));
                Assert.That(ensure.Invoke(null, new object[] { file, "6000.6.3f1" }), Is.True);
                Assert.That(Dependency(file, dependency), Is.EqualTo(minimum));
                var previous = File.ReadAllText(file);
                Assert.That(ensure.Invoke(null, new object[] { file, "6000.6.3f1" }), Is.False);
                Assert.That(File.ReadAllText(file), Is.EqualTo(previous));
                foreach (var preserved in new[] { "99.0.0", "https://example.invalid/custom.git#pinned" })
                {
                    Seed(preserved);
                    Assert.That(ensure.Invoke(null, new object[] { file, "6000.6.3f1" }), Is.False);
                    Assert.That(Dependency(file, dependency), Is.EqualTo(preserved));
                }
            }
            finally { Directory.Delete(folder, true); }
        }
    }

    /// <summary>Recompile native package assemblies with only their declared dependency closure.
    /// Uses Unity's bundled Roslyn/runtime, never starts another editor or edits imported sources.</summary>
    public sealed class ExtensionIsolationTests
    {
        const string Prefix = "com.ivanmurzak.unity.mcp.";
        readonly string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [Test]
        public void AllExtensionAssembliesCompileWithoutUndeclaredPackages()
        {
            var packages = PackageInfo.GetAllRegisteredPackages().ToDictionary(p => p.name);
            var extensions = packages.Values.Where(p => p.name.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
            Assert.That(extensions.Length, Is.GreaterThanOrEqualTo(10), "Extension packages did not resolve");
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in assemblies)
            {
                var definition = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly.name);
                var package = string.IsNullOrEmpty(definition) ? null : PackageInfo.FindForAssetPath(definition);
                if (package == null && !string.IsNullOrEmpty(definition))
                    package = packages.Values.FirstOrDefault(p => IsInsidePackage(Resolve(definition, packages), p.resolvedPath));
                if (package != null) owners[assembly.name] = package.name;
                else if (assembly.sourceFiles.Any(path => IsInsidePackage(Resolve(path, packages), Application.dataPath)))
                    owners[assembly.name] = "__project_assets__";
            }
            var diagnostics = Path.Combine(project, "Library/ExtensionIsolation");
            Directory.CreateDirectory(diagnostics);
            File.WriteAllLines(Path.Combine(diagnostics, "packages.txt"), packages.Values.OrderBy(p => p.name)
                .Select(p => p.name + "|" + p.source + "|" + string.Join(",", p.dependencies.Select(d => d.name))));
            File.WriteAllLines(Path.Combine(diagnostics, "owners.txt"), owners.OrderBy(p => p.Key).Select(p => p.Key + "|" + p.Value));
            var contexts = new Dictionary<string, IsolatedCompilation>();
            int compiled = 0;
            foreach (var extension in extensions)
            {
                var allowed = Closure(extension.name, packages);
                // Test Framework is provided by Unity's test consumer, not by a shipped runtime package.
                allowed.UnionWith(Closure("com.unity.test-framework", packages));
                // Engine modules are implicit references of ordinary Unity asmdefs.
                allowed.UnionWith(packages.Values.Where(p => p.source == PackageSource.BuiltIn
                    && p.name.StartsWith("com.unity.modules.", StringComparison.Ordinal)).Select(p => p.name));
                var context = new IsolatedCompilation(this, extension.name, assemblies, owners, packages, allowed);
                contexts[extension.name] = context;
                var own = assemblies.Where(a => owners.TryGetValue(a.name, out var owner) && owner == extension.name).ToArray();
                Assert.That(own.Any(a => a.name.EndsWith(".Editor", StringComparison.Ordinal)), Is.True,
                    extension.name + " editor assembly is missing (check compilation defines)");
                Assert.That(own.Any(a => a.name.EndsWith(".Editor.Tests", StringComparison.Ordinal)), Is.True,
                    extension.name + " original editor test assembly was not discovered");
                foreach (var assembly in own.Where(a => a.sourceFiles.Length > 0))
                {
                    context.Get(assembly);
                    compiled++;
                    TestContext.WriteLine("Isolated compilation passed: " + assembly.name);
                }
            }
            Assert.That(compiled, Is.GreaterThanOrEqualTo(20), "No meaningful extension compilation happened");

            // Both sides of this control compile the SAME planted class. Only the sibling DLL differs.
            var animation = assemblies.Single(a => a.name == "com.IvanMurzak.Unity.MCP.Animation.Editor");
            var cinemachine = assemblies.Single(a => a.name == "com.IvanMurzak.Unity.MCP.Cinemachine.Editor");
            var animationContext = contexts[Prefix + "animation"];
            var sibling = Resolve(cinemachine.outputPath, packages);
            // Plant the undeclared reference BEFORE the production filter. Without the filter,
            // this same source compiles, so the control tests exclusion rather than a missing input.
            var planted = animation.allReferences.Append(sibling);
            var filtered = References(animation, animationContext.Allowed, owners, packages, planted).ToArray();
            Assert.That(filtered, Does.Not.Contain(sibling), "The producer's planted undeclared DLL was not excluded");
            var isolated = animationContext.References(animation, filtered);
            var fixture = assemblies.First(a => owners.TryGetValue(a.name, out var owner) && owner == "__project_assets__");
            var fixturePath = Resolve(fixture.outputPath, packages);
            Assert.That(References(animation, animationContext.Allowed, owners, packages, new[] { fixturePath }), Is.Empty,
                "Project/installer fixture assemblies must not leak into native package compilation");
            var fixtureControl = new HashSet<string>(animationContext.Allowed, StringComparer.Ordinal) { "__project_assets__" };
            Assert.That(References(animation, fixtureControl, owners, packages, new[] { fixturePath }), Does.Contain(fixturePath));
            var canary = Path.Combine(project, "Library/ExtensionIsolation/SiblingCanary.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(canary)!);
            File.WriteAllText(canary, "internal static class SiblingCanary { internal static System.Type Missing = typeof(com.IvanMurzak.Unity.MCP.Editor.Rql.Cinemachine.CinemachineModule); }");
            var negative = Compile(animation, isolated, packages, "negative", canary);
            Assert.That(negative.code, Is.Not.Zero, "Undeclared sibling unexpectedly compiled");
            Assert.That(negative.output, Does.Contain("CS0234").Or.Contain("CS0246"), negative.output);
            var positive = Compile(animation, isolated.Append(sibling), packages, "positive", canary);
            Assert.That(positive.code, Is.Zero, "Canary control failed for a reason other than sibling isolation\n" + positive.output);
            TestContext.WriteLine($"Compiled {compiled} native extension assemblies; sibling canary failed without and passed with the sibling DLL.");

            // A declared DLL can expose members enabled only by another installed package.
            // This int field needs no InputSystem type in its signature, so filtering DLLs alone misses it.
            var cmContext = contexts[Prefix + "cinemachine"];
            File.WriteAllText(canary, "internal static class OptionalCanary { internal static int Read(global::Unity.Cinemachine.CinemachineInputAxisController provider) => provider.PlayerIndex; }");
            var optionalNegative = Compile(cinemachine, cmContext.References(cinemachine), packages,
                "negative-optional", canary, cmContext.Defines(cinemachine));
            Assert.That(optionalNegative.code, Is.Not.Zero, "A dependency's conditionally available member leaked into the isolated consumer");
            Assert.That(optionalNegative.output, Does.Contain("CS1061"), optionalNegative.output);
            var withInput = new HashSet<string>(cmContext.Allowed, StringComparer.Ordinal);
            withInput.UnionWith(Closure("com.unity.inputsystem", packages));
            var cmWithInput = new IsolatedCompilation(this, Prefix + "cinemachine", assemblies, owners, packages, withInput, "with-input");
            var optionalPositive = Compile(cinemachine, cmWithInput.References(cinemachine), packages,
                "positive-optional", canary, cmWithInput.Defines(cinemachine));
            Assert.That(optionalPositive.code, Is.Zero, "Conditional-member control did not compile with its explicit dependency\n" + optionalPositive.output);
        }

        [Serializable] sealed class Definition { public VersionDefine[] versionDefines = Array.Empty<VersionDefine>(); }
        [Serializable] sealed class VersionDefine { public string name = ""; public string define = ""; }

        sealed class IsolatedCompilation
        {
            readonly ExtensionIsolationTests host;
            readonly string component, variant;
            readonly Dictionary<string, Assembly> assemblies;
            readonly Dictionary<string, string> owners;
            readonly Dictionary<string, PackageInfo> packages;
            readonly Dictionary<string, string> outputs = new Dictionary<string, string>();
            readonly HashSet<string> visiting = new HashSet<string>();
            public HashSet<string> Allowed { get; }

            public IsolatedCompilation(ExtensionIsolationTests host, string component, Assembly[] assemblies,
                Dictionary<string, string> owners, Dictionary<string, PackageInfo> packages, HashSet<string> allowed,
                string suffix = "")
            {
                this.host = host; this.component = component; this.owners = owners; this.packages = packages;
                this.assemblies = assemblies.ToDictionary(a => a.name, StringComparer.OrdinalIgnoreCase);
                Allowed = allowed;
                variant = "isolated-" + component.Substring(Prefix.Length) + suffix;
            }

            public string[] Defines(Assembly assembly)
            {
                var removed = new HashSet<string>();
                var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly.name);
                if (!string.IsNullOrEmpty(path))
                {
                    var definition = JsonUtility.FromJson<Definition>(File.ReadAllText(Resolve(path, packages)));
                    foreach (var entry in definition.versionDefines ?? Array.Empty<VersionDefine>())
                        if (packages.ContainsKey(entry.name) && !Allowed.Contains(entry.name)) removed.Add(entry.define);
                }
                if (!Allowed.Contains("com.unity.inputsystem")) removed.Add("ENABLE_INPUT_SYSTEM");
                return assembly.defines.Where(d => !removed.Contains(d)).ToArray();
            }

            public string[] References(Assembly assembly, IEnumerable<string>? candidates = null) =>
                ExtensionIsolationTests.References(assembly, Allowed, owners, packages, candidates)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Select(path =>
                        assemblies.TryGetValue(Path.GetFileNameWithoutExtension(path), out var dependency)
                            && dependency.name != assembly.name ? Get(dependency) : path).ToArray();

            public string Get(Assembly assembly)
            {
                if (outputs.TryGetValue(assembly.name, out var cached)) return cached;
                Assert.That(visiting.Add(assembly.name), Is.True, "Cyclic compiler references: " + assembly.name);
                var original = assembly.allReferences.Select(p => Resolve(p, packages))
                    .Where(p => Path.GetFileNameWithoutExtension(p) != assembly.name)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var references = References(assembly);
                var defines = Defines(assembly);
                var removed = assembly.defines.Except(defines).ToArray();
                var changedCondition = removed.Length > 0 && assembly.sourceFiles.Any(path => {
                    var text = File.ReadAllText(Resolve(path, packages));
                    return removed.Any(symbol => System.Text.RegularExpressions.Regex.IsMatch(text,
                        @"(?m)^\s*#(?:if|elif)\b[^\r\n]*\b" + System.Text.RegularExpressions.Regex.Escape(symbol) + @"\b"));
                });
                var own = owners.TryGetValue(assembly.name, out var owner) && owner == component;
                var output = Resolve(assembly.outputPath, packages);
                // allReferences includes auto-referenced DLLs that may not be used at all.
                // Only a dependency's emitted assembly references can require rebuilding it.
                var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assembly.name);
                var used = loaded == null ? null : new HashSet<string>(loaded.GetReferencedAssemblies()
                    .Select(a => a.Name!), StringComparer.OrdinalIgnoreCase);
                var changedReference = original.Except(references, StringComparer.OrdinalIgnoreCase)
                    .Any(path => used == null || used.Contains(Path.GetFileNameWithoutExtension(path)));
                if (assembly.sourceFiles.Length > 0 && (own || changedCondition || changedReference))
                {
                    var result = host.Compile(assembly, references, packages, variant, defines: defines);
                    Assert.That(result.code, Is.Zero, component + "/" + assembly.name + " isolated dependency compilation\n"
                        + "Removed references: " + string.Join(",", original.Except(references).Select(Path.GetFileName)) + "\n" + result.output);
                    output = result.path;
                    TestContext.WriteLine("Isolated source compilation: " + component + "/" + assembly.name);
                }
                visiting.Remove(assembly.name);
                return outputs[assembly.name] = output;
            }
        }

        static HashSet<string> Closure(string name, Dictionary<string, PackageInfo> packages)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            void Visit(string id)
            {
                if (!result.Add(id) || !packages.TryGetValue(id, out var package)) return;
                foreach (var dependency in package.dependencies) Visit(dependency.name);
            }
            Visit(name);
            return result;
        }

        static string Resolve(string path, Dictionary<string, PackageInfo> packages)
        {
            var normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var split = normalized.IndexOf('/', "Packages/".Length);
                if (split > 0 && packages.TryGetValue(normalized.Substring(9, split - 9), out var package))
                    return Path.Combine(package.resolvedPath, normalized.Substring(split + 1));
            }
            return Path.GetFullPath(path);
        }

        static IEnumerable<string> References(Assembly assembly, HashSet<string> allowed,
            Dictionary<string, string> owners, Dictionary<string, PackageInfo> packages, IEnumerable<string>? candidates = null)
        {
            foreach (var reference in candidates ?? assembly.allReferences)
            {
                var full = Resolve(reference, packages);
                var name = Path.GetFileNameWithoutExtension(full);
                if (name == assembly.name) continue;
                if (owners.TryGetValue(name, out var owner) && !allowed.Contains(owner)) continue;
                // Also exclude precompiled dependencies residing inside undeclared UPM packages.
                var packageOwner = packages.Values.FirstOrDefault(p => IsInsidePackage(full, p.resolvedPath));
                if (packageOwner != null && !allowed.Contains(packageOwner.name)) continue;
                yield return full;
            }
        }

        static bool IsInsidePackage(string file, string packagePath) => Path.GetFullPath(file).StartsWith(
            Path.GetFullPath(packagePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        [Test]
        public void PrecompiledPackageOwnershipNormalizesSeparatorsAndKeepsDirectoryBoundaries()
        {
            var package = Path.Combine(project, "Library/PackageCache/com.example.optional");
            var dll = Path.Combine(package, "Plugins/Optional.dll");
            Assert.That(IsInsidePackage(dll, package.Replace('\\', '/')), Is.True,
                "Unity package paths may use forward slashes while resolved file paths use native separators");
            Assert.That(IsInsidePackage(dll, package + Path.DirectorySeparatorChar), Is.True);
            Assert.That(IsInsidePackage(Path.Combine(package + ".other", "Plugins/Optional.dll"), package), Is.False);
        }

        (int code, string output, string path) Compile(Assembly assembly, IEnumerable<string> references,
            Dictionary<string, PackageInfo> packages, string variant, string? extraSource = null, IEnumerable<string>? defines = null)
        {
            var folder = Path.Combine(project, "Library/ExtensionIsolation", assembly.name, variant);
            Directory.CreateDirectory(folder);
            string Quote(string value) => "\"" + value.Replace("\\", "/") + "\"";
            var arguments = new List<string> { "-nologo", "-target:library", "-nostdlib+", "-langversion:latest",
                "-unsafe+", "-out:" + Quote(Path.Combine(folder, assembly.name + ".dll")) };
            arguments.Add("-define:" + string.Join(";", defines ?? assembly.defines));
            arguments.AddRange(references.Distinct().Select(p => "-reference:" + Quote(p)));
            arguments.AddRange(assembly.sourceFiles.Select(p => Quote(Resolve(p, packages))));
            if (extraSource != null) arguments.Add(Quote(extraSource));
            var rsp = Path.Combine(folder, "compile.rsp");
            File.WriteAllLines(rsp, arguments);
            var data = EditorApplication.applicationContentsPath;
            var runtime = Path.Combine(data, "NetCoreRuntime", Application.platform == RuntimePlatform.WindowsEditor ? "dotnet.exe" : "dotnet");
            var compiler = Path.Combine(data, "DotNetSdkRoslyn/csc.dll");
            if (!File.Exists(compiler))
            {
                // Unity 6.6 ships a full SDK instead of the older DotNetSdkRoslyn layout.
                var sdk = Path.Combine(data, "DotNetSdk");
                var versions = Path.Combine(sdk, "sdk");
                var candidates = Directory.Exists(versions) ? Directory.GetDirectories(versions)
                    .Select(p => Path.Combine(p, "Roslyn/bincore/csc.dll")).Where(File.Exists).ToArray() : Array.Empty<string>();
                Assert.That(candidates.Length, Is.EqualTo(1), "Expected one bundled Unity SDK compiler");
                compiler = candidates[0];
                runtime = Path.Combine(sdk, Application.platform == RuntimePlatform.WindowsEditor ? "dotnet.exe" : "dotnet");
            }
            Assert.That(File.Exists(runtime) && File.Exists(compiler), Is.True, "Unity bundled Roslyn/runtime are missing");
            using var process = Process.Start(new ProcessStartInfo(runtime, Quote(compiler) + " @" + Quote(rsp)) {
                WorkingDirectory = project, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            })!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(120000))
            {
                process.Kill();
                Assert.Fail("Isolated compiler timed out: " + assembly.name);
            }
            var output = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
            File.WriteAllText(Path.Combine(folder, "compile.log"), output);
            return (process.ExitCode, output, Path.Combine(folder, assembly.name + ".dll"));
        }
    }
}
