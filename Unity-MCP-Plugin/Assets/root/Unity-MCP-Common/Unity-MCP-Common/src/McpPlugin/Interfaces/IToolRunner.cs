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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Model;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public interface IToolRunner
    {
        Observable<Unit> OnToolsUpdated { get; }
        int EnabledToolsCount { get; }
        int TotalToolsCount { get; }
        bool HasTool(string name);
        bool AddTool(string name, IRunTool runner);
        bool RemoveTool(string name);
        bool IsToolEnabled(string name);
        bool SetToolEnabled(string name, bool enabled);
        Task<IResponseData<ResponseCallTool>> RunCallTool(IRequestCallTool request, CancellationToken cancellationToken = default);
        Task<IResponseData<ResponseListTool[]>> RunListTool(IRequestListTool request, CancellationToken cancellationToken = default);
    }
}
