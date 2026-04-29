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
using System.Text.Json;
using System.Text.Json.Serialization;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Covers the layered config-loader contract:
    ///   flags > env vars > on-disk config > defaults
    /// implemented by <see cref="EnvironmentUtils.ApplyEnvironmentOverrides"/>.
    /// </summary>
    public class EnvironmentUtilsTests
    {
        const string DiskHost = "http://localhost:24029";
        const string DiskLocalToken = "DISK_LOCAL_TOKEN";
        const string DiskCloudToken = "DISK_CLOUD_TOKEN";

        static UnityMcpPlugin.UnityConnectionConfig BuildDiskConfig(ConnectionMode mode = ConnectionMode.Cloud)
        {
            // Simulates a config that came back from disk: explicit values for both
            // local and cloud tokens, default mode = Cloud (matching the plugin's default).
            return new UnityMcpPlugin.UnityConnectionConfig
            {
                LocalHost = DiskHost,
                LocalToken = DiskLocalToken,
                CloudToken = DiskCloudToken,
                ConnectionMode = mode,
                AuthOption = AuthOption.required,
                KeepConnected = true,
                KeepServerRunning = false,
                TransportMethod = TransportMethod.streamableHttp
            };
        }

        static IReadOnlyDictionary<string, string> NoArgs()
            => new Dictionary<string, string>();
        static Func<string, string?> NoEnv()
            => _ => null;
        static Func<string, string?> Env(params (string k, string v)[] pairs)
        {
            var dict = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var (k, v) in pairs) dict[k] = v;
            return key => dict.TryGetValue(key, out var val) ? val : null;
        }
        static IReadOnlyDictionary<string, string> Args(params (string k, string v)[] pairs)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (k, v) in pairs) dict[k] = v;
            return dict;
        }

        // --- Disk-only path: env / args absent ---

        [Test]
        public void Override_DiskOnly_NoEnvNoArgs_LeavesConfigUnchanged()
        {
            var config = BuildDiskConfig();
            var record = EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), NoEnv());

            Assert.IsFalse(record.HasAny, "Expected no overrides when env and args are empty.");
            Assert.AreEqual(DiskHost, config.LocalHost);
            Assert.AreEqual(DiskCloudToken, config.CloudToken);
            Assert.AreEqual(DiskLocalToken, config.LocalToken);
            Assert.AreEqual(ConnectionMode.Cloud, config.ConnectionMode);
            Assert.AreEqual(AuthOption.required, config.AuthOption);
        }

        // --- Env var override ---

        [Test]
        public void Override_EnvToken_WritesToCloudTokenWhenModeIsCloud()
        {
            var config = BuildDiskConfig(mode: ConnectionMode.Cloud);
            var env = Env((EnvironmentUtils.EnvToken, "ENV_TOKEN"));

            var record = EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);

            Assert.IsTrue(record.HasAny);
            Assert.IsTrue(record.Contains(EnvironmentUtils.FieldCloudToken),
                "Expected the CloudToken backing field to be tracked when mode is Cloud.");
            Assert.AreEqual("ENV_TOKEN", config.CloudToken);
            Assert.AreEqual(DiskLocalToken, config.LocalToken,
                "LocalToken must remain at the disk baseline when mode is Cloud.");
        }

        [Test]
        public void Override_EnvToken_WritesToLocalTokenWhenModeIsCustom()
        {
            var config = BuildDiskConfig(mode: ConnectionMode.Custom);
            var env = Env((EnvironmentUtils.EnvToken, "ENV_TOKEN"));

            EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);

            Assert.AreEqual("ENV_TOKEN", config.LocalToken);
            Assert.AreEqual(DiskCloudToken, config.CloudToken);
        }

        // --- Flag override (highest priority, beats env) ---

        [Test]
        public void Override_FlagToken_BeatsEnvToken()
        {
            var config = BuildDiskConfig(mode: ConnectionMode.Cloud);
            var args = Args((EnvironmentUtils.FlagToken, "FLAG_TOKEN"));
            var env = Env((EnvironmentUtils.EnvToken, "ENV_TOKEN"));

            EnvironmentUtils.ApplyEnvironmentOverrides(config, args, env);

            Assert.AreEqual("FLAG_TOKEN", config.CloudToken,
                "When both --token and UNITY_MCP_TOKEN are set, the flag must win.");
        }

        [Test]
        public void Override_UnityMcpStyleFlag_BeatsEnvVar()
        {
            // CLI translates --token foo into UNITY_MCP_TOKEN env, but the plugin still
            // accepts --UNITY_MCP_TOKEN=foo style flags directly. They must beat env vars.
            var config = BuildDiskConfig(mode: ConnectionMode.Cloud);
            var args = Args((EnvironmentUtils.EnvToken, "FLAG_VIA_FULL_NAME"));
            var env = Env((EnvironmentUtils.EnvToken, "ENV_TOKEN"));

            EnvironmentUtils.ApplyEnvironmentOverrides(config, args, env);

            Assert.AreEqual("FLAG_VIA_FULL_NAME", config.CloudToken);
        }

        // --- Per-field layering ---

        [Test]
        public void Override_PerField_TokenFromEnv_HostFromDisk()
        {
            var config = BuildDiskConfig();
            var env = Env((EnvironmentUtils.EnvToken, "ENV_TOKEN"));

            EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);

            Assert.AreEqual(DiskHost, config.LocalHost,
                "Host must remain at the disk baseline when only the token was overridden.");
            Assert.AreEqual("ENV_TOKEN", config.CloudToken);
        }

        // --- Trailing-slash robustness ---

        [Test]
        public void Override_TrailingSlashOnHostUrl_IsStripped()
        {
            var config = BuildDiskConfig();
            var env = Env((EnvironmentUtils.EnvCloudUrl, "http://localhost:5220/"));

            EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);

            Assert.AreEqual("http://localhost:5220", config.LocalHost,
                "Trailing slash must be stripped defensively.");
        }

        [TestCase("http://localhost:5220/", true)]
        [TestCase("http://127.0.0.1:5220", true)]
        [TestCase("http://[::1]:5220/", true)]
        [TestCase("https://ai-game.dev", false)]
        [TestCase("not-a-url", false)]
        [TestCase("", false)]
        public void IsLoopbackUrl_RecognisesLoopbackHosts(string url, bool expected)
        {
            Assert.AreEqual(expected, EnvironmentUtils.IsLoopbackUrl(url),
                $"IsLoopbackUrl mismatch for '{url}'.");
        }

        [Test]
        public void Override_LoopbackHostUrl_InfersCustomMode()
        {
            // Worktree scenario: disk says Cloud, env supplies a localhost URL.
            // Expected: ConnectionMode is inferred to Custom so the worktree's local
            // dev token routes to LocalToken (not CloudToken) and the SignalR client
            // talks to <host>/hub/mcp-server (not <host>/mcp/hub/mcp-server).
            var config = BuildDiskConfig(mode: ConnectionMode.Cloud);
            var env = Env(
                (EnvironmentUtils.EnvCloudUrl, "http://localhost:5220/"),
                (EnvironmentUtils.EnvToken, "WORKTREE_TOKEN"));

            var record = EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);

            Assert.AreEqual(ConnectionMode.Custom, config.ConnectionMode,
                "Loopback host URL should infer Custom mode.");
            Assert.AreEqual("http://localhost:5220", config.LocalHost);
            Assert.AreEqual("WORKTREE_TOKEN", config.LocalToken,
                "Token override must route to LocalToken once mode is inferred Custom.");
            Assert.AreEqual(DiskCloudToken, config.CloudToken,
                "CloudToken on disk must NOT be clobbered by the env override.");
            Assert.IsTrue(record.Contains(EnvironmentUtils.FieldConnectionMode));
            Assert.IsTrue(record.Contains(EnvironmentUtils.FieldHost));
            Assert.IsTrue(record.Contains(EnvironmentUtils.FieldLocalToken));
        }

        [Test]
        public void Override_RemoteHostUrl_DoesNotInferCustomMode()
        {
            var config = BuildDiskConfig(mode: ConnectionMode.Cloud);
            var env = Env((EnvironmentUtils.EnvCloudUrl, "https://ai-game.dev"));

            EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);

            Assert.AreEqual(ConnectionMode.Cloud, config.ConnectionMode,
                "Non-loopback URL must NOT auto-flip the mode.");
        }

        [Test]
        public void Override_ExplicitConnectionMode_BeatsLoopbackInference()
        {
            var config = BuildDiskConfig(mode: ConnectionMode.Cloud);
            var env = Env(
                (EnvironmentUtils.EnvCloudUrl, "http://localhost:5220"),
                (EnvironmentUtils.EnvConnectionMode, "Cloud"));

            EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);

            Assert.AreEqual(ConnectionMode.Cloud, config.ConnectionMode,
                "Explicit UNITY_MCP_CONNECTION_MODE must beat loopback inference.");
        }

        // --- Persistence: overrides MUST NOT be written to disk ---

        [Test]
        public void Persistence_BaselineRoundTripsToDiskWithoutOverrides()
        {
            var config = BuildDiskConfig(mode: ConnectionMode.Cloud);
            var env = Env(
                (EnvironmentUtils.EnvCloudUrl, "http://localhost:5220"),
                (EnvironmentUtils.EnvToken, "ENV_TOKEN"));

            var record = EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);
            // sanity
            Assert.AreEqual(ConnectionMode.Custom, config.ConnectionMode);
            Assert.AreEqual("http://localhost:5220", config.LocalHost);
            Assert.AreEqual("ENV_TOKEN", config.LocalToken);

            // Simulate Save: restore baseline → serialize → restore overrides.
            EnvironmentUtils.ApplyBaseline(config, record);

            Assert.AreEqual(DiskHost, config.LocalHost, "Baseline restore failed for LocalHost.");
            Assert.AreEqual(DiskLocalToken, config.LocalToken, "Baseline restore failed for LocalToken.");
            Assert.AreEqual(ConnectionMode.Cloud, config.ConnectionMode, "Baseline restore failed for ConnectionMode.");

            var json = SerializeForDisk(config);
            // The serialized JSON must NOT contain the runtime override values.
            StringAssert.DoesNotContain("http://localhost:5220", json);
            StringAssert.DoesNotContain("ENV_TOKEN", json);
            StringAssert.Contains(DiskHost, json);
            StringAssert.Contains(DiskLocalToken, json);

            // After serialization, re-apply overrides.
            EnvironmentUtils.ApplyOverrides(config, record);
            Assert.AreEqual(ConnectionMode.Custom, config.ConnectionMode);
            Assert.AreEqual("http://localhost:5220", config.LocalHost);
            Assert.AreEqual("ENV_TOKEN", config.LocalToken);
        }

        [Test]
        public void Persistence_EmptyRecord_NoOpOnApplyBaselineAndOverrides()
        {
            var config = BuildDiskConfig();
            var record = new EnvironmentUtils.OverrideRecord();
            EnvironmentUtils.ApplyBaseline(config, record);
            EnvironmentUtils.ApplyOverrides(config, record);
            // No exceptions, values unchanged.
            Assert.AreEqual(DiskHost, config.LocalHost);
        }

        // --- Validation: bad inputs are tolerated ---

        [Test]
        public void Override_InvalidEnumValue_IsIgnored()
        {
            var config = BuildDiskConfig(mode: ConnectionMode.Cloud);
            var env = Env((EnvironmentUtils.EnvConnectionMode, "Bogus"));

            EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);

            Assert.AreEqual(ConnectionMode.Cloud, config.ConnectionMode,
                "Unparseable enum values must be silently ignored.");
        }

        [Test]
        public void Override_QuotedValue_IsTrimmed()
        {
            var config = BuildDiskConfig();
            var env = Env((EnvironmentUtils.EnvToken, "\"QUOTED_TOKEN\""));

            EnvironmentUtils.ApplyEnvironmentOverrides(config, NoArgs(), env);

            Assert.AreEqual("QUOTED_TOKEN", config.CloudToken);
        }

        // --- Helpers ---

        static string SerializeForDisk(UnityMcpPlugin.UnityConnectionConfig config)
        {
            return JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            });
        }
    }
}
