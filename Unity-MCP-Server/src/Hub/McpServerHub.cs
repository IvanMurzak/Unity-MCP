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
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class McpServerHub : BaseHub<McpServerHub>, IMcpServerHub
    {
        readonly Common.Version _version;
        readonly HubEventToolsChange _eventAppToolsChange;
        readonly HubEventPromptsChange _eventAppPromptsChange;
        readonly HubEventResourcesChange _eventAppResourcesChange;
        readonly IRequestTrackingService _requestTrackingService;

        public McpServerHub(
            ILogger<McpServerHub> logger,
            Common.Version version,
            IHubContext<McpServerHub> hubContext,
            HubEventToolsChange eventAppToolsChange,
            HubEventPromptsChange eventAppPromptsChange,
            HubEventResourcesChange eventAppResourcesChange,
            IRequestTrackingService requestTrackingService)
            : base(logger, hubContext)
        {
            _version = version ?? throw new ArgumentNullException(nameof(version));
            _eventAppToolsChange = eventAppToolsChange ?? throw new ArgumentNullException(nameof(eventAppToolsChange));
            _eventAppPromptsChange = eventAppPromptsChange ?? throw new ArgumentNullException(nameof(eventAppPromptsChange));
            _eventAppResourcesChange = eventAppResourcesChange ?? throw new ArgumentNullException(nameof(eventAppResourcesChange));
            _requestTrackingService = requestTrackingService ?? throw new ArgumentNullException(nameof(requestTrackingService));
        }

        public Task<IResponseData> OnListToolsUpdated(string data)
        {
            _logger.LogTrace("{method}. {guid}. Data: {data}",
                nameof(IMcpServerHub.OnListToolsUpdated), _guid, data);

            _eventAppToolsChange.OnNext(new HubEventToolsChange.EventData
            {
                ConnectionId = Context.ConnectionId,
                Data = data
            });
            return ResponseData.Success(data, string.Empty).TaskFromResult<IResponseData>();
        }

        public Task<IResponseData> OnListPromptsUpdated(string data)
        {
            _logger.LogTrace("{method}. {guid}. Data: {data}",
                nameof(IMcpServerHub.OnListPromptsUpdated), _guid, data);

            _eventAppPromptsChange.OnNext(new HubEventPromptsChange.EventData
            {
                ConnectionId = Context.ConnectionId,
                Data = data
            });

            return ResponseData.Success(data, string.Empty).TaskFromResult<IResponseData>();
        }

        public Task<IResponseData> OnListResourcesUpdated(string data)
        {
            _logger.LogTrace("{method}. {guid}. Data: {data}",
                nameof(IMcpServerHub.OnListResourcesUpdated), _guid, data);

            _eventAppResourcesChange.OnNext(new HubEventResourcesChange.EventData
            {
                ConnectionId = Context.ConnectionId,
                Data = data
            });

            return ResponseData.Success(data, string.Empty).TaskFromResult<IResponseData>();
        }

        public Task<IResponseData> OnToolRequestCompleted(ToolRequestCompletedData data)
        {
            _logger.LogTrace("{method}. {guid}. RequestId: {requestId}",
                nameof(IMcpServerHub.OnToolRequestCompleted), _guid, data.RequestId);

            try
            {
                _requestTrackingService.CompleteRequest(data.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing tool response for RequestId: {requestId}", data.RequestId);
            }

            return ResponseData.Success(string.Empty, string.Empty).TaskFromResult<IResponseData>();
        }

        public Task<VersionHandshakeResponse> OnVersionHandshake(VersionHandshakeRequest request)
        {
            try
            {
                _logger.LogTrace("{method}. {guid}. PluginVersion: {pluginVersion}, ApiVersion: {apiVersion}, UnityVersion: {unityVersion}",
                    nameof(IMcpServerHub.OnVersionHandshake), _guid, request.PluginVersion, request.ApiVersion, request.UnityVersion);

                var serverApiVersion = _version.Api;
                var isApiVersionCompatible = IsApiVersionCompatible(request.ApiVersion, serverApiVersion);

                var response = new VersionHandshakeResponse
                {
                    ApiVersion = serverApiVersion,
                    ServerVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                    Compatible = isApiVersionCompatible,
                    Message = isApiVersionCompatible
                        ? "API version is compatible."
                        : $"API version mismatch. Plugin: {request.ApiVersion}, Server: {serverApiVersion}. Please update to compatible versions."
                };

                if (!isApiVersionCompatible)
                {
                    _logger.LogError("API version mismatch detected. Plugin: {pluginApiVersion}, Server: {serverApiVersion}",
                        request.ApiVersion, serverApiVersion);
                }
                else
                {
                    _logger.LogInformation("Version handshake successful. Plugin: {pluginVersion}, API: {apiVersion}, Unity Version: {unityVersion}",
                        request.PluginVersion, request.ApiVersion, request.UnityVersion);
                }

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during version handshake.");
                return Task.FromResult(new VersionHandshakeResponse
                {
                    ApiVersion = "Unknown",
                    ServerVersion = "Unknown",
                    Compatible = false,
                    Message = $"Error during version handshake: {ex.Message}"
                });
            }
        }

        private static bool IsApiVersionCompatible(string pluginApiVersion, string serverApiVersion)
        {
            if (string.IsNullOrEmpty(pluginApiVersion) || string.IsNullOrEmpty(serverApiVersion))
                return false;

            // For now, require exact version match. In the future, this could be enhanced
            // to support semantic versioning compatibility rules
            return pluginApiVersion.Equals(serverApiVersion, StringComparison.OrdinalIgnoreCase);
        }
    }
}
