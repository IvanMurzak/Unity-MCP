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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin.Common;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public class JsonAiAgentConfig : AiAgentConfig
    {
        private readonly Dictionary<string, (JsonNode value, bool required, ValueComparisonMode comparison)> _properties = new();
        private readonly HashSet<string> _propertiesToRemove = new();

        public override string ExpectedFileContent
        {
            get
            {
                var serverConfig = BuildServerEntry();
                var pathSegments = Consts.MCP.Server.BodyPathSegments(BodyPath);

                var innerContent = new JsonObject
                {
                    [DefaultMcpServerName] = serverConfig
                };

                // Build nested structure from innermost to outermost
                var result = innerContent;
                for (int i = pathSegments.Length - 1; i >= 0; i--)
                {
                    result = new JsonObject { [pathSegments[i]] = result };
                }

                return result.ToString();
            }
        }

        public JsonAiAgentConfig(
            string name,
            string configPath,
            string bodyPath = Consts.MCP.Server.DefaultBodyPath)
            : base(
                name: name,
                configPath: configPath,
                bodyPath: bodyPath)
        {
            // empty
        }

        public JsonAiAgentConfig SetProperty(string key, JsonNode value, bool requiredForConfiguration = false, ValueComparisonMode comparison = ValueComparisonMode.Exact)
        {
            _properties[key] = (value, requiredForConfiguration, comparison);
            return this;
        }

        public JsonAiAgentConfig SetPropertyToRemove(string key)
        {
            _propertiesToRemove.Add(key);
            return this;
        }

        public new JsonAiAgentConfig AddIdentityKey(string key)
        {
            base.AddIdentityKey(key);
            return this;
        }

        public override bool Configure()
        {
            if (string.IsNullOrEmpty(ConfigPath))
                return false;

            Debug.Log($"{Consts.Log.Tag} Configuring MCP client with path: {ConfigPath}, bodyPath: {BodyPath}");

            try
            {
                if (!File.Exists(ConfigPath))
                {
                    // Create all necessary directories
                    var directory = Path.GetDirectoryName(ConfigPath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    // Create the file with expected content
                    File.WriteAllText(path: ConfigPath, contents: ExpectedFileContent);
                    return true;
                }

                var json = File.ReadAllText(ConfigPath);
                JsonObject? rootObj = null;

                try
                {
                    rootObj = JsonNode.Parse(json)?.AsObject();
                    if (rootObj == null)
                        throw new Exception("Config file is not a valid JSON object.");
                }
                catch
                {
                    File.WriteAllText(path: ConfigPath, contents: ExpectedFileContent);
                    return true;
                }

                var pathSegments = Consts.MCP.Server.BodyPathSegments(BodyPath);

                // Navigate to or create the target location in the existing JSON
                var targetObj = EnsureJsonPathExists(rootObj, pathSegments);

                // Remove deprecated server entries
                foreach (var name in DeprecatedMcpServerNames)
                    targetObj.Remove(name);

                // Remove duplicate entries that represent the same server under a different name
                RemoveDuplicateServerEntries(targetObj);

                // Get or create the server entry under DefaultMcpServerName
                JsonObject serverEntry;
                if (targetObj[DefaultMcpServerName]?.AsObject() is JsonObject existingEntry)
                {
                    serverEntry = existingEntry;
                }
                else
                {
                    serverEntry = new JsonObject();
                    targetObj[DefaultMcpServerName] = serverEntry;
                }

                // Remove specified properties from the entry
                foreach (var key in _propertiesToRemove)
                    serverEntry.Remove(key);

                // Set properties on the entry (sorted for deterministic output)
                foreach (var key in _properties.Keys.OrderBy(k => k, StringComparer.Ordinal))
                {
                    var clonedValue = _properties[key].value.ToJsonString() is string jsonStr
                        ? JsonNode.Parse(jsonStr)
                        : null;
                    serverEntry[key] = clonedValue;
                }

                // Write back to file
                File.WriteAllText(ConfigPath, rootObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                return IsConfigured();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading config file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        public override bool IsConfigured()
        {
            if (string.IsNullOrEmpty(ConfigPath) || !File.Exists(ConfigPath))
                return false;

            try
            {
                var json = File.ReadAllText(ConfigPath);

                if (string.IsNullOrWhiteSpace(json))
                    return false;

                var rootObj = JsonNode.Parse(json)?.AsObject();
                if (rootObj == null)
                    return false;

                var pathSegments = Consts.MCP.Server.BodyPathSegments(BodyPath);

                // Navigate to the target location using bodyPath segments
                var targetObj = NavigateToJsonPath(rootObj, pathSegments);
                if (targetObj == null)
                    return false;

                var serverEntry = targetObj[DefaultMcpServerName];
                if (serverEntry == null)
                    return false;

                return AreRequiredPropertiesMatching(serverEntry) && !HasPropertiesToRemove(serverEntry);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading config file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        private JsonObject BuildServerEntry()
        {
            var obj = new JsonObject();
            foreach (var key in _properties.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                var clonedValue = _properties[key].value.ToJsonString() is string jsonStr
                    ? JsonNode.Parse(jsonStr)
                    : null;
                obj[key] = clonedValue;
            }
            return obj;
        }

        private bool AreRequiredPropertiesMatching(JsonNode? serverEntry)
        {
            if (serverEntry == null)
                return false;

            foreach (var prop in _properties)
            {
                if (!prop.Value.required)
                    continue;

                var existingValue = serverEntry[prop.Key];
                if (existingValue == null)
                    return false;

                if (!AreJsonValuesEquivalent(prop.Value.comparison, prop.Value.value, existingValue))
                    return false;
            }

            return true;
        }

        private static bool AreJsonValuesEquivalent(ValueComparisonMode comparison, JsonNode expected, JsonNode actual)
        {
            if (comparison == ValueComparisonMode.Path
                && TryGetStringValue(expected, out var expectedPath)
                && TryGetStringValue(actual, out var actualPath))
            {
                return NormalizePath(expectedPath) == NormalizePath(actualPath);
            }

            if (comparison == ValueComparisonMode.Url
                && TryGetStringValue(expected, out var expectedUrl)
                && TryGetStringValue(actual, out var actualUrl))
            {
                return string.Equals(NormalizeUrl(expectedUrl), NormalizeUrl(actualUrl), StringComparison.OrdinalIgnoreCase);
            }

            return expected.ToJsonString() == actual.ToJsonString();
        }

        private static bool TryGetStringValue(JsonNode node, out string value)
        {
            if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var str) && str != null)
            {
                value = str;
                return true;
            }
            value = default!;
            return false;
        }

        /// <summary>Normalizes a file path by unifying separators.</summary>
        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>Normalizes a URL by lowercasing scheme+host and trimming trailing slashes.</summary>
        private static string NormalizeUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var authority = uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
                var pathPart = uri.AbsolutePath.TrimEnd('/');
                return authority + pathPart + uri.Query;
            }
            return url.TrimEnd('/');
        }

        private bool HasPropertiesToRemove(JsonNode? serverEntry)
        {
            if (serverEntry == null || _propertiesToRemove.Count == 0)
                return false;

            foreach (var key in _propertiesToRemove)
            {
                if (serverEntry[key] != null)
                    return true;
            }

            return false;
        }

        private static JsonObject? NavigateToJsonPath(JsonObject rootObj, string[] pathSegments)
        {
            JsonObject? current = rootObj;

            foreach (var segment in pathSegments)
            {
                if (current == null)
                    return null;

                current = current[segment]?.AsObject();
            }

            return current;
        }

        /// <summary>
        /// Removes sibling server entries that represent the same server under a different name,
        /// identified by matching identity key property values (e.g. "command", "url", "serverUrl").
        /// </summary>
        private void RemoveDuplicateServerEntries(JsonObject targetObj)
        {
            // Collect identity values we're about to write
            var ourIdentityValues = new Dictionary<string, (JsonNode value, ValueComparisonMode comparison)>();
            foreach (var identityKey in _identityKeys)
            {
                if (_properties.TryGetValue(identityKey, out var prop))
                    ourIdentityValues[identityKey] = (prop.value, prop.comparison);
            }

            if (ourIdentityValues.Count == 0)
                return;

            // Find sibling entries that share any identity value
            var keysToRemove = new List<string>();
            foreach (var kv in targetObj)
            {
                if (kv.Key == DefaultMcpServerName)
                    continue;

                var entry = kv.Value?.AsObject();
                if (entry == null)
                    continue;

                foreach (var identity in ourIdentityValues)
                {
                    var existingValue = entry[identity.Key];
                    if (existingValue != null && AreJsonValuesEquivalent(identity.Value.comparison, identity.Value.value, existingValue))
                    {
                        keysToRemove.Add(kv.Key);
                        break;
                    }
                }
            }

            foreach (var key in keysToRemove)
                targetObj.Remove(key);
        }

        private static JsonObject EnsureJsonPathExists(JsonObject rootObj, string[] pathSegments)
        {
            JsonObject current = rootObj;

            foreach (var segment in pathSegments)
            {
                if (current[segment]?.AsObject() is JsonObject existingObj)
                {
                    current = existingObj;
                }
                else
                {
                    var newObj = new JsonObject();
                    current[segment] = newObj;
                    current = newObj;
                }
            }

            return current;
        }
    }
}
