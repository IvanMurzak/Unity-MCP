/*
Copyright (c) 2025 Ivan Murzak
Licensed under the Apache License, Version 2.0.
See the LICENSE file in the project root for more information.
*/

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Notes:
    /// - Purpose: store API keys per-user outside the project (avoid source control and plaintext files).
    /// - EditorPrefs is not suitable for secrets (plaintext, easy to leak via backups/profile sync).
    /// - Unity has no built-in cross-platform secure keystore API for the Editor.
    /// - We use OS credential managers (Windows Credential Manager, macOS Keychain, Linux Secret Service)
    ///   because they provide encryption at rest and user-scoped access control.
    /// </summary>
    public static partial class SecureKeyStore
    {
        private const string ServiceName = "com.ivanmurzak.unity.mcp";
        private const string DisplayName = "AI Game Developer";

        static readonly object InMemoryLock = new();
        static Dictionary<string, byte[]>? inMemoryStore;
        static readonly bool UseInMemoryStore = IsHeadlessEnvironment();

        public static string? Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var targetName = BuildTargetName(key);
            if (UseInMemoryStore)
                return GetInMemory(targetName);

            return Read(targetName);
        }

        public static void Set(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (string.IsNullOrWhiteSpace(value))
            {
                Delete(key);
                return;
            }

            var resolvedValue = value!;
            var targetName = BuildTargetName(key);
            if (UseInMemoryStore)
            {
                SetInMemory(targetName, resolvedValue);
                return;
            }

            Write(targetName, resolvedValue);
        }

        public static void Delete(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            var targetName = BuildTargetName(key);
            if (UseInMemoryStore)
            {
                DeleteInMemory(targetName);
                return;
            }

            DeleteInternal(targetName);
        }

        static string? GetInMemory(string targetName)
        {
            lock (InMemoryLock)
            {
                if (inMemoryStore == null || !inMemoryStore.TryGetValue(targetName, out var bytes))
                    return null;

                if (bytes.Length == 0)
                    return null;

                var value = Encoding.UTF8.GetString(bytes);
                Array.Clear(bytes, 0, bytes.Length);
                inMemoryStore[targetName] = Encoding.UTF8.GetBytes(value);
                return value;
            }
        }

        static void SetInMemory(string targetName, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            lock (InMemoryLock)
            {
                inMemoryStore ??= new Dictionary<string, byte[]>(StringComparer.Ordinal);
                inMemoryStore[targetName] = bytes;
            }
        }

        static void DeleteInMemory(string targetName)
        {
            lock (InMemoryLock)
            {
                if (inMemoryStore == null)
                    return;

                if (inMemoryStore.TryGetValue(targetName, out var bytes))
                    Array.Clear(bytes, 0, bytes.Length);

                inMemoryStore.Remove(targetName);
            }
        }

        static bool IsHeadlessEnvironment()
        {
            if (Application.isBatchMode)
                return true;

            return IsCiEnvironment();
        }

        static bool IsCiEnvironment()
        {
            return HasEnvFlag("CI")
                || HasEnvFlag("GITHUB_ACTIONS")
                || HasEnvFlag("TF_BUILD")
                || HasEnvFlag("BUILD_BUILDID")
                || HasEnvFlag("JENKINS_URL")
                || HasEnvFlag("TEAMCITY_VERSION")
                || HasEnvFlag("GITLAB_CI");
        }

        static bool HasEnvFlag(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
