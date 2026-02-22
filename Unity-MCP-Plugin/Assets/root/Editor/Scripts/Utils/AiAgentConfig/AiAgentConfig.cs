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
using System.Collections.Generic;
using com.IvanMurzak.McpPlugin.Common;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public enum ValueComparisonMode { Exact, Path, Url }

    public abstract class AiAgentConfig
    {
        public static readonly string[] DeprecatedMcpServerNames = { "Unity-MCP" };
        public const string DefaultMcpServerName = "ai-game-developer";
        public static readonly string[] DefaultIdentityKeys = { "command", "url" };

        protected readonly List<string> _identityKeys = new(DefaultIdentityKeys);

        public string Name { get; set; }
        public string ConfigPath { get; set; }
        public string BodyPath { get; set; }
        public abstract string ExpectedFileContent { get; }
        public IReadOnlyList<string> IdentityKeys => _identityKeys;

        public AiAgentConfig(
            string name,
            string configPath,
            string bodyPath = Consts.MCP.Server.DefaultBodyPath)
        {
            Name = name;
            ConfigPath = configPath;
            BodyPath = bodyPath;
        }

        public AiAgentConfig AddIdentityKey(string key)
        {
            if (!_identityKeys.Contains(key))
                _identityKeys.Add(key);
            return this;
        }

        public abstract bool Configure();
        public abstract bool Unconfigure();
        public abstract bool IsDetected();
        public abstract bool IsConfigured();
    }
}