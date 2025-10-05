/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
namespace com.IvanMurzak.Unity.MCP.Common
{
    public class ConnectionConfig
    {
        public string Endpoint { get; set; } = Consts.Hub.DefaultEndpoint;

        /// <summary>
        /// Timeout in milliseconds for MCP operations. This is set at runtime via command line args or environment variables.
        /// </summary>
        public static int TimeoutMs { get; set; } = Consts.Hub.DefaultTimeoutMs;

        public override string ToString()
            => $"Endpoint: {Endpoint}, Timeout: {TimeoutMs}ms";
    }
}
