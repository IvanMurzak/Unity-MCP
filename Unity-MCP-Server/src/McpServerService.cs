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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using R3;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class McpServerService : IHostedService
    {
        readonly ILogger<McpServerService> _logger;
        readonly IMcpServer _mcpServer;
        readonly IMcpRunner _mcpRunner;
        readonly IToolRunner _toolRunner;
        readonly IPromptRunner _promptRunner;
        readonly IResourceRunner _resourceRunner;
        readonly EventAppToolsChange _eventAppToolsChange;
        readonly CompositeDisposable _disposables = new();

        public IMcpServer McpServer => _mcpServer;
        public IMcpRunner McpRunner => _mcpRunner;
        public IToolRunner ToolRunner => _toolRunner;
        public IPromptRunner PromptRunner => _promptRunner;
        public IResourceRunner ResourceRunner => _resourceRunner;

        public static McpServerService? Instance { get; private set; }

        public McpServerService(
            ILogger<McpServerService> logger,
            IMcpServer mcpServer,
            IMcpRunner mcpRunner,
            IToolRunner toolRunner,
            IPromptRunner promptRunner,
            IResourceRunner resourceRunner,
            EventAppToolsChange eventAppToolsChange)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("{0} Ctor.", GetType().GetTypeShortName());
            _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
            _mcpRunner = mcpRunner ?? throw new ArgumentNullException(nameof(mcpRunner));
            _toolRunner = toolRunner ?? throw new ArgumentNullException(nameof(toolRunner));
            _promptRunner = promptRunner ?? throw new ArgumentNullException(nameof(promptRunner));
            _resourceRunner = resourceRunner ?? throw new ArgumentNullException(nameof(resourceRunner));
            _eventAppToolsChange = eventAppToolsChange ?? throw new ArgumentNullException(nameof(eventAppToolsChange));

            // if (Instance != null)
            //     throw new InvalidOperationException($"{typeof(McpServerService).Name} is already initialized.");
            Instance = this;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("{0} StartAsync.", GetType().GetTypeShortName());
            _disposables.Clear();

            _eventAppToolsChange
                .Subscribe(data => OnListToolUpdated(data, cancellationToken))
                .AddTo(_disposables);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("{0} StopAsync.", GetType().GetTypeShortName());
            _disposables.Clear();
            if (Instance == this)
                Instance = null;
            return McpPlugin.StaticDisposeAsync();
        }

        async void OnListToolUpdated(EventAppToolsChange.EventData eventData, CancellationToken cancellationToken)
        {
            _logger.LogTrace("{0} OnListToolUpdated", GetType().GetTypeShortName());
            try
            {
                await McpServer.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} Error updating tools: {Message}", GetType().GetTypeShortName(), ex.Message);
            }
        }
    }
}
