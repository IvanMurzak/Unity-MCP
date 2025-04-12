#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public partial class McpPlugin : IMcpPlugin
    {
        public const string Version = "0.1.0";

        readonly ILogger<McpPlugin> _logger;
        readonly IRpcRouter _rpcRouter;

        public IMcpRunner McpRunner { get; private set; }
        public IRemoteServer? RemoteServer { get; private set; } = null;
        public HubConnectionState ConnectionState => _rpcRouter.ConnectionState;

        public McpPlugin(ILogger<McpPlugin> logger, IRpcRouter rpcRouter, IMcpRunner mcpRunner, IRemoteServer remoteServer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("Ctor. Version: {0}", Version);

            _rpcRouter = rpcRouter ?? throw new ArgumentNullException(nameof(rpcRouter));

            McpRunner = mcpRunner ?? throw new ArgumentNullException(nameof(mcpRunner));
            RemoteServer = remoteServer ?? throw new ArgumentNullException(nameof(remoteServer));

            if (HasInstance)
            {
                _logger.LogError("Connector already created. Use Singleton instance.");
                return;
            }

            instance = this;
        }

        public Task<bool> Connect(CancellationToken cancellationToken = default)
        {
            RemoteServer?.Connect(cancellationToken);
            return _rpcRouter.Connect(cancellationToken);
        }

        public Task Disconnect(CancellationToken cancellationToken = default)
        {
            RemoteServer?.Disconnect(cancellationToken);
            return _rpcRouter.Disconnect(cancellationToken);
        }

        public void Dispose()
        {
            RemoteServer?.Dispose();
            _rpcRouter.Dispose();
            instance = null;
        }
        ~McpPlugin() => Dispose();
    }
}