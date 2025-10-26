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
using com.IvanMurzak.Unity.MCP.Common;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public static class McpClientUtils
    {
        public static bool IsMcpClientConfigured(string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
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
                    var command = kv.Value?["command"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(command) || !IsCommandMatch(command!))
                        continue;

                    var args = kv.Value?["args"]?.AsArray();
                    return DoArgumentsMatch(args);
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

        static bool IsCommandMatch(string command)
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

        static bool DoArgumentsMatch(JsonArray? args)
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

                // Check named format
                else if (arg!.StartsWith($"{Consts.MCP.Server.Args.Port}=") && arg.Substring(Consts.MCP.Server.Args.Port.Length + 1) == targetPort)
                    foundPort = true;
                else if (arg!.StartsWith($"{Consts.MCP.Server.Args.PluginTimeout}=") && arg.Substring(Consts.MCP.Server.Args.PluginTimeout.Length + 1) == targetTimeout)
                    foundTimeout = true;
            }

            return foundPort && foundTimeout;
        }

        public static JsonObject? NavigateToJsonPath(JsonObject rootObj, string[] pathSegments)
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

        public static JsonObject EnsureJsonPathExists(JsonObject rootObj, string[] pathSegments)
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