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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public partial class McpPlugin : IMcpPlugin
    {
        readonly ILogger<McpPlugin> _logger;
        readonly IRpcRouter? _rpcRouter;
        readonly CompositeDisposable _disposables = new();

        public ILogger Logger => _logger;
        public IMcpRunner McpRunner { get; private set; }
        public IRpcRouter? RpcRouter => _rpcRouter;
        public ReadOnlyReactiveProperty<HubConnectionState> ConnectionState => _rpcRouter?.ConnectionState
            ?? new ReactiveProperty<HubConnectionState>(HubConnectionState.Disconnected);
        public ReadOnlyReactiveProperty<bool> KeepConnected => _rpcRouter?.KeepConnected
            ?? new ReactiveProperty<bool>(false);

        public McpPlugin(ILogger<McpPlugin> logger, IMcpRunner mcpRunner, IRpcRouter? rpcRouter = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("{0} Ctor.", typeof(McpPlugin).Name);

            McpRunner = mcpRunner ?? throw new ArgumentNullException(nameof(mcpRunner));

            var cancellationToken = _disposables.ToCancellationToken();

            _rpcRouter = rpcRouter;
            _rpcRouter?.ConnectionState
                .Where(state => state == HubConnectionState.Connected)
                .Where(state => !cancellationToken.IsCancellationRequested)
                .Subscribe(async state =>
                {
                    _logger.LogDebug("{class}.{method}, connection state: {2}",
                        nameof(McpPlugin),
                        nameof(ConnectionState),
                        state);

                    // Perform version handshake first
                    var handshakeResponse = await _rpcRouter.PerformVersionHandshake(cancellationToken);
                    if (handshakeResponse != null && !handshakeResponse.Compatible)
                    {
                        LogVersionMismatchError(handshakeResponse);
                        // Still proceed with tool notification for now, but user will see the error
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var tasks = Enumerable.Empty<Task>();

                    if (mcpRunner.EnabledToolsCount > 0)
                        tasks.Append(_rpcRouter.NotifyAboutUpdatedTools(cancellationToken));

                    if (mcpRunner.EnabledPromptsCount > 0)
                        tasks.Append(_rpcRouter.NotifyAboutUpdatedPrompts(cancellationToken));

                    if (mcpRunner.EnabledResourcesCount > 0)
                        tasks.Append(_rpcRouter.NotifyAboutUpdatedResources(cancellationToken));

                    await Task.WhenAll(tasks);

                    _logger.LogDebug("{class}.{method}, initial notifications sent.",
                        nameof(McpPlugin),
                        nameof(ConnectionState));
                })
                .AddTo(_disposables);

            McpRunner.OnToolsUpdated
                .Subscribe(async _ =>
                {
                    _logger.LogDebug("{class}.{method}, tools updated event received.",
                        nameof(McpPlugin),
                        nameof(McpRunner.OnToolsUpdated));

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (_rpcRouter == null)
                    {
                        _logger.LogWarning("{class}.{method}, RPC Router is not initialized, cannot notify about updated tools.",
                            nameof(McpPlugin),
                            nameof(McpRunner.OnToolsUpdated));
                        return;
                    }

                    await _rpcRouter.NotifyAboutUpdatedTools(cancellationToken);
                })
                .AddTo(_disposables);

            McpRunner.OnPromptsUpdated
                .Subscribe(async _ =>
                {
                    _logger.LogDebug("{class}.{method}, prompts updated event received.",
                        nameof(McpPlugin),
                        nameof(McpRunner.OnPromptsUpdated));

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (_rpcRouter == null)
                    {
                        _logger.LogWarning("{class}.{method}, RPC Router is not initialized, cannot notify about updated tools.",
                            nameof(McpPlugin),
                            nameof(McpRunner.OnToolsUpdated));
                        return;
                    }

                    await _rpcRouter.NotifyAboutUpdatedPrompts(cancellationToken);
                })
                .AddTo(_disposables);

            McpRunner.OnResourcesUpdated
                .Subscribe(async _ =>
                {
                    _logger.LogDebug("{class}.{method}, resources updated event received.",
                        nameof(McpPlugin),
                        nameof(McpRunner.OnResourcesUpdated));

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (_rpcRouter == null)
                    {
                        _logger.LogWarning("{class}.{method}, RPC Router is not initialized, cannot notify about updated tools.",
                            nameof(McpPlugin),
                            nameof(McpRunner.OnToolsUpdated));
                        return;
                    }

                    await _rpcRouter.NotifyAboutUpdatedResources(cancellationToken);
                })
                .AddTo(_disposables);

            if (HasInstance)
            {
                _logger.LogError($"{nameof(McpPlugin)} already created. Use Singleton instance.");
                return;
            }

            _instance.Value = this;

            // Dispose if another instance is created, because only one instance is allowed.
            _instance
                .Where(instance => instance != this)
                .Subscribe(instance => Dispose())
                .AddTo(_disposables);
        }

        public Task<bool> Connect(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{class}.{method} called.", nameof(McpPlugin), nameof(Connect));
            if (_rpcRouter == null)
                return Task.FromResult(false);
            return _rpcRouter.Connect(cancellationToken);
        }

        public Task Disconnect(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{class}.{method} called.", nameof(McpPlugin), nameof(Disconnect));
            if (_rpcRouter == null)
                return Task.CompletedTask;
            return _rpcRouter.Disconnect(cancellationToken);
        }

        private void LogVersionMismatchError(VersionHandshakeResponse handshakeResponse)
        {
            var errorMessage = $"[Unity-MCP] API VERSION MISMATCH: {handshakeResponse.Message}";

            // Log using ILogger which will be connected to Unity's logging system from the outside
            _logger.LogError(errorMessage);
        }

        public void Dispose()
        {
            _logger.LogInformation("{class}.{method} called.", nameof(McpPlugin), nameof(Dispose));
#pragma warning disable CS4014
            DisposeAsync();
            // DisposeAsync().Wait();
            // Unity won't reload Domain if we call DisposeAsync().Wait() here.
#pragma warning restore CS4014
        }

        public async Task DisposeAsync()
        {
            _logger.LogInformation("{class}.{method} called.", nameof(McpPlugin), nameof(DisposeAsync));

            _disposables.Dispose();

            var localInstance = _instance.CurrentValue;
            if (localInstance == this)
                _instance.Value = null;

            try
            {
                if (_rpcRouter != null)
                    await _rpcRouter.Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during async disposal: {0}\n{1}", ex.Message, ex.StackTrace);
            }

            try
            {
                if (_rpcRouter != null)
                    await _rpcRouter.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during async disposal: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        ~McpPlugin() => Dispose();
    }
}
