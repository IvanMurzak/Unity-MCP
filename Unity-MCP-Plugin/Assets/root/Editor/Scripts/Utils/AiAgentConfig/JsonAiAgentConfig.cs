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
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin.Common;
using UnityEngine;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public class JsonAiAgentConfig : AiAgentConfig
    {
        private readonly Dictionary<string, (JsonNode value, bool required)> _properties = new();
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
            TransportMethod transportMethod,
            string? transportMethodValue = null,
            string bodyPath = Consts.MCP.Server.DefaultBodyPath)
            : base(
                name: name,
                transportMethod: transportMethod,
                transportMethodValue: transportMethodValue,
                configPath: configPath,
                bodyPath: bodyPath)
        {
            // empty
        }

        public JsonAiAgentConfig SetProperty(string key, JsonNode value, bool requiredForConfiguration = false)
        {
            _properties[key] = (value, requiredForConfiguration);
            return this;
        }

        public JsonAiAgentConfig SetPropertyToRemove(string key)
        {
            _propertiesToRemove.Add(key);
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

                // Set properties on the entry
                foreach (var prop in _properties)
                {
                    var clonedValue = prop.Value.value.ToJsonString() is string jsonStr
                        ? JsonNode.Parse(jsonStr)
                        : null;
                    serverEntry[prop.Key] = clonedValue;
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

                foreach (var kv in targetObj)
                {
                    if (!AreRequiredPropertiesMatching(kv.Value))
                        continue;

                    if (HasPropertiesToRemove(kv.Value))
                        continue;

                    return true;
                }

                return false;
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
            foreach (var prop in _properties)
            {
                var clonedValue = prop.Value.value.ToJsonString() is string jsonStr
                    ? JsonNode.Parse(jsonStr)
                    : null;
                obj[prop.Key] = clonedValue;
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

                var expectedJson = prop.Value.value.ToJsonString();
                var actualJson = existingValue.ToJsonString();
                if (expectedJson != actualJson)
                    return false;
            }

            return true;
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
