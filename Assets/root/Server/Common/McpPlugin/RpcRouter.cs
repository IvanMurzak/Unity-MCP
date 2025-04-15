#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Data;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class RpcRouter : IRpcRouter
    {
        readonly ILogger<RpcRouter> _logger;
        readonly IMcpRunner _localApp;
        readonly IConnectionManager _connectionManager;
        readonly CompositeDisposable _serverEventsDisposables = new();
        readonly IDisposable _hubConnectionDisposable;

        public ReadOnlyReactiveProperty<HubConnectionState> ConnectionState => _connectionManager.ConnectionState;
        public ReadOnlyReactiveProperty<bool> KeepConnected => _connectionManager.KeepConnected;

        public RpcRouter(ILogger<RpcRouter> logger, IConnectionManager connectionManager, IMcpRunner localApp)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("Ctor.");
            _localApp = localApp ?? throw new ArgumentNullException(nameof(localApp));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));

            _connectionManager.Endpoint = Consts.Hub.DefaultEndpoint + Consts.Hub.RemoteApp;

            _hubConnectionDisposable = connectionManager.HubConnection
                .Subscribe(SubscribeOnServerEvents);
        }

        public Task<bool> Connect(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("Connecting... (to RemoteApp: {0}).", _connectionManager.Endpoint);
            return _connectionManager.Connect(cancellationToken);
        }
        public Task Disconnect(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("Disconnecting... (to RemoteApp: {0}).", _connectionManager.Endpoint);
            return _connectionManager.Disconnect(cancellationToken);
        }

        void SubscribeOnServerEvents(HubConnection? hubConnection)
        {
            _logger.LogTrace("Clearing server events disposables.");
            _serverEventsDisposables.Clear();

            if (hubConnection == null)
                return;

            _logger.LogTrace("Subscribing to server events.");

            hubConnection.On<RequestCallTool, IResponseData<ResponseCallTool>>(Consts.RPC.Client.RunCallTool, async data =>
                {
                    _logger.LogDebug("Call Tool called.");
                    return await _localApp.RunCallTool(data);
                })
                .AddTo(_serverEventsDisposables);

            hubConnection.On<RequestListTool, IResponseData<ResponseListTool[]>>(Consts.RPC.Client.RunListTool, async data =>
                {
                    _logger.LogDebug("List Tool called.");
                    return await _localApp.RunListTool(data);
                })
                .AddTo(_serverEventsDisposables);

            hubConnection.On<RequestResourceContent, IResponseData<ResponseResourceContent[]>>(Consts.RPC.Client.RunResourceContent, async data =>
                {
                    _logger.LogDebug("Read Resource content called.");
                    return await _localApp.RunResourceContent(data);
                })
                .AddTo(_serverEventsDisposables);

            hubConnection.On<RequestListResources, IResponseData<ResponseListResource[]>>(Consts.RPC.Client.RunListResources, async data =>
                {
                    _logger.LogDebug("List Resources called.");
                    return await _localApp.RunListResources(data);
                })
                .AddTo(_serverEventsDisposables);

            hubConnection.On<RequestListResourceTemplates, IResponseData<ResponseResourceTemplate[]>>(Consts.RPC.Client.RunListResourceTemplates, async data =>
                {
                    _logger.LogDebug("List Resource Templates called.");
                    return await _localApp.RunResourceTemplates(data);
                })
                .AddTo(_serverEventsDisposables);
        }

        public void Dispose()
        {
            DisposeAsync().Wait();
        }

        public Task DisposeAsync()
        {
            _serverEventsDisposables.Dispose();
            _hubConnectionDisposable.Dispose();

            return _connectionManager.DisposeAsync();
        }
    }
}