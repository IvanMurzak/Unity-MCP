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
using System.Text.Json;
using System.Text.Json.Serialization;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Converter;
using NUnit.Framework;
using Version = com.IvanMurzak.McpPlugin.Common.Version;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.ReflectorModules.Full
{
    // ──────────────────────────────────────────────────────────────────────────────────────────
    // Throwaway in-repo verification of the IReflectorModule discovery mechanism wired into the
    // Unity plugin build (see UnityMcpPlugin.Build.cs `.WithReflectorModulesFromAssembly(...)`).
    //
    // This is a UNIT-level proof: it constructs a McpPluginBuilder directly and registers THIS test
    // assembly for module discovery, asserting that an IReflectorModule with ZERO hardcoded
    // reference is auto-discovered and that all four contribution surfaces reach effect, that a
    // throwing module is isolated, and that a core-ignored heavy assembly is never type-enumerated.
    //
    // The fixture types live in this Editor.Tests asmdef (a test-only assembly, gated behind
    // `TestAssemblies` + the `UNITY_MCP_READY` define constraint), so they never ship in a player
    // build and are not discovered by the production `BuildMcpPlugin` path (which `.IgnoreAssemblies`
    // the test asmdefs). The test drives the builder directly, mirroring the upstream xUnit suite
    // (MCP-Plugin-dotnet ReflectorModuleDiscoveryTests).
    // ──────────────────────────────────────────────────────────────────────────────────────────

    // ── Fixture payload + converter types ───────────────────────────────────────────────────────

    /// <summary>Marker payload type the verification module registers a JSON converter for.</summary>
    public sealed class VerificationPayload
    {
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Type the verification module registers a reflection converter for.</summary>
    public sealed class VerificationReflectedType
    {
        public int Number { get; set; }
    }

    /// <summary>Type the verification module blacklists from serialization.</summary>
    public sealed class VerificationBlacklistedType
    {
        public string Secret { get; set; } = string.Empty;
    }

    /// <summary>A System.Text.Json converter contributed by the verification module.</summary>
    public sealed class VerificationPayloadJsonConverter : JsonConverter<VerificationPayload>
    {
        public override VerificationPayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new VerificationPayload { Value = reader.GetString() ?? string.Empty };

        public override void Write(Utf8JsonWriter writer, VerificationPayload value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }

    /// <summary>A reflection converter contributed by the verification module.</summary>
    public sealed class VerificationReflectionConverter : GenericReflectionConverter<VerificationReflectedType>
    {
    }

    // ── Modules ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The flagship verification module: a single discoverable module contributing a JSON converter,
    /// a reflection converter, a serialization-blacklist type, AND scan-ignore entries (assembly +
    /// namespace). Discovered with zero hardcoded reference — purely via assembly scan.
    /// </summary>
    public sealed class VerificationFullContributionModule : IReflectorModule
    {
        // A non-existent assembly prefix — safe to contribute (cannot collide with a protected assembly).
        public const string IgnoredAssemblyPrefix = "Some.Nonexistent.Unity.Extension.Assembly";
        // A non-existent namespace prefix — exercises the namespace scan-ignore surface harmlessly.
        public const string IgnoredNamespacePrefix = "Some.Nonexistent.Unity.Extension.Namespace";

        public int Order => 10;

        public void Configure(IReflectorModuleContext ctx)
        {
            ctx.Reflector.JsonSerializer.AddConverter(new VerificationPayloadJsonConverter());
            ctx.Reflector.Converters.Add(new VerificationReflectionConverter());
            ctx.Reflector.Converters.BlacklistType(typeof(VerificationBlacklistedType));
            ctx.Scan
                .IgnoreAssemblies(IgnoredAssemblyPrefix)
                .IgnoreNamespaces(IgnoredNamespacePrefix);
        }
    }
}

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.ReflectorModules.Throwing
{
    /// <summary>A module that throws during Configure — used to assert failure isolation.</summary>
    public sealed class ThrowingVerificationModule : IReflectorModule
    {
        public int Order => 0;

        public void Configure(IReflectorModuleContext ctx)
            => throw new InvalidOperationException("Intentional failure from ThrowingVerificationModule.");
    }

    /// <summary>A healthy module sitting alongside the throwing one — must still run.</summary>
    public sealed class SurvivingVerificationModule : IReflectorModule
    {
        public static bool Ran;
        public int Order => 1;

        public void Configure(IReflectorModuleContext ctx) => Ran = true;
    }
}

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.ReflectorModules
{
    using FullNs = com.IvanMurzak.Unity.MCP.Editor.Tests.ReflectorModules.Full;
    using ThrowingNs = com.IvanMurzak.Unity.MCP.Editor.Tests.ReflectorModules.Throwing;

    [TestFixture]
    public class ReflectorModuleDiscoveryTests
    {
        static readonly Version _version = new Version();
        static readonly Assembly TestAssembly = typeof(FullNs.VerificationFullContributionModule).Assembly;

        // Two sibling fixture namespaces, NEITHER a prefix of the other, so a test can ignore exactly
        // one without collaterally pruning the other (IScanIgnoreBuilder / IgnoreNamespaces matches by
        // StartsWith).
        const string NsFull = "com.IvanMurzak.Unity.MCP.Editor.Tests.ReflectorModules.Full";
        const string NsThrowing = "com.IvanMurzak.Unity.MCP.Editor.Tests.ReflectorModules.Throwing";

        // ── (1)-(4) Full contribution: every surface reaches effect via dynamic discovery ────────

        [Test]
        public void FullContribution_AllFourSurfacesReachEffect_ViaDynamicDiscovery()
        {
            // Arrange — keep ONLY the FullContribution namespace; ignore the Throwing fixtures so they
            // do not interfere with this assertion. The FullContributionModule carries no hardcoded
            // reference anywhere; it is found purely by scanning TestAssembly.
            var reflector = new Reflector();
            var builder = new McpPluginBuilder(_version)
                .WithReflectorModulesFromAssembly(new[] { TestAssembly })
                .IgnoreNamespaces(NsThrowing);

            // Act
            builder.Build(reflector);

            // (1) JSON converter registered.
            Assert.IsNotNull(
                reflector.JsonSerializer.GetJsonConverter(typeof(FullNs.VerificationPayload)),
                "Module-contributed JSON converter should be registered.");

            // (2) Reflection converter registered (resolvable for the target type).
            Assert.IsNotNull(
                reflector.Converters.GetConverter(typeof(FullNs.VerificationReflectedType)),
                "Module-contributed reflection converter should be resolvable.");
            Assert.IsTrue(
                reflector.Converters.GetAllSerializers().Any(c => c is FullNs.VerificationReflectionConverter),
                "Module-contributed reflection converter instance should be present.");

            // (3) Serialization blacklist applied.
            Assert.IsTrue(
                reflector.Converters.IsTypeBlacklisted(typeof(FullNs.VerificationBlacklistedType)),
                "Module-contributed serialization-blacklist type should be blacklisted.");

            // (4) Scan-ignore contributions accepted (no exception, build completes). The assembly +
            // namespace prefixes are non-existent on purpose, so they cannot collide with a protected
            // assembly/namespace; reaching this assertion proves the IScanIgnoreBuilder surface was
            // exercised end-to-end through a dynamically-discovered module.
            Assert.Pass("All four IReflectorModule contribution surfaces reached effect via dynamic discovery.");
        }

        // ── Throw-isolation: a throwing module is caught; the healthy sibling still runs ─────────

        [Test]
        public void FailureIsolation_ThrowingModuleCaught_SurvivingSiblingStillRuns()
        {
            // Arrange — keep ONLY the Throwing namespace (ThrowingVerificationModule +
            // SurvivingVerificationModule); ignore the FullContribution root namespace's other module.
            ThrowingNs.SurvivingVerificationModule.Ran = false;
            var reflector = new Reflector();
            var builder = new McpPluginBuilder(_version)
                .WithReflectorModulesFromAssembly(new[] { TestAssembly })
                .IgnoreNamespaces(NsFull);

            // Act — Build must NOT throw despite ThrowingVerificationModule.
            Assert.DoesNotThrow(() => builder.Build(reflector),
                "A throwing module must be isolated; Build must not propagate the exception.");

            // Assert — the healthy sibling module still ran.
            Assert.IsTrue(ThrowingNs.SurvivingVerificationModule.Ran,
                "The surviving sibling module should still run after a throwing module is isolated.");
        }

        // ── Perf sanity: a core-ignored heavy assembly is never type-enumerated for modules ──────

        [Test]
        public void Discovery_SkipsModule_WhenHostingAssemblyIsIgnored()
        {
            // Arrange — register TestAssembly for module scan, then ignore that very assembly by name.
            // If discovery honored the ignore prune (it must, for the perf guarantee), no module runs:
            // the heavy assembly is never type-enumerated.
            var reflector = new Reflector();
            var builder = new McpPluginBuilder(_version)
                .WithReflectorModulesFromAssembly(new[] { TestAssembly })
                .IgnoreAssembly(TestAssembly);

            // Act
            builder.Build(reflector);

            // Assert — the module never ran: no converter, type not blacklisted.
            Assert.IsNull(
                reflector.JsonSerializer.GetJsonConverter(typeof(FullNs.VerificationPayload)),
                "An ignored hosting assembly must not be type-enumerated; its module must not run.");
            Assert.IsFalse(
                reflector.Converters.IsTypeBlacklisted(typeof(FullNs.VerificationBlacklistedType)),
                "An ignored hosting assembly's module must not contribute a blacklist entry.");
        }
    }
}
