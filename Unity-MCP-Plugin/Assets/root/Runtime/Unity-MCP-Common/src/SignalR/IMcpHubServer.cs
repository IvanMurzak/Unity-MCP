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
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Common.SignalR
{
    /// <summary>
    /// Strong typed interface for server-side Hub API.
    /// These are methods that clients can call remotely on the server.
    /// </summary>
    public interface IMcpHubServer : IDisposable
    {
        /// <summary>
        /// Called by client when the list of available tools has been updated.
        /// </summary>
        /// <param name="data">Serialized list of tools data</param>
        /// <returns>Response data indicating success or failure</returns>
        Task<IResponseData> OnListToolsUpdated(string data);

        /// <summary>
        /// Called by client when the list of available resources has been updated.
        /// </summary>
        /// <param name="data">Serialized list of resources data</param>
        /// <returns>Response data indicating success or failure</returns>
        Task<IResponseData> OnListResourcesUpdated(string data);

        /// <summary>
        /// Called by client when a tool request has been completed.
        /// </summary>
        /// <param name="data">Tool request completion data</param>
        /// <returns>Response data indicating success or failure</returns>
        Task<IResponseData> OnToolRequestCompleted(ToolRequestCompletedData data);

        /// <summary>
        /// Called by client to perform version handshake with the server.
        /// </summary>
        /// <param name="request">Version handshake request containing client version info</param>
        /// <returns>Version handshake response with server compatibility info</returns>
        Task<VersionHandshakeResponse> OnVersionHandshake(VersionHandshakeRequest request);

        /// <summary>
        /// Called by client when domain reload has started (Unity-specific).
        /// </summary>
        /// <returns>Response data indicating acknowledgment</returns>
        Task<IResponseData> OnDomainReloadStarted();

        /// <summary>
        /// Called by client when domain reload has completed (Unity-specific).
        /// </summary>
        /// <returns>Response data indicating acknowledgment</returns>
        Task<IResponseData> OnDomainReloadCompleted();
    }
}