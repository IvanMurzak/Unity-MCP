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

namespace com.IvanMurzak.Unity.MCP.Editor.DevControl
{
    /// <summary>
    /// PURE-MANAGED resolver for the DEV-ONLY bridge's two config vars, with precedence
    /// <c>process env &gt; project-root <c>.env</c> file &gt; default</c>. Unity normally reads only
    /// process env (<c>EnvironmentUtils</c>), but the editor is launched from the GUI / IDE with no
    /// shell exports — so this adds a thin <c>.env</c>-file layer beneath the process env, letting a
    /// developer (or the infra worktree provisioner) drop <c>UNITY_MCP_DEV_CONTROL=1</c> into the
    /// project's <c>.env</c> without exporting anything. Mirrors Godot's <c>GodotMcpEnvFile.LookupRaw</c>.
    ///
    /// <para>No UnityEngine / UnityEditor types — the caller (<c>Startup</c>) passes the project-root
    /// path in, so this stays unit-testable headless and never throws (a missing/unreadable
    /// <c>.env</c> is the common case, not an error).</para>
    /// </summary>
    public static class DevControlEnv
    {
        /// <summary>Enable flag: the server starts ONLY when this resolves to exactly <c>"1"</c>.</summary>
        public const string EnvEnable = "UNITY_MCP_DEV_CONTROL";

        /// <summary>Port override; falls back to <see cref="DevControlServer.DefaultPort"/> when unset/unparseable.</summary>
        public const string EnvPort = "UNITY_MCP_DEV_CONTROL_PORT";

        /// <summary>
        /// Resolve <paramref name="key"/> with precedence process-env &gt; <c>.env</c> file (at
        /// <c>&lt;projectRoot&gt;/.env</c>) &gt; null. Returns the trimmed/unquoted value or null when
        /// neither source carries a non-empty value. Never throws.
        /// </summary>
        public static string? Resolve(string key, string? projectRootPath)
        {
            var fromProcess = SafeGetEnv(key);
            if (!string.IsNullOrEmpty(fromProcess))
                return fromProcess;

            if (string.IsNullOrEmpty(projectRootPath))
                return null;

            var envPath = Path.Combine(projectRootPath, ".env");
            return LookupRaw(envPath, key);
        }

        /// <summary>
        /// True when the dev-control bridge is enabled for the given project root (the enable var
        /// resolves to exactly <c>"1"</c> through the precedence chain).
        /// </summary>
        public static bool IsEnabled(string? projectRootPath)
            => Resolve(EnvEnable, projectRootPath) == "1";

        /// <summary>
        /// Resolve the port for the given project root, falling back to <paramref name="defaultPort"/>
        /// when the var is unset or not a valid 1..65535 integer.
        /// </summary>
        public static int ResolvePort(string? projectRootPath, int defaultPort)
        {
            var raw = Resolve(EnvPort, projectRootPath);
            if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out var parsed) && parsed > 0 && parsed <= 65535)
                return parsed;
            return defaultPort;
        }

        static string? SafeGetEnv(string key)
        {
            try { return Normalize(Environment.GetEnvironmentVariable(key)); }
            catch { return null; }
        }

        /// <summary>
        /// Read ONE key's value from the <c>.env</c> file at <paramref name="path"/>. Skips blank /
        /// <c>#</c>-comment lines, splits on the first <c>=</c>, trims the key, and normalizes the value
        /// (trim + strip a single pair of wrapping quotes). Last occurrence wins. Returns null when the
        /// file is missing/unreadable, the key is absent, or the value is blank. Never throws.
        /// </summary>
        public static string? LookupRaw(string? path, string key)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrEmpty(key))
                return null;

            string[] lines;
            try
            {
                if (!File.Exists(path))
                    return null;
                lines = File.ReadAllLines(path);
            }
            catch (Exception)
            {
                return null;
            }

            string? found = null; // last occurrence wins
            foreach (var rawLine in lines)
            {
                if (rawLine == null)
                    continue;
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                var eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                if (!string.Equals(line.Substring(0, eq).Trim(), key, StringComparison.Ordinal))
                    continue;
                var value = Normalize(line.Substring(eq + 1));
                if (!string.IsNullOrEmpty(value))
                    found = value;
            }
            return found;
        }

        /// <summary>Trim whitespace and strip a single pair of wrapping single or double quotes.</summary>
        static string? Normalize(string? raw)
        {
            if (raw == null)
                return null;
            var trimmed = raw.Trim();
            if (trimmed.Length >= 2 &&
                ((trimmed[0] == '"' && trimmed[^1] == '"') || (trimmed[0] == '\'' && trimmed[^1] == '\'')))
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            return trimmed;
        }
    }
}
