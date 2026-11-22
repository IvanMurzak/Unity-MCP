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

        static string Asmdef(params string[] constraints) => Paired(null, constraints);

        // An assembly definition as `commands/extensions.py settle` writes it: the gate defines plus, with
        // a plugin expression, the version define that holds only for that plugin release.
        static string Paired(string? pluginExpression, params string[] constraints)
        {
            if (pluginExpression != null)
                constraints = constraints.Append(Match).ToArray();
            return "{\n  \"name\": \"X\",\n  \"defineConstraints\": [\n    "
                + string.Join(",\n    ", constraints.Select(constraint => "\"" + constraint + "\"")) + "\n  ],\n  \"versionDefines\": ["
                + (pluginExpression == null ? "" : $"{{\"name\": \"{Plugin}\", \"expression\": \"{pluginExpression}\", \"define\": \"{Match}\"}}")
                + "]\n}";
        }

        const string Match = "UNITY_MCP_CORE_MATCH";

        // The shape package.openupm.com returns: every version with its own dependencies.
        static string Registry(params (string Version, string? PluginFloor)[] versions)
            => "{\"name\":\"x\",\"dist-tags\":{\"latest\":\"9.9.9\"},\"versions\":{"
                + string.Join(",", versions.Select(v => $"\"{v.Version}\":{{\"version\":\"{v.Version}\",\"dependencies\":{{"
                    + (v.PluginFloor == null ? "\"com.unity.modules.animation\":\"1.0.0\"" : $"\"{Plugin}\":\"{v.PluginFloor}\"") + "}}"))
                + "}}";

        static ExtensionFit Fit(string plugin, params string[] asmdefs) => ExtensionCompatibility.FitOf(asmdefs, Prefix, Current, Plugin, plugin);

        [Theory]
        [InlineData("1.13.0")]
        [InlineData("1.13.0+build.7")] // build metadata changes nothing
        public void Extension_built_for_this_plugin_release_works(string plugin)
        {
            Assert.Equal(ExtensionFit.Works, Fit(plugin,
                Paired("[1.13.0]", NuGetConfig.ReadyDefine, Current, "!UNITY_MCP_EXCLUDE_FROM_COMPILATION"),
                Paired("[1.13.0]", NuGetConfig.ReadyDefine, Current)));
        }

        [Fact]
        public void Extension_gated_on_another_generation_in_any_asmdef_is_skipped()
        {
            Assert.Equal(ExtensionFit.Skipped, Fit("1.13.0",
                Paired("[1.13.0]", NuGetConfig.ReadyDefine, Current),
                Paired("[1.13.0]", NuGetConfig.ReadyDefine, Prefix + "38")));
            Assert.Equal(ExtensionFit.Skipped, Fit("1.13.0", Asmdef(NuGetConfig.ReadyDefine, Prefix + "38")));
        }

        [Theory]
        [InlineData("1.13.1")] // a later plugin release, even with the same generation
        [InlineData("1.12.0")]
        [InlineData("2.0.0")]
        [InlineData("1.13.0-rc.1")] // Unity orders a pre-release below its release: `[1.13.0]` does not hold
        public void Extension_built_for_another_plugin_release_is_skipped(string plugin)
        {
            Assert.Equal(ExtensionFit.Skipped, Fit(plugin,
                Paired("[1.13.0]", NuGetConfig.ReadyDefine, Current),
                Asmdef(NuGetConfig.ReadyDefine, Current)));
        }

        [Fact]
        public void Extension_on_the_current_generation_without_a_plugin_version_is_unprotected()
        {
            // What was published before the pairing: it compiles now and breaks the next plugin update.
            Assert.Equal(ExtensionFit.Unprotected, Fit("1.13.0", Asmdef(NuGetConfig.ReadyDefine, Current)));
            Assert.Equal(ExtensionFit.Unprotected, Fit("1.13.0",
                Paired("[1.13.0]", NuGetConfig.ReadyDefine, Current),
                Asmdef(NuGetConfig.ReadyDefine, Current)));
            // A version define the assembly does not require constrains nothing.
            var unused = Paired("[1.0.0]", NuGetConfig.ReadyDefine, Current).Replace("\"" + Match + "\"\n  ]", "\"OTHER\"\n  ]");
            Assert.Equal(ExtensionFit.Unprotected, Fit("1.13.0", unused));
        }

        [Fact]
        public void Extension_without_the_generation_gate_is_not_judged()
        {
            // An extension older than the gate (or a third-party one) compiles against any plugin.
            Assert.Equal(ExtensionFit.Works, Fit("1.13.0", Asmdef(NuGetConfig.ReadyDefine), "{}", "not json", "[]"));
            Assert.Equal(ExtensionFit.Works, Fit("1.13.0"));
            // Nor is an optional assembly a package keeps for other plugin versions, outside the gate.
            Assert.Equal(ExtensionFit.Works, Fit("1.13.0", Paired("[1.0.0,1.5.0)", NuGetConfig.ReadyDefine), Paired("[1.13.0]", NuGetConfig.ReadyDefine, Current)));
        }

        [Theory]
        [InlineData("1.2.3", "1.2.3", true)]
        [InlineData("1.2.3", "1.3.0", true)]
        [InlineData("1.2.3", "1.2.2", false)]
        [InlineData("[1.2.3]", "1.2.3", true)]
        [InlineData("[1.2.3]", "1.2.4", false)]
        [InlineData("[1.2.3]", "1.2.2", false)]
        [InlineData("[1.2.0,1.3.0)", "1.2.9", true)]
        [InlineData("[1.2.0,1.3.0)", "1.3.0", false)]
        [InlineData("(1.2.0,1.3.0]", "1.2.0", false)]
        [InlineData("(1.2.0,1.3.0]", "1.3.0", true)]
        [InlineData("[1.2,2)", "1.9.9", true)]
        [InlineData("[1.2,2)", "2.0.0", false)]
        [InlineData("[1.2.3]", "1.2.3-rc.1", false)] // a pre-release sorts below its release
        [InlineData("1.2.3", "1.2.3-rc.1", false)]
        [InlineData("[1.2.0,1.3.0)", "1.3.0-rc.1", true)]
        [InlineData("[1.2.3]", "1.2.3+build.7", true)]
        [InlineData(" [1.2, 2) ", "1.9.9", false)] // Unity leaves the define of an expression it cannot read unset
        [InlineData("(1.2.3)", "1.2.3", false)]
        [InlineData("nonsense", "1.0.0", false)]
        [InlineData("[1.2.3", "1.0.0", false)]
        [InlineData("", "1.0.0", false)]
        [InlineData("[1.2.3]", "unknown", true)] // a version that cannot be read is not judged
        public void A_version_define_expression_is_read_as_Unity_reads_it(string expression, string version, bool expected)
        {
            Assert.Equal(expected, ExtensionCompatibility.Satisfies(expression, version));
        }

        [Fact]
        public void The_modules_of_this_repository_are_built_for_this_plugin()
        {
            // Each real module package, read the way the plugin reads an installed one.
            var manifest = RepoFile.Find("extensions/manifest.json");
            var core = RepoFile.Find("Unity-MCP-Plugin/Packages/" + Plugin + "/package.json");
            Assert.NotNull(manifest);
            Assert.NotNull(core);
            string pluginVersion;
            using (var package = System.Text.Json.JsonDocument.Parse(File.ReadAllText(core!)))
                pluginVersion = package.RootElement.GetProperty("version").GetString()!;
            var packages = Directory.GetDirectories(Path.GetDirectoryName(manifest)!)
                .Select(module => Path.Combine(module, "Packages"))
                .Where(Directory.Exists)
                .SelectMany(Directory.GetDirectories)
                .ToArray();

            using (var inventory = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifest!)))
                Assert.Equal(inventory.RootElement.GetProperty("extensions").GetArrayLength(), packages.Length);
            foreach (var package in packages)
            {
                var asmdefs = ExtensionCompatibility.ShippedAsmdefs(package).ToArray();
                Assert.True(asmdefs.Length >= 2, $"{package}: expected its Editor and Runtime asmdef, found {asmdefs.Length}");
                Assert.Equal(ExtensionFit.Works, Fit(pluginVersion, asmdefs.Select(File.ReadAllText).ToArray()));
                // And with no other plugin release: this is what keeps a plugin-only update from breaking the compile.
                Assert.Equal(ExtensionFit.Skipped, Fit("0.0.1", asmdefs.Select(File.ReadAllText).ToArray()));
            }
        }

        [Fact]
        public void Test_and_hidden_folders_of_a_package_are_not_read()
        {
            var package = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
            try
            {
                foreach (var relative in new[] { "Editor/Scripts/A.Editor.asmdef", "Runtime/A.Runtime.asmdef", "Tests/Editor/A.Tests.asmdef", "Samples~/Demo/A.Demo.asmdef", ".git/modules/x/B.asmdef" })
                {
                    var path = Path.Combine(package, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, Asmdef(Prefix + "38"));
                }

                var read = ExtensionCompatibility.ShippedAsmdefs(package).Select(path => Path.GetFileName(path)).OrderBy(name => name).ToArray();

                Assert.Equal(new[] { "A.Editor.asmdef", "A.Runtime.asmdef" }, read);
                // Bounded: a package folder that holds a whole project is not walked to the bottom.
                Assert.Equal(new[] { "A.Runtime.asmdef" }, ExtensionCompatibility.ShippedAsmdefs(package, depth: 1).Select(path => Path.GetFileName(path)));
            }
            finally
            {
                Directory.Delete(package, recursive: true);
            }
        }

        static readonly string Published = Registry(("2.1.1", "1.6.2"), ("2.1.3", "1.13.0"), ("2.1.2", "1.13.0"), ("2.1.4", "1.14.0"));

        [Theory]
        [InlineData("1.13.0", "2.1.3")] // the highest of the versions published for that plugin release
        [InlineData("1.14.0", "2.1.4")]
        [InlineData("1.6.2", "2.1.1")]
        [InlineData("1.13.5", null)] // a plugin release has its own extension versions: none is published yet
        [InlineData("1.12.0", null)]
        [InlineData("1.14.0-rc.1", null)] // no extension is built for a pre-release plugin
        [InlineData("1.13.0+build.7", "2.1.3")]
        [InlineData("unknown", null)]
        public void The_version_for_a_plugin_is_the_highest_one_published_for_that_release(string plugin, string? expected)
        {
            Assert.Equal(expected, ExtensionCompatibility.VersionFor(Published, Plugin, plugin));
        }

        [Fact]
        public void The_cases_shared_with_the_cli_give_the_same_versions()
        {
            // The CLI implements the same rule (cli/src/utils/manifest.ts) and reads the same file
            // (cli/tests/extension-version-rule-parity.test.ts): the two cannot drift apart.
            var path = RepoFile.Find("tests/extension-version-rule.json");
            Assert.NotNull(path);
            using var cases = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path!));
            Assert.Equal(Plugin, cases.RootElement.GetProperty("plugin").GetString());
            var registry = cases.RootElement.GetProperty("registry").GetRawText();
            var expected = cases.RootElement.GetProperty("expected").EnumerateObject().ToArray();

            Assert.True(expected.Length > 5);
            Assert.All(expected, item => Assert.Equal(item.Value.GetString(), ExtensionCompatibility.VersionFor(registry, Plugin, item.Name)));
        }

        [Fact]
        public void Versions_that_cannot_be_ordered_or_do_not_depend_on_the_plugin_are_ignored()
        {
            var registry = Registry(("2.1.1", "1.13.0"), ("3.0.0-preview.1", "1.13.0"), ("2.5.0", null), ("2.4.0", "^1.13.0"),
                ("2.6.99999999999", "1.13.0"), ("2.7.0", "1.13.99999999999"));

            Assert.Equal("2.1.1", ExtensionCompatibility.VersionFor(registry, Plugin, "1.13.0"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{\"versions\":[]}")]
        [InlineData("{\"dist-tags\":{\"latest\":\"2.1.2\"}}")]
        public void An_unusable_registry_document_names_no_version(string registry)
        {
            Assert.Null(ExtensionCompatibility.VersionFor(registry, Plugin, "1.13.0"));
        }

        [Theory]
        [InlineData("2.1.1", "1.13.0", false, "2.1.3")] // the version built for the installed plugin, not the latest one
        [InlineData("2.1.2", "1.13.0", false, "2.1.3")]
        [InlineData("2.1.3", "1.13.0", false, null)]    // already the newest one for this plugin
        [InlineData("2.1.4", "1.13.0", false, null)]    // a working extension is never downgraded
        [InlineData("2.1.4", "1.13.0", true, "2.1.3")]  // one that Unity skips follows the plugin, down too
        [InlineData("2.1.3", "1.13.0", true, null)]
        [InlineData("2.1.1", "1.12.0", true, null)]     // nothing is published for plugin 1.12.0
        [InlineData("2.2.0-preview.1", "1.13.0", false, null)]
        [InlineData("2.2.0-preview.1", "1.13.0", true, "2.1.3")]
        public void An_installed_extension_moves_to_the_version_for_the_installed_plugin(string installed, string plugin, bool anyDirection, string? expected)
        {
            Assert.Equal(expected, ExtensionCompatibility.UpdateTargetFor(installed, Published, Plugin, plugin, anyDirection));
        }

        private static readonly DateTime Now = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData("2026-11-22T11:30:00.000Z", true)]       // half an hour old
        [InlineData("2026-11-21T12:00:01.000Z", true)]       // a second inside the day
        [InlineData("2026-11-21T12:00:00.000Z", false)]      // a day old
        [InlineData("2026-11-19T12:31:58.293Z", false)]
        [InlineData("2026-11-22T14:30:00.000+03:00", true)]  // an offset is read, not dropped
        [InlineData("2026-11-22T12:05:00.000Z", true)]       // a clock a little behind the registry
        [InlineData("yesterday", false)]
        public void A_release_is_young_for_a_day_after_its_publication(string published, bool expected)
        {
            var registry = "{\"time\":{\"created\":\"2025-01-01T00:00:00.000Z\",\"1.14.0\":\"" + published + "\"}}";

            Assert.Equal(expected, ExtensionCompatibility.PublishedWithin(registry, "1.14.0", Now, TimeSpan.FromHours(24)));
        }

        [Theory]
        [InlineData("{\"time\":{\"1.13.0\":\"2026-11-22T11:30:00.000Z\"}}")]  // another version
        [InlineData("{\"time\":{\"1.14.0\":1763811000}}")]
        [InlineData("{\"time\":\"2026-11-22T11:30:00.000Z\"}")]
        [InlineData("{\"versions\":{}}")]
        [InlineData("[]")]
        [InlineData("not json")]
        public void A_release_whose_publication_time_cannot_be_read_is_not_young(string registry)
        {
            Assert.False(ExtensionCompatibility.PublishedWithin(registry, "1.14.0", Now, TimeSpan.FromHours(24)));
        }
    }
}
