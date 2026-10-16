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
                if (package != null) owners[assembly.name] = package.name;
            }
            int compiled = 0;
            foreach (var extension in extensions)
            {
                var allowed = Closure(extension.name, packages);
                // Test Framework is provided by Unity's test consumer, not by a shipped runtime package.
                allowed.UnionWith(Closure("com.unity.test-framework", packages));
                var own = assemblies.Where(a => owners.TryGetValue(a.name, out var owner) && owner == extension.name).ToArray();
                Assert.That(own.Any(a => a.name.EndsWith(".Editor", StringComparison.Ordinal)), Is.True,
                    extension.name + " editor assembly is missing (check compilation defines)");
                Assert.That(own.Any(a => a.name.EndsWith(".Editor.Tests", StringComparison.Ordinal)), Is.True,
                    extension.name + " original editor test assembly was not discovered");
                foreach (var assembly in own.Where(a => a.sourceFiles.Length > 0))
                {
                    var references = References(assembly, allowed, owners, packages);
                    var result = Compile(assembly, references, packages, "isolated");
                    Assert.That(result.code, Is.Zero, extension.name + "/" + assembly.name + "\n" + result.output);
                    compiled++;
                    TestContext.WriteLine("Isolated compilation passed: " + assembly.name);
                }
            }
            Assert.That(compiled, Is.GreaterThanOrEqualTo(20), "No meaningful extension compilation happened");

            // Both sides of this control compile the SAME planted class. Only the sibling DLL differs.
            var animation = assemblies.Single(a => a.name == "com.IvanMurzak.Unity.MCP.Animation.Editor");
            var cinemachine = assemblies.Single(a => a.name == "com.IvanMurzak.Unity.MCP.Cinemachine.Editor");
            var allowedAnimation = Closure(Prefix + "animation", packages);
            var isolated = References(animation, allowedAnimation, owners, packages);
            var canary = Path.Combine(project, "Library/ExtensionIsolation/SiblingCanary.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(canary)!);
            File.WriteAllText(canary, "internal static class SiblingCanary { internal static System.Type Missing = typeof(com.IvanMurzak.Unity.MCP.Editor.Rql.Cinemachine.CinemachineModule); }");
            var negative = Compile(animation, isolated, packages, "negative", canary);
            Assert.That(negative.code, Is.Not.Zero, "Undeclared sibling unexpectedly compiled");
            Assert.That(negative.output, Does.Contain("CS0234").Or.Contain("CS0246"), negative.output);
            var positive = Compile(animation, isolated.Append(Resolve(cinemachine.outputPath, packages)), packages, "positive", canary);
            Assert.That(positive.code, Is.Zero, "Canary control failed for a reason other than sibling isolation\n" + positive.output);
            TestContext.WriteLine($"Compiled {compiled} native extension assemblies; sibling canary failed without and passed with the sibling DLL.");
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
            Dictionary<string, string> owners, Dictionary<string, PackageInfo> packages)
        {
            foreach (var reference in assembly.allReferences)
            {
                var full = Resolve(reference, packages);
                var name = Path.GetFileNameWithoutExtension(full);
                if (owners.TryGetValue(name, out var owner) && !allowed.Contains(owner)) continue;
                // Also exclude precompiled dependencies residing inside undeclared UPM packages.
                var packageOwner = packages.Values.FirstOrDefault(p => full.StartsWith(
                    p.resolvedPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
                if (packageOwner != null && !allowed.Contains(packageOwner.name)) continue;
                yield return full;
            }
        }

        (int code, string output) Compile(Assembly assembly, IEnumerable<string> references,
            Dictionary<string, PackageInfo> packages, string variant, string? extraSource = null)
        {
            var folder = Path.Combine(project, "Library/ExtensionIsolation", assembly.name, variant);
            Directory.CreateDirectory(folder);
            string Quote(string value) => "\"" + value.Replace("\\", "/") + "\"";
            var arguments = new List<string> { "-nologo", "-target:library", "-nostdlib+", "-langversion:latest",
                "-unsafe+", "-out:" + Quote(Path.Combine(folder, assembly.name + ".dll")) };
            arguments.Add("-define:" + string.Join(";", assembly.defines));
            arguments.AddRange(references.Distinct().Select(p => "-reference:" + Quote(p)));
            arguments.AddRange(assembly.sourceFiles.Select(p => Quote(Resolve(p, packages))));
            if (extraSource != null) arguments.Add(Quote(extraSource));
            var rsp = Path.Combine(folder, "compile.rsp");
            File.WriteAllLines(rsp, arguments);
            var data = EditorApplication.applicationContentsPath;
            var runtime = Path.Combine(data, "NetCoreRuntime", Application.platform == RuntimePlatform.WindowsEditor ? "dotnet.exe" : "dotnet");
            var compiler = Path.Combine(data, "DotNetSdkRoslyn/csc.dll");
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
            return (process.ExitCode, output);
        }
    }
}
