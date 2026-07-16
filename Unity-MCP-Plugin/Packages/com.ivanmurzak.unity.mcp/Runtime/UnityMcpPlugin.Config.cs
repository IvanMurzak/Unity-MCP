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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin
    {
        protected readonly object configMutex = new();

        protected UnityConnectionConfig unityConnectionConfig = null!; // Set by subclass constructors

        public class UnityConnectionConfig : ConnectionConfig
        {
            public static string DefaultHost => $"http://localhost:{GeneratePortFromDirectory()}";

            public static List<McpFeature> DefaultTools => new();
            public static List<McpFeature> DefaultPrompts => new();
            public static List<McpFeature> DefaultResources => new();

            /// <summary>
            /// Backing field for the local server URL. Serialized as "host" in JSON.
            /// Use <see cref="Host"/> for the active connection URL (routes through Cloud mode).
            /// </summary>
            [JsonPropertyName("host")]
            public string LocalHost { get; set; } = DefaultHost;

            /// <summary>
            /// Backing field for the local auth token. Serialized as "token" in JSON.
            /// Use <see cref="Token"/> for the active token (routes through Cloud mode).
            /// </summary>
            [JsonPropertyName("token")]
            public string? LocalToken { get; set; }

            public const string DefaultCloudServerBaseUrl = "https://ai-game.dev";

            public static string CloudServerBaseUrl
            {
                get
                {
                    var args = ArgsUtils.ParseCommandLineArguments();
                    var envValue = args.GetValueOrDefault(EnvironmentUtils.EnvCloudUrl)
                        ?? Environment.GetEnvironmentVariable(EnvironmentUtils.EnvCloudUrl);

                    if (string.IsNullOrWhiteSpace(envValue))
                        return DefaultCloudServerBaseUrl;

                    var normalized = envValue.Trim().Trim('"').TrimEnd('/');

                    if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
                        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    {
                        return DefaultCloudServerBaseUrl;
                    }

                    // Strip trailing "/mcp" so CloudServerUrl doesn't produce "/mcp/mcp"
                    if (normalized.EndsWith("/mcp", StringComparison.OrdinalIgnoreCase))
                        normalized = normalized[..^4];

                    return normalized;
                }
            }

            public static string CloudServerUrl => CloudServerBaseUrl + "/mcp";

            /// <summary>
            /// Returns the active connection host based on <see cref="ConnectionMode"/>.
            /// In Cloud mode, returns <see cref="CloudServerUrl"/>.
            /// In Local mode, returns <see cref="LocalHost"/>.
            /// </summary>
            [JsonIgnore]
            public override string Host
            {
                get => ConnectionMode == ConnectionMode.Cloud ? CloudServerUrl : LocalHost;
                set => LocalHost = value;
            }

            /// <summary>
            /// Gets/sets the active auth token based on <see cref="ConnectionMode"/>.
            /// In Cloud mode, routes to <see cref="CloudToken"/>.
            /// In Local mode, routes to <see cref="LocalToken"/>.
            /// Setter mirrors the getter so env-var / CLI overrides (which write via
            /// the generic Token property) land on the right field regardless of mode.
            /// </summary>
            /// <remarks>
            /// McpPlugin 7.0 removed the static <c>ConnectionConfig.Token</c> string in favour of the
            /// <see cref="ConnectionConfig.CredentialProvider"/> callback. This Unity-side property is kept as
            /// the single source of truth for the effective token (routed by <see cref="ConnectionMode"/>);
            /// the overridden <see cref="CredentialProvider"/> below presents it on every (re)connect, so the
            /// on-the-wire behaviour is identical to the pre-7.0 static-token connection. It is no longer an
            /// <c>override</c> because the base no longer declares <c>Token</c>.
            /// </remarks>
            [JsonIgnore]
            public string? Token
            {
                get => ConnectionMode == ConnectionMode.Cloud ? CloudToken : LocalToken;
                set
                {
                    if (ConnectionMode == ConnectionMode.Cloud)
                        CloudToken = value;
                    else
                        LocalToken = value;
                }
            }

            /// <summary>
            /// Editor-populated machine-store credential provider (mcp-authorize design 06 / D12 zero-button
            /// auto-adopt). When set — by <c>AccountCredentialService.Initialize()</c> — a <b>Cloud</b>-mode
            /// connection presents the proactively-refreshed account JWT read from the shared machine store
            /// (<c>~/.ai-game-dev/credentials.json</c>); a null/empty result falls back to the mode-routed
            /// <see cref="Token"/>, and Local/Custom mode ignores it entirely. Static because the machine
            /// store is per-machine, shared across every config instance and the runtime boot. Runtime-only;
            /// never serialized.
            /// </summary>
            [JsonIgnore]
            public static Func<Task<string?>>? CloudCredentialProvider { get; set; }

            /// <summary>
            /// McpPlugin 7.0 credential provider (replaces the removed static <c>ConnectionConfig.Token</c>).
            /// In <b>Cloud</b> mode it first consults <see cref="CloudCredentialProvider"/> (the shared
            /// machine-store account credential, proactively refreshed — design 06 / D12); when that yields
            /// a token it is presented on the (re)connect, giving zero-button sign-in without any UI. When it
            /// is unset or returns null/empty — and always in Local/Custom mode — it falls back to the
            /// mode-routed <see cref="Token"/>, so the on-the-wire behaviour is identical to the pre-b7
            /// static-token connection. A null token yields an anonymous connection. Runtime-only; never
            /// serialized (<see cref="JsonIgnoreAttribute"/>). The setter is intentionally a no-op: Unity
            /// derives the credential from the machine store / <see cref="Token"/> rather than from a
            /// host-injected provider.
            /// </summary>
            [JsonIgnore]
            public override Func<Task<string?>>? CredentialProvider
            {
                get => async () =>
                {
                    if (ConnectionMode == ConnectionMode.Cloud)
                    {
                        var provider = CloudCredentialProvider;
                        if (provider != null)
                        {
                            var token = await provider().ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(token))
                                return token;
                        }
                    }
                    return Token;
                };
                set { /* Unity derives the credential from the machine store / Token; a host-set provider is intentionally ignored. */ }
            }

            public LogLevel LogLevel { get; set; } = LogLevel.Warning;
            public bool KeepServerRunning { get; set; } = false;
            public TransportMethod TransportMethod { get; set; } = TransportMethod.streamableHttp;
            public AuthOption AuthOption { get; set; } = AuthOption.none;
            public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.Cloud;
            public string? CloudToken { get; set; }
            public List<McpFeature> Tools { get; set; } = new();
            public List<McpFeature> Prompts { get; set; } = new();
            public List<McpFeature> Resources { get; set; } = new();
            public Dictionary<string, bool> SkillAutoGenerate { get; set; } = new();

            /// <summary>
            /// When non-null, only the tools whose names appear in this list are enabled;
            /// all others are disabled. Set by the <c>UNITY_MCP_TOOLS</c> environment variable
            /// (comma-separated tool IDs). Not persisted to disk.
            /// </summary>
            [JsonIgnore]
            public List<string>? EnabledToolsOverride { get; set; }

            public UnityConnectionConfig()
            {
                SetDefault();
            }

            public UnityConnectionConfig SetDefault()
            {
                Host = DefaultHost;
                var isCi = EnvironmentUtils.IsCi();
                KeepConnected = !isCi;
                KeepServerRunning = !isCi;
                GenerateSkillFiles = false;
                SkillsPath = ".claude/skills"; // default skills location for Claude Code
                SkillAutoGenerate = new();
                TransportMethod = TransportMethod.streamableHttp;
                AuthOption = AuthOption.none;
                ConnectionMode = ConnectionMode.Cloud;
                CloudToken = null;
                LogLevel = LogLevel.Warning;
                TimeoutMs = Consts.Hub.DefaultTimeoutMs;
                Tools = DefaultTools;
                Prompts = DefaultPrompts;
                Resources = DefaultResources;
                // Seed the LOCAL server token directly, NOT through the mode-routed `Token` setter.
                // `ConnectionMode` was set to `Cloud` above, so `Token = GenerateToken()` would route
                // the freshly-generated local secret into `CloudToken` and leave `LocalToken` null —
                // the local server token would then never be seeded here, forcing the generate-if-empty
                // fallback in `GetOrCreateConfig` to mint (and re-mint) it, which is exactly how a
                // persisted local token could drift and orphan an already-written client `.mcp.json`
                // (stale Bearer -> 401; Unity-MCP #897 / mcp-authorize i2). `GenerateToken()` produces a
                // local server secret; `CloudToken` is populated by Cloud sign-in, so it stays null here.
                LocalToken = GenerateToken();
                return this;
            }

            /// <summary>
            /// Migrates a persisted legacy <see cref="AuthOption.required"/> value to the offline
            /// <see cref="AuthOption.token"/> mode (mcp-authorize g5/g6). The b5 breaking change deleted
            /// the server-side <c>required</c> strategy, so an un-migrated config would launch the local
            /// server with <c>authorization=required</c> and crash it on boot. <c>required</c> was the
            /// static shared-secret pairing mode, so it maps to the re-added offline <c>token</c> mode
            /// (the same secret, now carried as <c>token=</c>). Returns <c>true</c> when a migration was
            /// applied (so the caller re-saves the healed config); idempotent — a config already on
            /// none/oauth/token is left untouched.
            /// </summary>
            public bool MigrateLegacyAuthOption()
            {
                if (AuthOption == AuthOption.required)
                {
                    AuthOption = AuthOption.token;
                    return true;
                }
                return false;
            }

            public class McpFeature
            {
                public string Name { get; set; } = string.Empty;
                public bool Enabled { get; set; } = true;

                public McpFeature() { }
                public McpFeature(string name, bool enabled)
                {
                    Name = name;
                    Enabled = enabled;
                }
            }
        }
    }

    public enum ConnectionMode
    {
        Custom,
        Cloud
    }
}
