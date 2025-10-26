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
    internal class JsonClientConfig : ClientConfig
    {
        public JsonClientConfig(string name, string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
            : base(name, configPath, bodyPath)
        {
        }

        public override bool Configure()
        {
            return McpClientUtils.ConfigureJsonMcpClient(ConfigPath, BodyPath);
        }
    }
}