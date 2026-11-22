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
using System.IO;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    /// <summary>
    /// The rule behind the startup extension update: an extension gated on another dependency
    /// generation is dead in this plugin, and the version to move to is the highest published one
    /// that does not need a newer plugin.
    /// </summary>
    public class ExtensionCompatibilityTests
    {
        const string Plugin = "com.ivanmurzak.unity.mcp";
        const string Prefix = NuGetConfig.DependencyGenerationDefinePrefix;
        const string Current = NuGetConfig.DependencyGenerationDefine;

        static string Asmdef(params string[] constraints)
            => "{\n  \"name\": \"X\",\n  \"defineConstraints\": [\n    "
                + string.Join(",\n    ", constraints.Select(constraint => "\"" + constraint + "\"")) + "\n  ]\n}";

        // The shape package.openupm.com returns: every version with its own dependencies.
        static string Registry(params (string Version, string? PluginFloor)[] versions)
            => "{\"name\":\"x\",\"dist-tags\":{\"latest\":\"9.9.9\"},\"versions\":{"
                + string.Join(",", versions.Select(v => $"\"{v.Version}\":{{\"version\":\"{v.Version}\",\"dependencies\":{{"
                    + (v.PluginFloor == null ? "\"com.unity.modules.animation\":\"1.0.0\"" : $"\"{Plugin}\":\"{v.PluginFloor}\"") + "}}"))
                + "}}";

        [Fact]
        public void Extension_gated_on_the_current_generation_is_compatible()
        {
            var asmdefs = new[] { Asmdef(NuGetConfig.ReadyDefine, Current, "!UNITY_MCP_EXCLUDE_FROM_COMPILATION"), Asmdef(NuGetConfig.ReadyDefine, Current) };

            Assert.False(ExtensionCompatibility.IsBuiltForAnotherGeneration(asmdefs, Prefix, Current));
        }

        [Fact]
        public void Extension_gated_on_another_generation_in_any_asmdef_is_stale()
        {
            var asmdefs = new[] { Asmdef(NuGetConfig.ReadyDefine, Current), Asmdef(NuGetConfig.ReadyDefine, Prefix + "38") };

            Assert.True(ExtensionCompatibility.IsBuiltForAnotherGeneration(asmdefs, Prefix, Current));
            Assert.Equal(new[] { Prefix + "38" }, ExtensionCompatibility.GenerationDefines(asmdefs[1], Prefix));
        }

        [Fact]
        public void Extension_without_the_generation_gate_is_not_judged()
        {
            // An extension older than the gate (or a third-party one) compiles against any plugin.
            Assert.False(ExtensionCompatibility.IsBuiltForAnotherGeneration(new[] { Asmdef(NuGetConfig.ReadyDefine), "{}" }, Prefix, Current));
            Assert.False(ExtensionCompatibility.IsBuiltForAnotherGeneration(new string[0], Prefix, Current));
        }

        [Fact]
        public void The_modules_of_this_repository_are_built_for_this_plugin()
        {
            // Each real module package, read the way the plugin reads an installed one.
            var manifest = RepoFile.Find("extensions/manifest.json");
            Assert.NotNull(manifest);
            var packages = Directory.GetDirectories(Path.GetDirectoryName(manifest)!)
                .Select(module => Path.Combine(module, "Packages"))
                .Where(Directory.Exists)
                .SelectMany(Directory.GetDirectories)
                .ToArray();

            Assert.Equal(10, packages.Length);
            foreach (var package in packages)
            {
                var asmdefs = ExtensionCompatibility.ShippedAsmdefs(package).ToArray();
                Assert.True(asmdefs.Length >= 2, $"{package}: expected its Editor and Runtime asmdef, found {asmdefs.Length}");
                Assert.All(asmdefs, path => Assert.Equal(new[] { Current }, ExtensionCompatibility.GenerationDefines(File.ReadAllText(path), Prefix)));
                Assert.False(ExtensionCompatibility.IsBuiltForAnotherGeneration(asmdefs.Select(File.ReadAllText), Prefix, Current));
            }
        }

        [Fact]
        public void Test_and_hidden_folders_of_a_package_are_not_read()
        {
            var package = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
            try
            {
                foreach (var relative in new[] { "Editor/Scripts/A.Editor.asmdef", "Runtime/A.Runtime.asmdef", "Tests/Editor/A.Tests.asmdef", "Samples~/Demo/A.Demo.asmdef" })
                {
                    var path = Path.Combine(package, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, Asmdef(Prefix + "38"));
                }

                var read = ExtensionCompatibility.ShippedAsmdefs(package).Select(path => Path.GetFileName(path)).OrderBy(name => name).ToArray();

                Assert.Equal(new[] { "A.Editor.asmdef", "A.Runtime.asmdef" }, read);
            }
            finally
            {
                Directory.Delete(package, recursive: true);
            }
        }

        [Theory]
        [InlineData("1.13.0", "2.1.2")] // the release that moved the generation ships its own extensions
        [InlineData("1.13.5", "2.1.2")] // a later plugin release without a newer extension keeps them
        [InlineData("1.14.0", "2.1.3")]
        [InlineData("1.12.0", "2.1.1")] // never a version that would pull a newer plugin in
        [InlineData("1.6.1", null)]
        public void The_version_for_a_plugin_is_the_highest_one_that_does_not_need_a_newer_plugin(string plugin, string? expected)
        {
            var registry = Registry(("2.1.1", "1.6.2"), ("2.1.3", "1.14.0"), ("2.1.2", "1.13.0"));

            Assert.Equal(expected, ExtensionCompatibility.HighestVersionFor(registry, Plugin, plugin));
        }

        [Fact]
        public void Versions_that_cannot_be_ordered_or_do_not_depend_on_the_plugin_are_ignored()
        {
            var registry = Registry(("2.1.1", "1.6.2"), ("3.0.0-preview.1", "1.0.0"), ("2.5.0", null), ("2.4.0", "^1.0.0"));

            Assert.Equal("2.1.1", ExtensionCompatibility.HighestVersionFor(registry, Plugin, "1.13.0"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{\"versions\":[]}")]
        [InlineData("{\"dist-tags\":{\"latest\":\"2.1.2\"}}")]
        public void An_unusable_registry_document_names_no_version(string registry)
        {
            Assert.Null(ExtensionCompatibility.HighestVersionFor(registry, Plugin, "1.13.0"));
        }

        [Fact]
        public void A_stale_extension_moves_to_the_version_built_for_the_installed_plugin()
        {
            var registry = Registry(("2.1.1", "1.6.2"), ("2.1.2", "1.13.0"), ("2.1.3", "1.14.0"));

            Assert.Equal((ExtensionCompatibility.Remedy.Update, "2.1.2"), ExtensionCompatibility.Decide("2.1.1", registry, Plugin, "1.13.0"));
        }

        [Theory]
        [InlineData("2.1.2", "1.13.0")] // already the newest one for this plugin: installing it again changes nothing
        [InlineData("2.1.1", "1.12.0")] // nothing was published for the generation of plugin 1.12.0
        [InlineData("2.1.3", "1.13.0")] // never a downgrade
        public void A_stale_extension_without_a_newer_version_for_this_plugin_is_left_alone(string installed, string plugin)
        {
            var registry = Registry(("2.1.1", "1.6.2"), ("2.1.2", "1.13.0"), ("2.1.3", "1.14.0"));

            Assert.Equal((ExtensionCompatibility.Remedy.NoVersion, (string?)null), ExtensionCompatibility.Decide(installed, registry, Plugin, plugin));
        }

        [Fact]
        public void A_registry_that_could_not_be_read_is_not_mistaken_for_no_version()
        {
            Assert.Equal((ExtensionCompatibility.Remedy.Unknown, (string?)null), ExtensionCompatibility.Decide("2.1.1", null, Plugin, "1.13.0"));
        }
    }
}
