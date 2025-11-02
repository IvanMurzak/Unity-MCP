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

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin
    {
        public const string Version = "0.21.0";

        protected UnityMcpPlugin(UnityConnectionConfig? config = null)
        {
            if (config == null)
            {
                config = GetOrCreateConfig(out var wasCreated);
                this.data = config ?? throw new InvalidOperationException("ConnectionConfig is null");
                if (wasCreated)
                    Save();
            }
            else
            {
                this.data = config ?? throw new InvalidOperationException("ConnectionConfig is null");
            }
        }
    }
}
