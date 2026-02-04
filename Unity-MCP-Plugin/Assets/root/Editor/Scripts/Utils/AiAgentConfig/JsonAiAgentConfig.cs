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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin.Common;
using UnityEngine;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public class JsonAiAgentConfig : AiAgentConfig
    {
        public override string ExpectedFileContent => TransportMethod == TransportMethod.streamableHttp
            ? Startup.Server.RawJsonConfigurationHttp(UnityMcpPlugin.Host, BodyPath, type: McpServerHttpType).ToString()
            : Startup.Server.RawJsonConfigurationStdio(UnityMcpPlugin.Port, BodyPath, UnityMcpPlugin.TimeoutMs, type: McpServerStdioType).ToString();

        public JsonAiAgentConfig(string name, TransportMethod transportMethod, string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
            : base(name, transportMethod, configPath, bodyPath)
        {
            // empty
        }

        public override bool Configure() => ConfigureJsonMcpClient(ConfigPath, BodyPath, TransportMethod);
        public override bool IsConfigured() => IsMcpClientConfigured(ConfigPath, BodyPath, TransportMethod);

        public static bool ConfigureJsonMcpClient(string configPath, string bodyPath, TransportMethod transportMethod)
        {
            if (string.IsNullOrEmpty(configPath))
                return false;

            Debug.Log($"{Consts.Log.Tag} Configuring MCP client with path: {configPath}, bodyPath: {bodyPath}, transport: {transportMethod}");

            try
            {
                // Generate the appropriate configuration based on transport method
                var rawConfig = transportMethod == TransportMethod.streamableHttp
                    ? Startup.Server.RawJsonConfigurationHttp(UnityMcpPlugin.Host, bodyPath)
                    : Startup.Server.RawJsonConfigurationStdio(UnityMcpPlugin.Port, bodyPath, UnityMcpPlugin.TimeoutMs);

                if (!File.Exists(configPath))
                {
                    // Create all necessary directories
                    var directory = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    // Create the file if it doesn't exist
                    File.WriteAllText(path: configPath, contents: rawConfig.ToString());
                    return true;
                }

                var json = File.ReadAllText(configPath);
                JsonObject? rootObj = null;

                try
                {
                    // Parse the existing config as JsonObject
                    rootObj = JsonNode.Parse(json)?.AsObject();
                    if (rootObj == null)
                        throw new Exception("Config file is not a valid JSON object.");
                }
                catch
                {
                    File.WriteAllText(path: configPath, contents: rawConfig.ToString());
                    return true;
                }

                // Get path segments and navigate to the injection target
                var pathSegments = Consts.MCP.Server.BodyPathSegments(bodyPath);

                // Generate the configuration to inject using the last segment as bodyPath
                var injectObj = transportMethod == TransportMethod.streamableHttp
                    ? Startup.Server.RawJsonConfigurationHttp(UnityMcpPlugin.Host, pathSegments.Last())
                    : Startup.Server.RawJsonConfigurationStdio(UnityMcpPlugin.Port, pathSegments.Last(), UnityMcpPlugin.TimeoutMs);

                if (injectObj == null)
                    throw new Exception("Injected config is not a valid JSON object.");

                var injectMcpServers = injectObj[pathSegments.Last()]?.AsObject();
                if (injectMcpServers == null)
                    throw new Exception($"Missing '{pathSegments.Last()}' object in inject config.");

                // Navigate to or create the target location in the existing JSON
                var targetObj = EnsureJsonPathExists(rootObj, pathSegments);

                // Find entries to remove based on transport method
                // For stdio: remove entries with matching command
                // For http: remove entries with matching url
                // Also remove entries with properties from the other transport method
                var keysToRemove = targetObj
                    .Where(kv =>
                    {
                        var command = kv.Value?["command"]?.GetValue<string>();
                        var url = kv.Value?["url"]?.GetValue<string>();

                        // Remove if command matches (for stdio detection)
                        if (!string.IsNullOrEmpty(command) && IsCommandMatch(command!))
                            return true;

                        // Remove if url matches (for http detection)
                        if (!string.IsNullOrEmpty(url) && IsUrlMatch(url!))
                            return true;

                        return false;
                    })
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                    targetObj.Remove(key);

                // Merge/overwrite entries from injectMcpServers
                foreach (var kv in injectMcpServers)
                {
                    // Clone the value to avoid parent conflict
                    targetObj[kv.Key] = kv.Value?.ToJsonString() is string jsonStr
                        ? JsonNode.Parse(jsonStr)
                        : null;
                }

                // Write back to file
                File.WriteAllText(configPath, rootObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                return IsMcpClientConfigured(configPath, bodyPath, transportMethod);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading config file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }
        public static bool IsMcpClientConfigured(string configPath, string bodyPath, TransportMethod transportMethod)
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return false;

            try
            {
                var json = File.ReadAllText(configPath);

                if (string.IsNullOrWhiteSpace(json))
                    return false;

                var rootObj = JsonNode.Parse(json)?.AsObject();
                if (rootObj == null)
                    return false;

                var pathSegments = Consts.MCP.Server.BodyPathSegments(bodyPath);

                // Navigate to the target location using bodyPath segments
                var targetObj = NavigateToJsonPath(rootObj, pathSegments);
                if (targetObj == null)
                    return false;

                foreach (var kv in targetObj)
                {
                    if (transportMethod == TransportMethod.streamableHttp)
                    {
                        // For http: check url and type, ensure no command/args
                        if (!IsHttpConfigValid(kv.Value))
                            continue;
                        return true;
                    }
                    else
                    {
                        // For stdio: check command and args, ensure no url
                        if (!IsStdioConfigValid(kv.Value))
                            continue;
                        return true;
                    }
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

        /// <summary>
        /// Validates that a server entry is correctly configured for HTTP transport.
        /// Must have: type="http", url matching expected
        /// Must NOT have: command, args
        /// </summary>
        protected static bool IsHttpConfigValid(JsonNode? serverEntry)
        {
            if (serverEntry == null)
                return false;

            var url = serverEntry["url"]?.GetValue<string>();
            var type = serverEntry["type"]?.GetValue<string>();
            var command = serverEntry["command"];
            var args = serverEntry["args"];

            // Must have correct url and type
            if (string.IsNullOrEmpty(url) || !IsUrlMatch(url!))
                return false;

            if (type != "http")
                return false;

            // Must NOT have stdio properties
            if (command != null || args != null)
                return false;

            return true;
        }

        /// <summary>
        /// Validates that a server entry is correctly configured for stdio transport.
        /// Must have: command matching expected, args with correct port/timeout
        /// Must NOT have: url
        /// </summary>
        protected static bool IsStdioConfigValid(JsonNode? serverEntry)
        {
            if (serverEntry == null)
                return false;

            var command = serverEntry["command"]?.GetValue<string>();
            var args = serverEntry["args"]?.AsArray();
            var url = serverEntry["url"];

            // Must have correct command
            if (string.IsNullOrEmpty(command) || !IsCommandMatch(command!))
                return false;

            // Must have correct args
            if (!DoArgumentsMatch(args))
                return false;

            // Must NOT have HTTP properties
            if (url != null)
                return false;

            return true;
        }

        protected static bool IsCommandMatch(string command)
        {
            // Normalize both paths for comparison
            try
            {
                var normalizedCommand = Path.GetFullPath(command.Replace('/', Path.DirectorySeparatorChar));
                var normalizedTarget = Path.GetFullPath(Startup.Server.ExecutableFullPath.Replace('/', Path.DirectorySeparatorChar));
                return string.Equals(normalizedCommand, normalizedTarget, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // If normalization fails, fallback to string comparison
                return string.Equals(command, Startup.Server.ExecutableFullPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Checks if the URL in the config matches the expected MCP server URL.
        /// Compares normalized URLs to handle trailing slashes and case differences.
        /// </summary>
        protected static bool IsUrlMatch(string url)
        {
            try
            {
                var configUri = new Uri(url.TrimEnd('/'), UriKind.Absolute);
                var expectedUri = new Uri(UnityMcpPlugin.Host.TrimEnd('/'), UriKind.Absolute);

                // Compare host, port, and path
                return string.Equals(configUri.Host, expectedUri.Host, StringComparison.OrdinalIgnoreCase)
                    && configUri.Port == expectedUri.Port
                    && string.Equals(configUri.AbsolutePath.TrimEnd('/'), expectedUri.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // If URI parsing fails, fallback to string comparison
                return string.Equals(url.TrimEnd('/'), UnityMcpPlugin.Host.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
            }
        }

        protected static bool DoArgumentsMatch(JsonArray? args)
        {
            if (args == null)
                return false;

            var targetPort = UnityMcpPlugin.Port.ToString();
            var targetTimeout = UnityMcpPlugin.TimeoutMs.ToString();

            var foundPort = false;
            var foundTimeout = false;

            // Check for both positional and named argument formats
            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i]?.GetValue<string>();
                if (string.IsNullOrEmpty(arg))
                    continue;

                // Check positional format
                if (i == 0 && arg == targetPort)
                    foundPort = true;
                else if (i == 1 && arg == targetTimeout)
                    foundTimeout = true;
                else if (arg!.StartsWith($"{Consts.MCP.Server.Args.PluginTimeout}=") && arg.Substring(Consts.MCP.Server.Args.PluginTimeout.Length + 1) == targetTimeout)
                    foundTimeout = true;
                else if (arg!.StartsWith($"{Consts.MCP.Server.Args.Port}=") && arg[(Consts.MCP.Server.Args.Port.Length + 1)..] == targetPort)
                    foundPort = true;
            }

            return foundPort && foundTimeout;
        }

        protected static JsonObject? NavigateToJsonPath(JsonObject rootObj, string[] pathSegments)
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
        protected static JsonObject EnsureJsonPathExists(JsonObject rootObj, string[] pathSegments)
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