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
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class HubConnectionLogger : HubConnectionObservable, IDisposable
    {
        readonly string? _guid;
        readonly ILogger _logger;
        readonly CompositeDisposable _disposables = new();

        public HubConnectionLogger(ILogger logger, HubConnection hubConnection, string? guid = null) : base(hubConnection)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _guid = guid;

            _logger.LogTrace("{0} HubConnectionLogger.Ctor.", _guid);

            Closed
                .Where(x => _logger.IsEnabled(LogLevel.Debug))
                .Subscribe(ex =>
                {
                    _logger.LogTrace("{0} HubConnectionLogger HubConnection OnClosed. Exception: {1}", _guid, ex?.Message);
                    if (ex != null)
                        _logger.LogError(ex, "{0} HubConnectionLogger Error in Closed event subscription: {1}", _guid, ex.Message);
                })
                .AddTo(_disposables);

            Reconnecting
                .Where(x => _logger.IsEnabled(LogLevel.Debug))
                .Subscribe(ex =>
                {
                    _logger.LogTrace("{0} HubConnectionLogger HubConnection OnReconnecting.", _guid);
                    if (ex != null)
                        _logger.LogError(ex, "{0} HubConnectionLogger Error during reconnecting: {1}", _guid, ex.Message);
                })
                .AddTo(_disposables);

            Reconnected
                .Where(x => _logger.IsEnabled(LogLevel.Debug))
                .Subscribe(connectionId =>
                {
                    _logger.LogTrace("{0} HubConnectionLogger HubConnection OnReconnected with id {1}.", _guid, connectionId);
                })
                .AddTo(_disposables);
        }

        public override void Dispose()
        {
            _logger.LogTrace("{0} HubConnectionLogger.Dispose.", _guid);
            base.Dispose();
            _disposables.Dispose();
        }
    }
}
