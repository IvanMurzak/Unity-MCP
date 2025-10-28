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
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public interface IServerHub
    {
        Task<IResponseData> OnListToolsUpdated(string data);
        Task<IResponseData> OnListResourcesUpdated(string data);
        Task<IResponseData> OnToolRequestCompleted(ToolRequestCompletedData data);
    }
}
