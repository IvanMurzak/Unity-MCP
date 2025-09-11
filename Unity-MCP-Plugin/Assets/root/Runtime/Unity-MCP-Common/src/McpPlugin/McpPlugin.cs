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

            _rpcRouter = rpcRouter;
            _rpcRouter?.ConnectionState
                .Where(state => state == HubConnectionState.Connected)
                .Subscribe(async state =>
                {
                    _logger.LogDebug("{0}.{1}, connection state: {2}",
                        nameof(McpPlugin),
                        nameof(IRpcRouter.NotifyAboutUpdatedTools),
                        state);

                    var cancellationToken = _disposables.ToCancellationToken();
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    // Perform version handshake first
                    var handshakeResponse = await _rpcRouter.PerformVersionHandshake(cancellationToken);
                    if (handshakeResponse != null && !handshakeResponse.Compatible)
                    {
                        LogVersionMismatchError(handshakeResponse);
                        // Still proceed with tool notification for now, but user will see the error
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    await _rpcRouter.NotifyAboutUpdatedTools(cancellationToken);
                }).AddTo(_disposables);

            if (HasInstance)
            {
                _logger.LogError("Connector already created. Use Singleton instance.");
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
            => _rpcRouter?.Connect(cancellationToken) ?? Task.FromResult(false);

        public Task Disconnect(CancellationToken cancellationToken = default)
            => _rpcRouter?.Disconnect(cancellationToken) ?? Task.FromResult(false);

        private void LogVersionMismatchError(VersionHandshakeResponse handshakeResponse)
        {
            var errorMessage = $"[Unity-MCP] API VERSION MISMATCH: {handshakeResponse.Message}";

            // Log using ILogger which will be connected to Unity's logging system from the outside
            _logger.LogError(errorMessage);
        }

        public void Dispose()
        {
#pragma warning disable CS4014
            DisposeAsync();
            // DisposeAsync().Wait();
            // Unity won't reload Domain if we call DisposeAsync().Wait() here.
#pragma warning restore CS4014
        }

        public async Task DisposeAsync()
        {
            _disposables.Dispose();

            var localInstance = _instance.CurrentValue;
            if (localInstance == this)
                _instance.Value = null;

            try
            {
                if (_rpcRouter != null)
                {
                    await _rpcRouter.Disconnect();
                    await _rpcRouter.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during async disposal: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        ~McpPlugin() => Dispose();
    }
}
