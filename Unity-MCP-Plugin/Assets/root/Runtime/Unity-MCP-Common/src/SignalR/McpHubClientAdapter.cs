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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common.SignalR
{
    /// <summary>
    /// Adapter that implements IMcpHubClient using an IMcpRunner.
    /// This bridges the strong typed SignalR interface with the actual MCP runner implementation.
    /// </summary>
    public class McpHubClientAdapter : IMcpHubClient
    {
        readonly ILogger<McpHubClientAdapter> _logger;
        readonly IMcpRunner _mcpRunner;
        readonly IConnectionManager _connectionManager;

        public McpHubClientAdapter(ILogger<McpHubClientAdapter> logger, IMcpRunner mcpRunner, IConnectionManager connectionManager)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _mcpRunner = mcpRunner ?? throw new System.ArgumentNullException(nameof(mcpRunner));
            _connectionManager = connectionManager ?? throw new System.ArgumentNullException(nameof(connectionManager));
        }

        public async Task<IResponseData<ResponseCallTool>> RunCallTool(IRequestCallTool request, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{class}.{method}", nameof(McpHubClientAdapter), nameof(RunCallTool));
            return await _mcpRunner.RunCallTool(request, cancellationToken);
        }

        public async Task<IResponseData<ResponseListTool[]>> RunListTool(IRequestListTool request, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{class}.{method}", nameof(McpHubClientAdapter), nameof(RunListTool));
            return await _mcpRunner.RunListTool(request, cancellationToken);
        }

        public async Task<IResponseData<ResponseResourceContent[]>> RunResourceContent(IRequestResourceContent request, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{class}.{method}", nameof(McpHubClientAdapter), nameof(RunResourceContent));
            return await _mcpRunner.RunResourceContent(request, cancellationToken);
        }

        public async Task<IResponseData<ResponseListResource[]>> RunListResources(IRequestListResources request, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{class}.{method}", nameof(McpHubClientAdapter), nameof(RunListResources));
            return await _mcpRunner.RunListResources(request, cancellationToken);
        }

        public async Task<IResponseData<ResponseResourceTemplate[]>> RunListResourceTemplates(IRequestListResourceTemplates request, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{class}.{method}", nameof(McpHubClientAdapter), nameof(RunListResourceTemplates));
            return await _mcpRunner.RunResourceTemplates(request, cancellationToken);
        }

        public async Task ForceDisconnect()
        {
            _logger.LogDebug("{class}.{method}", nameof(McpHubClientAdapter), nameof(ForceDisconnect));
            await _connectionManager.Disconnect();
        }
    }
}