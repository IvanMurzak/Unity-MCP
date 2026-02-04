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
using com.IvanMurzak.McpPlugin.Common;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public abstract class AiAgentConfig
    {
        public static readonly string[] DeprecatedMcpServerNames = { "Unity-MCP" };
        public const string DefaultMcpServerName = "ai-game-developer";

        protected readonly string? _transportMethodValue;

        public string Name { get; set; }
        public string ConfigPath { get; set; }
        public string BodyPath { get; set; }
        public TransportMethod TransportMethod { get; }
        public abstract string ExpectedFileContent { get; }

        public AiAgentConfig(
            string name,
            string configPath,
            TransportMethod transportMethod,
            string? transportMethodValue = null,
            string bodyPath = Consts.MCP.Server.DefaultBodyPath)
        {
            Name = name;
            ConfigPath = configPath;
            BodyPath = bodyPath;
            TransportMethod = transportMethod;
            _transportMethodValue = transportMethodValue;
        }

        public abstract bool Configure();
        public abstract bool IsConfigured();
    }
}