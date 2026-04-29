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
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    /// <summary>
    /// Loads MCP plugin configuration from layered sources and (optionally) applies
    /// runtime overrides from environment variables and process command-line arguments.
    ///
    /// Priority (highest wins):
    ///   1. Process command-line flags (e.g. <c>--url</c>, <c>--token</c>, <c>--auth</c>,
    ///      or any <c>--UNITY_MCP_*</c> variant)
    ///   2. Process environment variables (<c>UNITY_MCP_*</c>)
    ///   3. On-disk config (<c>UserSettings/AI-Game-Developer-Config.json</c>)
    ///   4. Built-in defaults
    ///
    /// Overrides applied via env vars / flags are NEVER persisted back to disk —
    /// they are runtime-only. The <see cref="OverrideRecord"/> returned by
    /// <see cref="ApplyEnvironmentOverrides"/> captures both the disk-baseline values
    /// and the override values so that the editor's Save() flow can restore the
    /// baseline before serializing. See <c>UnityMcpPluginEditor.Save</c>.
    /// </summary>
    public static class EnvironmentUtils
    {
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(EnvironmentUtils));

        // Environment variable names for MCP connection overrides.
        public const string EnvHost = "UNITY_MCP_HOST";
        public const string EnvKeepConnected = "UNITY_MCP_KEEP_CONNECTED";
        public const string EnvAuthOption = "UNITY_MCP_AUTH_OPTION";
        public const string EnvToken = "UNITY_MCP_TOKEN";
        public const string EnvTools = "UNITY_MCP_TOOLS";
        public const string EnvStartServer = "UNITY_MCP_START_SERVER";
        public const string EnvTransport = "UNITY_MCP_TRANSPORT";
        public const string EnvCloudUrl = "UNITY_MCP_CLOUD_URL";
        public const string EnvConnectionMode = "UNITY_MCP_CONNECTION_MODE";

        // Short flag aliases recognised by the in-plugin command-line parser.
        // The CLI tool already translates these to the equivalent env vars before
        // launching Unity, but the plugin still parses them directly so
        // out-of-band invocations (manual editor launch, IDE config, etc.) work.
        public const string FlagUrl = "url";
        public const string FlagToken = "token";
        public const string FlagAuth = "auth";

        // Field-name keys used by OverrideRecord to track which fields were overridden
        // by env/flags so the Save flow can restore disk-baseline values before writing.
        public const string FieldHost = nameof(UnityMcpPlugin.UnityConnectionConfig.LocalHost);
        public const string FieldKeepConnected = "KeepConnected";
        public const string FieldAuthOption = "AuthOption";
        public const string FieldLocalToken = nameof(UnityMcpPlugin.UnityConnectionConfig.LocalToken);
        public const string FieldCloudToken = nameof(UnityMcpPlugin.UnityConnectionConfig.CloudToken);
        public const string FieldTools = "EnabledToolsOverride";
        public const string FieldStartServer = "KeepServerRunning";
        public const string FieldTransport = "TransportMethod";
        public const string FieldConnectionMode = "ConnectionMode";

        /// <summary>
        /// Captures, per overridden field, the disk-baseline value (what was read from JSON)
        /// and the runtime override value applied on top. Allows <c>Save()</c> to round-trip
        /// the baseline to disk while keeping the runtime override on the in-memory config.
        /// Empty when no env/flag overrides were detected.
        /// </summary>
        public sealed class OverrideRecord
        {
            public Dictionary<string, object?> BaselineValues { get; } = new();
            public Dictionary<string, object?> OverrideValues { get; } = new();

            public bool HasAny => BaselineValues.Count > 0;
            public bool Contains(string fieldName) => BaselineValues.ContainsKey(fieldName);

            internal void Track(string fieldName, object? baselineValue, object? overrideValue)
            {
                BaselineValues[fieldName] = baselineValue;
                OverrideValues[fieldName] = overrideValue;
            }
        }

        /// <summary>
        /// Checks if the current environment is a CI environment.
        /// </summary>
        public static bool IsCi()
        {
            var commandLineArgs = ArgsUtils.ParseCommandLineArguments();

            var ci = commandLineArgs.GetValueOrDefault("CI") ?? Environment.GetEnvironmentVariable("CI");
            var gha = commandLineArgs.GetValueOrDefault("GITHUB_ACTIONS") ?? Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            var az = commandLineArgs.GetValueOrDefault("TF_BUILD") ?? Environment.GetEnvironmentVariable("TF_BUILD"); // Azure Pipelines

            return string.Equals(ci?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gha?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(az?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Applies environment-variable and command-line-argument overrides to the given config.
        /// Args (highest priority) override env vars, env vars override the disk-baseline values
        /// already present on <paramref name="config"/>. Returns an <see cref="OverrideRecord"/>
        /// that captures the disk-baseline value for each field that was overridden.
        /// </summary>
        public static OverrideRecord ApplyEnvironmentOverrides(UnityMcpPlugin.UnityConnectionConfig config)
        {
            var args = ArgsUtils.ParseCommandLineArguments();
            return ApplyEnvironmentOverrides(config, args, Environment.GetEnvironmentVariable);
        }

        /// <summary>
        /// Test-friendly overload. <paramref name="argReader"/> is the parsed command-line
        /// arg dictionary (keys without leading dashes); <paramref name="envReader"/> resolves
        /// environment variables. Either may be empty / always-null to disable that source.
        /// </summary>
        public static OverrideRecord ApplyEnvironmentOverrides(
            UnityMcpPlugin.UnityConnectionConfig config,
            IReadOnlyDictionary<string, string> argReader,
            Func<string, string?> envReader)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (argReader == null) throw new ArgumentNullException(nameof(argReader));
            if (envReader == null) throw new ArgumentNullException(nameof(envReader));

            var record = new OverrideRecord();

            // Resolve a value with priority: short-flag-alias (highest) > UNITY_MCP_* env-style flag > env var.
            // Returns null if no source provided a non-empty value.
            string? Resolve(string envKey, string? flagAlias)
            {
                // 1. Short flag alias (e.g. "url", "token", "auth")
                if (!string.IsNullOrEmpty(flagAlias)
                    && argReader.TryGetValue(flagAlias, out var flagValue)
                    && !string.IsNullOrWhiteSpace(flagValue))
                {
                    return flagValue;
                }
                // 2. UNITY_MCP_* style flag
                if (argReader.TryGetValue(envKey, out var envFlagValue)
                    && !string.IsNullOrWhiteSpace(envFlagValue))
                {
                    return envFlagValue;
                }
                // 3. Process env var
                var envValue = envReader(envKey);
                if (!string.IsNullOrWhiteSpace(envValue))
                    return envValue;
                return null;
            }

            string Sanitize(string raw) => raw.Trim().Trim('"');

            // --- Host URL override (UNITY_MCP_HOST or UNITY_MCP_CLOUD_URL or --url) ---
            // UNITY_MCP_CLOUD_URL is the canonical key in the layered loader contract.
            // UNITY_MCP_HOST is preserved for backward compatibility.
            // --url is a short alias.
            // All values are defensively trimmed of trailing slashes.
            string? sanitizedHost = null;
            var rawHost = Resolve(EnvCloudUrl, FlagUrl) ?? Resolve(EnvHost, flagAlias: null);
            if (rawHost != null)
            {
                var host = Sanitize(rawHost).TrimEnd('/');
                if (host.Length > 0)
                {
                    sanitizedHost = host;
                    if (!string.Equals(host, config.LocalHost, StringComparison.Ordinal))
                    {
                        record.Track(FieldHost, config.LocalHost, host);
                        config.LocalHost = host;
                        _logger.LogInformation("[MCP] Override: {Key}={Value}", FieldHost, host);
                    }
                }
            }

            // --- Connection mode override (UNITY_MCP_CONNECTION_MODE) ---
            // Explicit env / flag wins; otherwise, if a host URL was provided and points to
            // a loopback address, infer Custom mode (worktree / local-dev scenarios). If the
            // URL points to a remote host, leave the disk-baseline ConnectionMode untouched.
            var rawMode = Resolve(EnvConnectionMode, flagAlias: null);
            ConnectionMode? targetMode = null;
            if (rawMode != null && Enum.TryParse<ConnectionMode>(Sanitize(rawMode), ignoreCase: true, out var explicitMode))
            {
                targetMode = explicitMode;
            }
            else if (sanitizedHost != null && IsLoopbackUrl(sanitizedHost))
            {
                targetMode = ConnectionMode.Custom;
            }
            if (targetMode.HasValue && targetMode.Value != config.ConnectionMode)
            {
                record.Track(FieldConnectionMode, config.ConnectionMode, targetMode.Value);
                config.ConnectionMode = targetMode.Value;
                _logger.LogInformation("[MCP] Override: {Key}={Value}", FieldConnectionMode, targetMode.Value);
            }

            // --- KeepConnected override ---
            var rawKeep = Resolve(EnvKeepConnected, flagAlias: null);
            if (rawKeep != null && bool.TryParse(Sanitize(rawKeep), out var keep) && keep != config.KeepConnected)
            {
                record.Track(FieldKeepConnected, config.KeepConnected, keep);
                config.KeepConnected = keep;
                _logger.LogInformation("[MCP] Override: {Key}={Value}", EnvKeepConnected, keep);
            }

            // --- AuthOption override (none / required / optional) ---
            var rawAuth = Resolve(EnvAuthOption, FlagAuth);
            if (rawAuth != null
                && Enum.TryParse<AuthOption>(Sanitize(rawAuth), ignoreCase: true, out var ao)
                && ao != config.AuthOption)
            {
                record.Track(FieldAuthOption, config.AuthOption, ao);
                config.AuthOption = ao;
                _logger.LogInformation("[MCP] Override: {Key}={Value}", EnvAuthOption, ao);
            }

            // --- Auth token override ---
            // Resolved AFTER ConnectionMode so we route to the correct underlying field
            // (LocalToken in Custom mode, CloudToken in Cloud mode). We track the specific
            // backing field rather than the abstract Token property because only the backing
            // fields are serialised — restoring the baseline must target the same field that
            // was clobbered.
            var rawToken = Resolve(EnvToken, FlagToken);
            if (rawToken != null)
            {
                var token = Sanitize(rawToken);
                if (config.ConnectionMode == ConnectionMode.Cloud)
                {
                    if (!string.Equals(token, config.CloudToken, StringComparison.Ordinal))
                    {
                        record.Track(FieldCloudToken, config.CloudToken, token);
                        config.CloudToken = token;
                        _logger.LogInformation("[MCP] Override: {Key}=*** (CloudToken)", EnvToken);
                    }
                }
                else
                {
                    if (!string.Equals(token, config.LocalToken, StringComparison.Ordinal))
                    {
                        record.Track(FieldLocalToken, config.LocalToken, token);
                        config.LocalToken = token;
                        _logger.LogInformation("[MCP] Override: {Key}=*** (LocalToken)", EnvToken);
                    }
                }
            }

            // --- Transport method override ---
            var rawTransport = Resolve(EnvTransport, flagAlias: null);
            if (rawTransport != null
                && Enum.TryParse<TransportMethod>(Sanitize(rawTransport), ignoreCase: true, out var tm)
                && tm != config.TransportMethod)
            {
                record.Track(FieldTransport, config.TransportMethod, tm);
                config.TransportMethod = tm;
                _logger.LogInformation("[MCP] Override: {Key}={Value}", EnvTransport, tm);
            }

            // --- Start server override ---
            var rawStart = Resolve(EnvStartServer, flagAlias: null);
            if (rawStart != null && bool.TryParse(Sanitize(rawStart), out var ss) && ss != config.KeepServerRunning)
            {
                record.Track(FieldStartServer, config.KeepServerRunning, ss);
                config.KeepServerRunning = ss;
                _logger.LogInformation("[MCP] Override: {Key}={Value}", EnvStartServer, ss);
            }

            // --- Enabled tools override (comma-separated) ---
            // EnabledToolsOverride is [JsonIgnore] so it is never persisted regardless of the
            // baseline-restoration logic; we still track it for completeness.
            var rawTools = Resolve(EnvTools, flagAlias: null);
            if (rawTools != null)
            {
                var ids = rawTools.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var trimmed = new List<string>(ids.Length);
                foreach (var id in ids)
                {
                    var value = Sanitize(id);
                    if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
                        trimmed.Add(value);
                }
                record.Track(FieldTools, config.EnabledToolsOverride, trimmed);
                config.EnabledToolsOverride = trimmed;
                _logger.LogInformation("[MCP] Override: {Key}={Value}", EnvTools, rawTools.Trim());
            }

            return record;
        }

        /// <summary>
        /// Applies the BaselineValues from <paramref name="record"/> to <paramref name="config"/>
        /// (i.e. reverses runtime overrides so the in-memory config reflects what is on disk).
        /// Used by the editor's Save() flow to ensure runtime-only overrides are not persisted.
        /// </summary>
        public static void ApplyBaseline(UnityMcpPlugin.UnityConnectionConfig config, OverrideRecord record)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (record == null) throw new ArgumentNullException(nameof(record));
            ApplyValues(config, record.BaselineValues);
        }

        /// <summary>
        /// Re-applies the OverrideValues from <paramref name="record"/> to <paramref name="config"/>
        /// (i.e. restores runtime overrides after a temporary baseline-restore).
        /// </summary>
        public static void ApplyOverrides(UnityMcpPlugin.UnityConnectionConfig config, OverrideRecord record)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (record == null) throw new ArgumentNullException(nameof(record));
            ApplyValues(config, record.OverrideValues);
        }

        static void ApplyValues(UnityMcpPlugin.UnityConnectionConfig config, IReadOnlyDictionary<string, object?> values)
        {
            foreach (var kvp in values)
            {
                switch (kvp.Key)
                {
                    case FieldHost:
                        config.LocalHost = (string?)kvp.Value ?? string.Empty;
                        break;
                    case FieldKeepConnected:
                        if (kvp.Value is bool kc) config.KeepConnected = kc;
                        break;
                    case FieldAuthOption:
                        if (kvp.Value is AuthOption ao) config.AuthOption = ao;
                        break;
                    case FieldLocalToken:
                        config.LocalToken = (string?)kvp.Value;
                        break;
                    case FieldCloudToken:
                        config.CloudToken = (string?)kvp.Value;
                        break;
                    case FieldTools:
                        config.EnabledToolsOverride = (List<string>?)kvp.Value;
                        break;
                    case FieldStartServer:
                        if (kvp.Value is bool ss) config.KeepServerRunning = ss;
                        break;
                    case FieldTransport:
                        if (kvp.Value is TransportMethod tm) config.TransportMethod = tm;
                        break;
                    case FieldConnectionMode:
                        if (kvp.Value is ConnectionMode cm) config.ConnectionMode = cm;
                        break;
                }
            }
        }

        /// <summary>
        /// Returns true if the given URL targets a loopback host
        /// (<c>localhost</c>, <c>127.0.0.0/8</c>, or IPv6 <c>::1</c>).
        /// Used to infer <see cref="ConnectionMode.Custom"/> when a worktree-style local URL is supplied.
        /// </summary>
        public static bool IsLoopbackUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;
            var host = uri.Host;
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;
            if (System.Net.IPAddress.TryParse(host, out var ip))
                return System.Net.IPAddress.IsLoopback(ip);
            return false;
        }
    }
}
