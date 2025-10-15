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
using Microsoft.AspNetCore.SignalR.Client;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public interface IRpcRouter : IDisposableAsync
    {
        ReadOnlyReactiveProperty<bool> KeepConnected { get; }
        ReadOnlyReactiveProperty<HubConnectionState> ConnectionState { get; }
        Task<bool> Connect(CancellationToken cancellationToken = default);
        Task Disconnect(CancellationToken cancellationToken = default);

        Task<ResponseData> NotifyAboutUpdatedTools(CancellationToken cancellationToken = default);
        Task<ResponseData> NotifyAboutUpdatedResources(CancellationToken cancellationToken = default);
        Task<ResponseData> NotifyToolRequestCompleted(ResponseCallTool response, CancellationToken cancellationToken = default);
        Task<VersionHandshakeResponse?> PerformVersionHandshake(CancellationToken cancellationToken = default);
    }
}
