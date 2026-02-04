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
    /// <summary>
    /// Configuration for AI agents that use command array format where all arguments
    /// are included in the "command" array instead of a separate "args" array.
    /// Format: { "command": ["executable", "arg1", "arg2", ...] }
    /// </summary>
    public class JsonCommandAiAgentConfig : JsonAiAgentConfig
    {
        public override string ExpectedFileContent
        {
            get
            {
                var pathSegments = Consts.MCP.Server.BodyPathSegments(BodyPath);

                // Start with the innermost content
                var innerContent = new JsonObject
                {
                    [DefaultMcpServerName] = CreateServerConfigObject()
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

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonCommandAiAgentConfig"/> class.
        /// </summary>
        /// <param name="name">The display name of the AI agent.</param>
        /// <param name="configPath">The path to the configuration file.</param>
        /// <param name="bodyPath">The JSON path to the MCP servers section.</param>
        public JsonCommandAiAgentConfig(
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

        public override bool Configure() => ConfigureCommandArrayMcpClient(ConfigPath, BodyPath);
        public override bool IsConfigured() => IsCommandArrayMcpClientConfigured(ConfigPath, BodyPath);

        /// <summary>
        /// Creates the server configuration object with command array format.
        /// </summary>
        private JsonObject CreateServerConfigObject()
        {
            return new JsonObject
            {
                ["type"] = "local",
                ["enabled"] = true,
                ["command"] = CreateCommandArray()
            };
        }

        /// <summary>
        /// Creates the command array containing executable path and all arguments.
        /// </summary>
        private static JsonArray CreateCommandArray()
        {
            return new JsonArray
            {
                Startup.Server.ExecutableFullPath.Replace('\\', '/'),
                $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
                $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
                $"{Consts.MCP.Server.Args.ClientTransportMethod}=stdio"
            };
        }

        /// <summary>
        /// Configures the MCP client using command array format.
        /// </summary>
        private bool ConfigureCommandArrayMcpClient(string configPath, string bodyPath)
        {
            if (string.IsNullOrEmpty(configPath))
                return false;

            Debug.Log($"{Consts.Log.Tag} Configuring MCP client (command array format) with path: {configPath} and bodyPath: {bodyPath}");

            try
            {
                var pathSegments = Consts.MCP.Server.BodyPathSegments(bodyPath);

                if (!File.Exists(configPath))
                {
                    // Create all necessary directories
                    var directory = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    // Create the file with the expected content
                    File.WriteAllText(configPath, ExpectedFileContent);
                    return true;
                }

                var json = File.ReadAllText(configPath);
                JsonObject? rootObj = null;

                try
                {
                    rootObj = JsonNode.Parse(json)?.AsObject();
                    if (rootObj == null)
                        throw new Exception("Config file is not a valid JSON object.");
                }
                catch
                {
                    File.WriteAllText(configPath, ExpectedFileContent);
                    return true;
                }

                // Navigate to or create the target location in the existing JSON
                var targetObj = EnsureJsonPathExists(rootObj, pathSegments);

                // Find and remove entries with matching executable in command array
                var keysToRemove = targetObj
                    .Where(kv => IsCommandArrayExecutableMatch(kv.Value))
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                    targetObj.Remove(key);

                // Add the new configuration
                targetObj[DefaultMcpServerName] = CreateServerConfigObject();

                // Write back to file
                File.WriteAllText(configPath, rootObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                return IsCommandArrayMcpClientConfigured(configPath, bodyPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error configuring MCP client: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if the MCP client is configured using command array format.
        /// </summary>
        private bool IsCommandArrayMcpClientConfigured(string configPath, string bodyPath)
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
                var targetObj = NavigateToJsonPath(rootObj, pathSegments);
                if (targetObj == null)
                    return false;

                foreach (var kv in targetObj)
                {
                    var commandArray = kv.Value?["command"]?.AsArray();
                    if (commandArray == null || commandArray.Count == 0)
                        continue;

                    // Check if first element is the executable
                    var executableInConfig = commandArray[0]?.GetValue<string>();
                    if (string.IsNullOrEmpty(executableInConfig) || !IsCommandMatch(executableInConfig!))
                        continue;

                    // Check if arguments are present in the command array
                    return DoCommandArrayArgumentsMatch(commandArray);
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
        /// Checks if the command array contains the expected arguments.
        /// </summary>
        private static bool DoCommandArrayArgumentsMatch(JsonArray commandArray)
        {
            var targetPort = UnityMcpPlugin.Port.ToString();
            var targetTimeout = UnityMcpPlugin.TimeoutMs.ToString();

            var foundPort = false;
            var foundTimeout = false;

            // Skip first element (executable), check remaining for arguments
            for (int i = 1; i < commandArray.Count; i++)
            {
                var arg = commandArray[i]?.GetValue<string>();
                if (string.IsNullOrEmpty(arg))
                    continue;

                if (arg!.StartsWith($"{Consts.MCP.Server.Args.Port}=") &&
                    arg[(Consts.MCP.Server.Args.Port.Length + 1)..] == targetPort)
                    foundPort = true;
                else if (arg!.StartsWith($"{Consts.MCP.Server.Args.PluginTimeout}=") &&
                    arg[(Consts.MCP.Server.Args.PluginTimeout.Length + 1)..] == targetTimeout)
                    foundTimeout = true;
            }

            return foundPort && foundTimeout;
        }

        /// <summary>
        /// Checks if a server entry's command array first element matches the expected executable path.
        /// </summary>
        private static bool IsCommandArrayExecutableMatch(JsonNode? serverEntry)
        {
            var commandArray = serverEntry?["command"]?.AsArray();
            if (commandArray == null || commandArray.Count == 0)
                return false;

            var firstElement = commandArray[0]?.GetValue<string>();
            if (string.IsNullOrEmpty(firstElement))
                return false;

            return IsCommandMatch(firstElement!);
        }
    }
}
