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
using com.IvanMurzak.Unity.MCP.Common;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public abstract class ClientConfig
    {
        public string Name { get; set; }
        public string ConfigPath { get; set; }
        public string BodyPath { get; set; }

        public ClientConfig(string name, string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
        {
            Name = name;
            ConfigPath = configPath;
            BodyPath = bodyPath;
        }

        public abstract bool Configure();
        public abstract bool IsConfigured();
    }
}