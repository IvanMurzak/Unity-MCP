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
using System.Text.Json;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// The lease a CLI session writes for an editor it started (<c>&lt;project&gt;/.ai-game-dev/session.json</c>, owned by
    /// cli-core's <c>EditorSession</c>). The editor only ever removes it, and only while it still carries its own nonce.
    /// </summary>
    public static class CliSessionLease
    {
        public const string DirectoryName = ".ai-game-dev";
        public const string FileName = "session.json";

        public static string PathFor(string projectRoot) => Path.Combine(projectRoot, DirectoryName, FileName);

        /// <summary>Whether the lease at <paramref name="leasePath"/> carries <paramref name="nonce"/>.</summary>
        public static bool IsOwn(string leasePath, string nonce)
        {
            try
            {
                if (!File.Exists(leasePath))
                    return false;
                using var document = JsonDocument.Parse(File.ReadAllText(leasePath));
                return document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("nonce", out var value)
                    && value.ValueKind == JsonValueKind.String
                    && string.Equals(value.GetString(), nonce, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false; // unreadable or malformed: not provably ours
            }
        }

        /// <summary>Compare-and-delete: removes the lease only while it carries <paramref name="nonce"/>. True when removed.</summary>
        public static bool TryDeleteOwn(string leasePath, string nonce)
        {
            if (!IsOwn(leasePath, nonce))
                return false;
            try
            {
                File.Delete(leasePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
