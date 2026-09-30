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
using System.Linq;
using System.Reflection;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Editor.API;
using Xunit;
using McpVersion = com.IvanMurzak.McpPlugin.Common.Version;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    /// <summary>
    /// Tool-attribute plumbing: what the Unity plugin registers with McpPlugin, and under what name.
    ///
    /// <para>WHY THIS MATTERS. <c>McpPluginBuilder</c> partitions tools by <see cref="McpToolType"/>
    /// into two DISJOINT registries — Standard tools reach <see cref="IToolManager"/> (the MCP
    /// <c>tools/list</c> an AI agent sees), System tools reach <see cref="ISystemToolManager"/> (the
    /// <c>/api/system-tools/</c> HTTP surface a CLI health check probes). The attribute is therefore not
    /// decoration: dropping <c>[AiToolType]</c> removes the class from discovery entirely, and flipping
    /// <c>ToolType</c> moves the tool to the other REST surface, where the caller gets "tool not
    /// found". Neither shows up as a compile error.</para>
    ///
    /// <para>SCOPE. Only <c>Tool_Ping</c> is compiled into this assembly, because it is the only
    /// <c>[AiToolType]</c> family in the plugin whose every partial file is engine-free (README.md
    /// lists the rest and what blocks them). That is a deliberate limit rather than an oversight: a
    /// partial class assembled from a subset of its files presents a tool surface that does not exist in
    /// production, so a reflection test over it would be measuring the wrong object and reporting green.
    /// The remaining families stay covered by the game-ci EditMode suites.</para>
    /// </summary>
    public class ToolAttributeTests
    {
        static readonly Assembly TestAssembly = typeof(ToolAttributeTests).Assembly;

        static MethodInfo PingMethod =>
            typeof(Tool_Ping).GetMethod(nameof(Tool_Ping.Ping), BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Tool_Ping.Ping not found — was the tool method renamed?");

        static AiToolAttribute PingAttribute =>
            PingMethod.GetCustomAttribute<AiToolAttribute>()
            ?? throw new InvalidOperationException("Tool_Ping.Ping is missing its [AiTool] attribute.");

        // ── The declaration side ───────────────────────────────────────────────────────────────────

        [Fact]
        public void ToolClass_CarriesAiToolType_SoTheAssemblyScanCanFindIt()
        {
            // WithToolsFromAssembly enumerates types and keeps the ones marked [AiToolType]. Without the
            // marker the class is skipped in complete silence — no warning, no error, just a tool that
            // has vanished from the protocol.
            Assert.NotNull(typeof(Tool_Ping).GetCustomAttribute<AiToolTypeAttribute>());
        }

        [Fact]
        public void ToolIdConstant_IsTheNameTheAttributeActuallyRegisters()
        {
            // The constant is what other code (CLI probes, skill files, docs) points at; the attribute
            // is what the protocol serves. They are two separate literals in the source and nothing but
            // this assert keeps them equal.
            Assert.Equal("ping", Tool_Ping.PingToolId);
            Assert.Equal(Tool_Ping.PingToolId, PingAttribute.Name);
        }

        [Fact]
        public void PingIsRegisteredAsASystemTool_NotAStandardOne()
        {
            // ping is the liveness probe the CLI calls over /api/system-tools/. Flipping this to
            // Standard moves it onto the agent-facing tools/list and 404s every health check.
            Assert.Equal(McpToolType.System, PingAttribute.ToolType);
        }

        [Fact]
        public void PingCarriesTheReadOnlyAndIdempotentHints_AndStaysEnabled()
        {
            // A pure echo: an agent may call it freely and repeatedly. These hints are what tell a
            // client that, and `Enabled` is what keeps it in the listing at all.
            Assert.True(PingAttribute.ReadOnlyHint);
            Assert.True(PingAttribute.IdempotentHint);
            Assert.True(PingAttribute.Enabled);
            Assert.Equal("Ping", PingAttribute.Title);
        }

        [Fact]
        public void PingCarriesSkillDescriptionAndBody_SoTheGeneratedSkillMdIsUseful()
        {
            // unity-skill-generate writes a SKILL.md whose YAML `description:` comes from
            // [AiSkillDescription] and whose markdown body comes from [AiSkillBody]. Missing either
            // yields an empty-ish skill file rather than a build failure.
            var description = PingMethod.GetCustomAttribute<AiSkillDescriptionAttribute>();
            Assert.True(description != null, "Tool_Ping.Ping is missing [AiSkillDescription].");
            Assert.False(string.IsNullOrWhiteSpace(description!.Description));

            var body = PingMethod.GetCustomAttribute<AiSkillBodyAttribute>();
            Assert.True(body != null, "Tool_Ping.Ping is missing [AiSkillBody].");
            Assert.False(string.IsNullOrWhiteSpace(body!.Body));
        }

        [Fact]
        public void EveryAiToolOnACompiledInFamily_DeclaresANameAndAKnownToolType()
        {
            // A sweep rather than a single-tool assert, so a future engine-free tool family added to
            // EngineFree.csproj is covered the moment it is compiled in.
            var tools = TestAssembly.GetTypes()
                .Where(t => t.GetCustomAttribute<AiToolTypeAttribute>() != null)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(m => (Type: t, Method: m, Attribute: m.GetCustomAttribute<AiToolAttribute>())))
                .Where(x => x.Attribute != null)
                .ToArray();

            Assert.NotEmpty(tools);
            foreach (var (type, method, attribute) in tools)
            {
                Assert.False(string.IsNullOrWhiteSpace(attribute!.Name),
                    $"{type.Name}.{method.Name} registers an unnamed tool.");
                Assert.True(Enum.IsDefined(typeof(McpToolType), attribute.ToolType),
                    $"{type.Name}.{method.Name} declares an unknown ToolType.");
            }

            // Tool ids are the protocol's primary key — a duplicate silently shadows one of them.
            var duplicates = tools.GroupBy(x => x.Attribute!.Name, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToArray();
            Assert.Empty(duplicates);
        }

        // ── The registration side: drive the REAL McpPluginBuilder ─────────────────────────────────

        [Fact]
        public void AssemblyScan_DiscoversPingAndLandsItOnTheSystemRegistry()
        {
            // End-to-end through the shipped framework: scan this assembly exactly the way the plugin
            // scans its own, then read the two registries the build produced. This is what turns the
            // attribute asserts above from "the source says X" into "the framework did X".
            using var plugin = BuildFromAssemblyScan();

            Assert.True(plugin.SystemTools.HasTool(Tool_Ping.PingToolId),
                "ping was not registered on the SYSTEM tool manager — the /api/system-tools/ probe would 404.");
            Assert.False(plugin.StandardTools.HasTool(Tool_Ping.PingToolId),
                "ping leaked onto the STANDARD tool manager — the two registries must stay disjoint.");

            Assert.Contains(Tool_Ping.PingToolId,
                plugin.SystemTools.GetAllTools().Select(t => t.Name));
        }

        [Fact]
        public void AssemblyScan_RegistersNothing_WhenTheHostingAssemblyIsIgnored()
        {
            // The negative half of the test above. Without it, "HasTool(ping) is true" would also be
            // satisfied by a framework that registers everything it can reach regardless of the scan
            // set — so the discovery claim would not actually be under test.
            using var plugin = BuildFromAssemblyScan(b => b.IgnoreAssembly(TestAssembly));

            Assert.False(plugin.SystemTools.HasTool(Tool_Ping.PingToolId));
            Assert.Empty(plugin.SystemTools.GetAllTools());
        }

        [Fact]
        public void ExplicitRegistration_ByType_ProducesTheSameSystemToolEntry()
        {
            // The runtime (game-build) path opts tools in by TYPE rather than by scanning. Both routes
            // must land the tool on the same registry under the same id.
            using var plugin = Build(b => b.WithTools(typeof(Tool_Ping)));

            Assert.True(plugin.SystemTools.HasTool(Tool_Ping.PingToolId));
            Assert.False(plugin.StandardTools.HasTool(Tool_Ping.PingToolId));
        }

        [Fact]
        public void BuilderWithNoToolsOptedIn_RegistersNothing()
        {
            // Zero tools by default — the invariant the runtime builder relies on, and the control that
            // proves the two positive tests above are observing an opt-in and not a default.
            using var plugin = Build(_ => { });

            Assert.Empty(plugin.SystemTools.GetAllTools());
            Assert.Empty(plugin.StandardTools.GetAllTools());
        }

        static BuiltPlugin BuildFromAssemblyScan(Action<IMcpPluginBuilder>? configure = null)
            => Build(b =>
            {
                b.WithToolsFromAssembly(TestAssembly);
                configure?.Invoke(b);
            });

        static BuiltPlugin Build(Action<IMcpPluginBuilder> configure)
        {
            var builder = new McpPluginBuilder(new McpVersion());
            configure(builder);
            return new BuiltPlugin(builder.Build(new Reflector()));
        }

        /// <summary>
        /// Thin owning wrapper so each test disposes the plugin it built — several may run in the same
        /// process and an undisposed instance keeps framework singletons alive across them. It also
        /// turns the two nullable manager properties into hard failures: a null registry would make
        /// every <c>Assert.Empty</c> below pass for the wrong reason if it were silently tolerated.
        /// </summary>
        sealed class BuiltPlugin : IDisposable
        {
            readonly IMcpPlugin _plugin;
            public BuiltPlugin(IMcpPlugin plugin) => _plugin = plugin;

            public ISystemToolManager SystemTools =>
                _plugin.McpManager.SystemToolManager
                ?? throw new InvalidOperationException("Build() produced no SystemToolManager.");

            public IToolManager StandardTools =>
                _plugin.McpManager.ToolManager
                ?? throw new InvalidOperationException("Build() produced no ToolManager.");

            public void Dispose() => (_plugin as IDisposable)?.Dispose();
        }
    }
}
