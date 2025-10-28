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
        readonly EventAppPromptsChange _eventAppPromptsChange;
        readonly EventAppResourcesChange _eventAppResourcesChange;
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
            EventAppToolsChange eventAppToolsChange,
            EventAppPromptsChange eventAppPromptsChange,
            EventAppResourcesChange eventAppResourcesChange)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("{0} Ctor.", GetType().GetTypeShortName());
            _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
            _mcpRunner = mcpRunner ?? throw new ArgumentNullException(nameof(mcpRunner));
            _toolRunner = toolRunner ?? throw new ArgumentNullException(nameof(toolRunner));
            _promptRunner = promptRunner ?? throw new ArgumentNullException(nameof(promptRunner));
            _resourceRunner = resourceRunner ?? throw new ArgumentNullException(nameof(resourceRunner));
            _eventAppToolsChange = eventAppToolsChange ?? throw new ArgumentNullException(nameof(eventAppToolsChange));
            _eventAppPromptsChange = eventAppPromptsChange ?? throw new ArgumentNullException(nameof(eventAppPromptsChange));
            _eventAppResourcesChange = eventAppResourcesChange ?? throw new ArgumentNullException(nameof(eventAppResourcesChange));

            // if (Instance != null)
            //     throw new InvalidOperationException($"{typeof(McpServerService).Name} is already initialized.");
            Instance = this;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("{type} {method}.", GetType().GetTypeShortName(), nameof(StartAsync));
            _disposables.Clear();

            _eventAppToolsChange
                .Subscribe(data =>
                {
                    _logger.LogTrace("{type} EventAppToolsChange. ConnectionId: {connectionId}", GetType().GetTypeShortName(), data.ConnectionId);
                    OnListToolUpdated(data, cancellationToken);
                })
                .AddTo(_disposables);

            _eventAppPromptsChange
                .Subscribe(data =>
                {
                    _logger.LogTrace("{type} EventAppPromptsChange. ConnectionId: {connectionId}", GetType().GetTypeShortName(), data.ConnectionId);
                    OnListPromptsUpdated(data, cancellationToken);
                })
                .AddTo(_disposables);

            _eventAppResourcesChange
                .Subscribe(data =>
                {
                    _logger.LogTrace("{type} EventAppResourcesChange. ConnectionId: {connectionId}", GetType().GetTypeShortName(), data.ConnectionId);
                    OnListResourcesUpdated(data, cancellationToken);
                })
                .AddTo(_disposables);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("{type} {method}.", GetType().GetTypeShortName(), nameof(StopAsync));
            _disposables.Clear();
            if (Instance == this)
                Instance = null;
            return McpPlugin.StaticDisposeAsync();
        }

        async void OnListToolUpdated(EventAppToolsChange.EventData eventData, CancellationToken cancellationToken)
        {
            _logger.LogTrace("{type} {method}", GetType().GetTypeShortName(), nameof(OnListToolUpdated));
            try
            {
                await McpServer.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("{type} Error updating tools: {Message}", GetType().GetTypeShortName(), ex.Message);
            }
        }
        async void OnResourceUpdated(EventAppToolsChange.EventData eventData, CancellationToken cancellationToken)
        {
            _logger.LogTrace("{type} {method}", GetType().GetTypeShortName(), nameof(OnResourceUpdated));
            try
            {
                await McpServer.SendNotificationAsync(NotificationMethods.ResourceUpdatedNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("{type} Error updating resource: {Message}", GetType().GetTypeShortName(), ex.Message);
            }
        }
        async void OnListPromptsUpdated(EventAppPromptsChange.EventData eventData, CancellationToken cancellationToken)
        {
            _logger.LogTrace("{type} {method}", GetType().GetTypeShortName(), nameof(OnListPromptsUpdated));
            try
            {
                await McpServer.SendNotificationAsync(NotificationMethods.PromptListChangedNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("{type} Error updating prompts: {Message}", GetType().GetTypeShortName(), ex.Message);
            }
        }
        async void OnListResourcesUpdated(EventAppResourcesChange.EventData eventData, CancellationToken cancellationToken)
        {
            _logger.LogTrace("{type} {method}", GetType().GetTypeShortName(), nameof(OnListResourcesUpdated));
            try
            {
                await McpServer.SendNotificationAsync(NotificationMethods.ResourceListChangedNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("{type} Error updating resource list: {Message}", GetType().GetTypeShortName(), ex.Message);
            }
        }
    }
}
