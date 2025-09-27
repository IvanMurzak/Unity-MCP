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
    public class RemoteApp : BaseHub<RemoteApp>, IRemoteApp
    {
        readonly Common.Version _version;
        readonly EventAppToolsChange _eventAppToolsChange;
        readonly IRequestTrackingService _requestTrackingService;

        public RemoteApp(ILogger<RemoteApp> logger, Common.Version version, IHubContext<RemoteApp> hubContext, EventAppToolsChange eventAppToolsChange, IRequestTrackingService requestTrackingService)
            : base(logger, hubContext)
        {
            _version = version ?? throw new ArgumentNullException(nameof(version));
            _eventAppToolsChange = eventAppToolsChange ?? throw new ArgumentNullException(nameof(eventAppToolsChange));
            _requestTrackingService = requestTrackingService ?? throw new ArgumentNullException(nameof(requestTrackingService));
        }

        Task<IResponseData> IRemoteApp.OnListToolsUpdated(string data)
        {
            _logger.LogTrace("{method}. {guid}. Data: {data}",
                nameof(IRemoteApp.OnListToolsUpdated), _guid, data);

            _eventAppToolsChange.OnNext(new EventAppToolsChange.EventData
            {
                ConnectionId = Context.ConnectionId,
                Data = data
            });
            return ResponseData.Success(data, string.Empty).TaskFromResult<IResponseData>();
        }

        Task<IResponseData> IRemoteApp.OnListResourcesUpdated(string data)
        {
            _logger.LogTrace("{method}. {guid}. Data: {data}",
                nameof(IRemoteApp.OnListResourcesUpdated), _guid, data);

            return ResponseData.Success(data, string.Empty).TaskFromResult<IResponseData>();
        }

        Task<IResponseData> IRemoteApp.OnToolRequestCompleted(ToolRequestCompletedData data)
        {
            _logger.LogTrace("{method}. {guid}. RequestId: {requestId}",
                nameof(IRemoteApp.OnToolRequestCompleted), _guid, data.RequestId);

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

        Task<VersionHandshakeResponse> IRemoteApp.OnVersionHandshake(VersionHandshakeRequest request)
        {
            _logger.LogTrace("{method}. {guid}. PluginVersion: {pluginVersion}, ApiVersion: {apiVersion}, UnityVersion: {unityVersion}",
                nameof(IRemoteApp.OnVersionHandshake), _guid, request.PluginVersion, request.ApiVersion, request.UnityVersion);

            var serverApiVersion = _version.Api;
            var compatible = IsApiVersionCompatible(request.ApiVersion, serverApiVersion);

            var response = new VersionHandshakeResponse
            {
                ApiVersion = serverApiVersion,
                ServerVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                Compatible = compatible,
                Message = compatible
                    ? "API version is compatible."
                    : $"API version mismatch. Plugin: {request.ApiVersion}, Server: {serverApiVersion}. Please update to compatible versions."
            };

            if (!compatible)
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
