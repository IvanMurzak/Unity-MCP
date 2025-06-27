#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public partial class McpPlugin : IMcpPlugin
    {
        public const string Version = "0.8.3";

        readonly ILogger<McpPlugin> _logger;
        readonly IRpcRouter? _rpcRouter;
        readonly CompositeDisposable _disposables = new();


        public ILogger Logger => _logger;
        public IMcpRunner McpRunner { get; private set; }
        public ReadOnlyReactiveProperty<HubConnectionState> ConnectionState => _rpcRouter?.ConnectionState
            ?? new ReactiveProperty<HubConnectionState>(HubConnectionState.Disconnected);
        public ReadOnlyReactiveProperty<bool> KeepConnected => _rpcRouter?.KeepConnected
            ?? new ReactiveProperty<bool>(false);

        public McpPlugin(ILogger<McpPlugin> logger, IMcpRunner mcpRunner, IRpcRouter? rpcRouter = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("{0} Ctor. Version: {Version}", typeof(McpPlugin).Name, Version);

            McpRunner = mcpRunner ?? throw new ArgumentNullException(nameof(mcpRunner));

            _rpcRouter = rpcRouter;
            _rpcRouter?.ConnectionState
                .Where(state => state == HubConnectionState.Connected)
                .Subscribe(state =>
                {
                    _logger.LogDebug("{0}.{1}, connection state: {2}", nameof(McpPlugin), nameof(IRpcRouter.NotifyAboutUpdatedTools), state);
                    _rpcRouter.NotifyAboutUpdatedTools(_disposables.ToCancellationToken());
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