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
using com.IvanMurzak.Unity.MCP.Common.Json;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class RpcRouter : IRpcRouter
    {
        readonly ILogger<RpcRouter> _logger;
        readonly IMcpRunner _mcpRunner;
        readonly IConnectionManager _connectionManager;
        readonly CompositeDisposable _serverEventsDisposables = new();
        readonly IDisposable _hubConnectionDisposable;
        readonly Version _apiVersion;

        public ReadOnlyReactiveProperty<HubConnectionState> ConnectionState => _connectionManager.ConnectionState;
        public ReadOnlyReactiveProperty<bool> KeepConnected => _connectionManager.KeepConnected;

        public RpcRouter(ILogger<RpcRouter> logger, Version apiVersion, IConnectionManager connectionManager, IMcpRunner mcpRunner)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("{class} Ctor.", nameof(RpcRouter));
            _mcpRunner = mcpRunner ?? throw new ArgumentNullException(nameof(mcpRunner));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _apiVersion = apiVersion ?? throw new ArgumentNullException(nameof(apiVersion));

            _connectionManager.Endpoint = Consts.Hub.RemoteApp;

            _hubConnectionDisposable = connectionManager.HubConnection
                .Subscribe(SubscribeOnServerEvents);
        }

        public Task<bool> Connect(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("{class} Connecting... (to RemoteApp: {endpoint}).", nameof(RpcRouter), _connectionManager.Endpoint);
            return _connectionManager.Connect(cancellationToken);
        }
        public Task Disconnect(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("{class} Disconnecting... (to RemoteApp: {endpoint}).", nameof(RpcRouter), _connectionManager.Endpoint);
            return _connectionManager.Disconnect(cancellationToken);
        }

        void SubscribeOnServerEvents(HubConnection? hubConnection)
        {
            _logger.LogTrace("{class} Clearing server events disposables.", nameof(RpcRouter));
            _serverEventsDisposables.Clear();

            if (hubConnection == null)
                return;

            _logger.LogTrace("{class} Subscribing to server events.", nameof(RpcRouter));

            hubConnection.On(Consts.RPC.Client.ForceDisconnect, async () =>
            {
                _logger.LogDebug("{class}.{method}", nameof(RpcRouter), Consts.RPC.Client.ForceDisconnect);
                await _connectionManager.Disconnect();
            });

            // Tool events -------------------------------------------------------------

            hubConnection.On<RequestCallTool, IResponseData<ResponseCallTool>>(Consts.RPC.Client.RunCallTool, async data =>
                {
                    _logger.LogDebug("{class}.{method}", nameof(RpcRouter), Consts.RPC.Client.RunCallTool);
                    return await _mcpRunner.RunCallTool(data);
                })
                .AddTo(_serverEventsDisposables);

            hubConnection.On<RequestListTool, IResponseData<ResponseListTool[]>>(Consts.RPC.Client.RunListTool, async data =>
                {
                    _logger.LogDebug("{class}.{method}", nameof(RpcRouter), Consts.RPC.Client.RunListTool);
                    return await _mcpRunner.RunListTool(data);
                })
                .AddTo(_serverEventsDisposables);

            // Prompt events -----------------------------------------------------------

            hubConnection.On<RequestGetPrompt, IResponseData<ResponseGetPrompt>>(Consts.RPC.Client.RunGetPrompt, async data =>
                {
                    _logger.LogDebug("{class}.{method}", nameof(RpcRouter), Consts.RPC.Client.RunGetPrompt);
                    return await _mcpRunner.RunGetPrompt(data);
                })
                .AddTo(_serverEventsDisposables);

            hubConnection.On<RequestListPrompts, IResponseData<ResponseListPrompts>>(Consts.RPC.Client.RunListPrompts, async data =>
                {
                    _logger.LogDebug("{class}.{method}", nameof(RpcRouter), Consts.RPC.Client.RunListPrompts);
                    return await _mcpRunner.RunListPrompts(data);
                })
                .AddTo(_serverEventsDisposables);

            // Resource events ---------------------------------------------------------

            hubConnection.On<RequestResourceContent, IResponseData<ResponseResourceContent[]>>(Consts.RPC.Client.RunResourceContent, async data =>
                {
                    _logger.LogDebug("{class}.{method}", nameof(RpcRouter), Consts.RPC.Client.RunResourceContent);
                    return await _mcpRunner.RunResourceContent(data);
                })
                .AddTo(_serverEventsDisposables);

            hubConnection.On<RequestListResources, IResponseData<ResponseListResource[]>>(Consts.RPC.Client.RunListResources, async data =>
                {
                    _logger.LogDebug("{class}.{method}", nameof(RpcRouter), Consts.RPC.Client.RunListResources);
                    return await _mcpRunner.RunListResources(data);
                })
                .AddTo(_serverEventsDisposables);

            hubConnection.On<RequestListResourceTemplates, IResponseData<ResponseResourceTemplate[]>>(Consts.RPC.Client.RunListResourceTemplates, async data =>
                {
                    _logger.LogDebug("{class}.{method}", nameof(RpcRouter), Consts.RPC.Client.RunListResourceTemplates);
                    return await _mcpRunner.RunResourceTemplates(data);
                })
                .AddTo(_serverEventsDisposables);
        }

        public Task<ResponseData> NotifyAboutUpdatedTools(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("{class} Notify server about updated tools.", nameof(RpcRouter));
            return _connectionManager.InvokeAsync<string, ResponseData>(Consts.RPC.Server.OnListToolsUpdated, string.Empty, cancellationToken);
        }

        public Task<ResponseData> NotifyAboutUpdatedPrompts(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("{class} Notify server about updated prompts.", nameof(RpcRouter));
            return _connectionManager.InvokeAsync<string, ResponseData>(Consts.RPC.Server.OnListPromptsUpdated, string.Empty, cancellationToken);
        }

        public Task<ResponseData> NotifyAboutUpdatedResources(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("{class} Notify server about updated resources.", nameof(RpcRouter));
            return _connectionManager.InvokeAsync<string, ResponseData>(Consts.RPC.Server.OnListResourcesUpdated, string.Empty, cancellationToken);
        }

        public Task<ResponseData> NotifyToolRequestCompleted(ResponseCallTool response, CancellationToken cancellationToken = default)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("{class} Notify tool request completed for request: {RequestID}\n{Json}",
                    nameof(RpcRouter),
                    response.RequestID,
                    System.Text.Json.JsonSerializer.Serialize(response, JsonOptions.Pretty)
                );
            }
            var data = new ToolRequestCompletedData
            {
                RequestId = response.RequestID,
                Result = response
            };
            return _connectionManager.InvokeAsync<ToolRequestCompletedData, ResponseData>(Consts.RPC.Server.OnToolRequestCompleted, data, cancellationToken);
        }

        public async Task<VersionHandshakeResponse?> PerformVersionHandshake(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("{class} Performing version handshake.", nameof(RpcRouter));

            var request = new VersionHandshakeRequest
            {
                RequestID = Guid.NewGuid().ToString(),
                ApiVersion = _apiVersion.Api,
                PluginVersion = _apiVersion.Plugin,
                UnityVersion = _apiVersion.UnityVersion
            };

            try
            {
                var response = await _connectionManager.InvokeAsync<VersionHandshakeRequest, VersionHandshakeResponse>(
                    Consts.RPC.Server.OnVersionHandshake, request, cancellationToken);

                if (response == null)
                {
                    _logger.LogError("{class} Version handshake failed: No response from server.", nameof(RpcRouter));
                    return null;
                }

                _logger.LogInformation("{class} Version handshake completed. Compatible: {Compatible}, Message: {Message}",
                    nameof(RpcRouter), response.Compatible, response.Message);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{class} Version handshake failed: {Error}", nameof(RpcRouter), ex.Message);
                return null;
            }
        }

        public void Dispose()
        {
            DisposeAsync().Wait();
        }

        public Task DisposeAsync()
        {
            _logger.LogTrace("{class} DisposeAsync.", nameof(RpcRouter));
            _serverEventsDisposables.Dispose();
            _hubConnectionDisposable.Dispose();

            return _connectionManager.DisposeAsync();
        }
    }
}
