/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.Text.Json.Nodes;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class Consts
    {
        public static partial class MCP
        {
            public static class Plugin
            {
                public const int LinesLimit = 1000;
            }
            public static class Server
            {
                public static partial class Args
                {
                    public const string Port = "--port";
                    public const string PluginTimeout = "--plugin-timeout";
                    public const string ClientTransportMethod = "--client-transport";
                }

                public static class Env
                {
                    public const string Port = "UNITY_MCP_PORT";
                    public const string PluginTimeout = "UNITY_MCP_PLUGIN_TIMEOUT";
                    public const string ClientTransportMethod = "UNITY_MCP_CLIENT_TRANSPORT";
                }

                public const string DefaultBodyPath = "mcpServers";
                public const string DefaultServerName = "Unity-MCP";
                public const string BodyPathDelimiter = "->";

                public static string[] BodyPathSegments(string bodyPath)
                {
                    return bodyPath.Split(BodyPathDelimiter);
                }

                public static JsonNode Config(
                    string executablePath,
                    string serverName = DefaultServerName,
                    string bodyPath = DefaultBodyPath,
                    int port = Hub.DefaultPort,
                    int timeoutMs = Hub.DefaultTimeoutMs)
                {
                    var pathSegments = BodyPathSegments(bodyPath);
                    var root = new JsonObject();
                    var current = root;

                    // Create nested structure following the path segments
                    foreach (var segment in pathSegments)
                    {
                        var child = new JsonObject();
                        current[segment] = child;
                        current = child;
                    }

                    // Place the server configuration at the final location
                    current[serverName] = new JsonObject
                    {
                        ["type"] = "stdio",
                        ["command"] = executablePath,
                        ["args"] = new JsonArray
                        {
                            $"{Args.Port}={port}",
                            $"{Args.PluginTimeout}={timeoutMs}",
                            $"{Args.ClientTransportMethod}={TransportMethod.stdio}"
                        }
                    };

                    return root;
                }

                public enum TransportMethod
                {
                    unknown,
                    stdio,
                    http
                }
            }
        }
    }
}
