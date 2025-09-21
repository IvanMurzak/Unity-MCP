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

namespace com.IvanMurzak.Unity.MCP.Common.SignalR
{
    /// <summary>
    /// Strong typed interface for client-side Hub API.
    /// These are methods that server can call remotely on the client.
    /// </summary>
    public interface IMcpHubClient
    {
        /// <summary>
        /// Called by server to execute a tool on the client.
        /// </summary>
        /// <param name="request">Tool execution request</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Response containing tool execution result</returns>
        Task<IResponseData<ResponseCallTool>> RunCallTool(IRequestCallTool request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called by server to get the list of available tools from the client.
        /// </summary>
        /// <param name="request">List tools request</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Response containing array of available tools</returns>
        Task<IResponseData<ResponseListTool[]>> RunListTool(IRequestListTool request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called by server to get resource content from the client.
        /// </summary>
        /// <param name="request">Resource content request</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Response containing resource content</returns>
        Task<IResponseData<ResponseResourceContent[]>> RunResourceContent(IRequestResourceContent request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called by server to get the list of available resources from the client.
        /// </summary>
        /// <param name="request">List resources request</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Response containing array of available resources</returns>
        Task<IResponseData<ResponseListResource[]>> RunListResources(IRequestListResources request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called by server to get the list of resource templates from the client.
        /// </summary>
        /// <param name="request">List resource templates request</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Response containing array of resource templates</returns>
        Task<IResponseData<ResponseResourceTemplate[]>> RunListResourceTemplates(IRequestListResourceTemplates request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called by server to force disconnect the client.
        /// </summary>
        /// <returns>Task representing the disconnect operation</returns>
        Task ForceDisconnect();
    }
}