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

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public class ManualAiAgentConfig : AiAgentConfig
    {
        public override string ExpectedFileContent => "";

        public ManualAiAgentConfig(string name)
            : base(name, "", "")
        {
        }

        public override bool Configure() => false;

        public override bool IsConfigured() => false;
    }
}
