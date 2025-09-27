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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public interface IMcpPlugin : IConnection, IDisposableAsync
    {
        ILogger Logger { get; }
        IMcpRunner McpRunner { get; }
        IRpcRouter? RpcRouter { get; }
    }
    public interface IConnection : IDisposableAsync
    {
        ReadOnlyReactiveProperty<bool> KeepConnected { get; }
        ReadOnlyReactiveProperty<HubConnectionState> ConnectionState { get; }
        Task<bool> Connect(CancellationToken cancellationToken = default);
        Task Disconnect(CancellationToken cancellationToken = default);
    }

    public interface IDisposableAsync : IDisposable
    {
        Task DisposeAsync();
    }

    // -----------------------------------------------------------------

    public interface IToolRunner
    {
        Task<IResponseData<ResponseCallTool>> RunCallTool(IRequestCallTool request, CancellationToken cancellationToken = default);
        Task<IResponseData<ResponseListTool[]>> RunListTool(IRequestListTool request, CancellationToken cancellationToken = default);
    }

    public interface IPromptRunner
    {
        Task<IResponseData<ResponseGetPrompt>> RunGetPrompt(IRequestGetPrompt request, CancellationToken cancellationToken = default);
        Task<IResponseData<ResponseListPrompts>> RunListPrompts(IRequestListPrompts request, CancellationToken cancellationToken = default);
    }

    public interface IResourceRunner
    {
        Task<IResponseData<ResponseResourceContent[]>> RunResourceContent(IRequestResourceContent request, CancellationToken cancellationToken = default);
        Task<IResponseData<ResponseListResource[]>> RunListResources(IRequestListResources request, CancellationToken cancellationToken = default);
        Task<IResponseData<ResponseResourceTemplate[]>> RunResourceTemplates(IRequestListResourceTemplates request, CancellationToken cancellationToken = default);
    }

    // -----------------------------------------------------------------

    public interface IServerHub
    {
        Task<IResponseData> OnListToolsUpdated(string data);
        Task<IResponseData> OnListResourcesUpdated(string data);
        Task<IResponseData> OnToolRequestCompleted(ToolRequestCompletedData data);
    }
}
