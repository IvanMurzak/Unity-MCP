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
using com.IvanMurzak.Unity.MCP.Common.SignalR;
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

        public Task<IResponseData> OnListToolsUpdated(string data)
        {
            _logger.LogTrace("OnListToolsUpdated. {0}. Data: {1}", _guid, data);
            _eventAppToolsChange.OnNext(new EventAppToolsChange.EventData
            {
                ConnectionId = Context.ConnectionId,
                Data = data
            });
            return ResponseData.Success(data, string.Empty).TaskFromResult<IResponseData>();
        }

        public Task<IResponseData> OnListResourcesUpdated(string data)
        {
            _logger.LogTrace("OnListResourcesUpdated. {0}. Data: {1}", _guid, data);
            return ResponseData.Success(data, string.Empty).TaskFromResult<IResponseData>();
        }

        public Task<IResponseData> OnToolRequestCompleted(ToolRequestCompletedData data)
        {
            _logger.LogTrace("OnToolRequestCompleted. {0}. RequestId: {1}", _guid, data.RequestId);

            try
            {
                _requestTrackingService.CompleteRequest(data.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing tool response for RequestId: {RequestId}", data.RequestId);
            }

            return ResponseData.Success(string.Empty, string.Empty).TaskFromResult<IResponseData>();
        }

        public Task<VersionHandshakeResponse> OnVersionHandshake(VersionHandshakeRequest request)
        {
            _logger.LogTrace("OnVersionHandshake. {0}. PluginVersion: {1}, ApiVersion: {2}, UnityVersion: {3}", _guid, request.PluginVersion, request.ApiVersion, request.UnityVersion);

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
                _logger.LogError("API version mismatch detected. Plugin: {PluginApiVersion}, Server: {ServerApiVersion}",
                    request.ApiVersion, serverApiVersion);
            }
            else
            {
                _logger.LogInformation("Version handshake successful. Plugin: {PluginVersion}, API: {ApiVersion}, Unity Version: {UnityVersion}",
                    request.PluginVersion, request.ApiVersion, request.UnityVersion);
            }

            return Task.FromResult(response);
        }

        public Task<IResponseData> OnDomainReloadStarted()
        {
            _logger.LogTrace("OnDomainReloadStarted. {0}", _guid);
            return ResponseData.Success(string.Empty, string.Empty).TaskFromResult<IResponseData>();
        }

        public Task<IResponseData> OnDomainReloadCompleted()
        {
            _logger.LogTrace("OnDomainReloadCompleted. {0}", _guid);
            return ResponseData.Success(string.Empty, string.Empty).TaskFromResult<IResponseData>();
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
