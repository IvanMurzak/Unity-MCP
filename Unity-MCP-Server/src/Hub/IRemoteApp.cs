/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using System;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public interface IRemoteApp : IToolResponseReceiver, IResourceResponseReceiver, IDisposable
    {
        Task<IResponseData> OnListToolsUpdated(string data);
        Task<IResponseData> OnListPromptsUpdated(string data);
        Task<IResponseData> OnListResourcesUpdated(string data);
        Task<IResponseData> OnToolRequestCompleted(ToolRequestCompletedData data);
        Task<VersionHandshakeResponse> OnVersionHandshake(VersionHandshakeRequest request);
    }

    public interface IToolResponseReceiver
    {
        // Task RespondOnCallTool(IResponseData<IResponseCallTool> data, CancellationToken cancellationToken = default);
        // Task RespondOnListTool(IResponseData<List<IResponseListTool>> data, CancellationToken cancellationToken = default);
    }

    public interface IResourceResponseReceiver
    {
        // Task RespondOnResourceContent(IResponseData<List<IResponseResourceContent>> data, CancellationToken cancellationToken = default);
        // Task RespondOnListResources(IResponseData<List<IResponseListResource>> data, CancellationToken cancellationToken = default);
        // Task RespondOnListResourceTemplates(IResponseData<List<IResponseResourceTemplate>> data, CancellationToken cancellationToken = default);
    }
}
