#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class ConnectionManager : IConnectionManager
    {
        public const string Version = "0.1.0";

        readonly ILogger<ConnectionManager> _logger;
        readonly ReactiveProperty<HubConnection> _hubConnection = new();
        readonly Func<string, Task<HubConnection>> _hubConnectionBuilder;

        Task<bool>? connectionTask;
        HubConnectionLogger? hubConnectionLogger;

        public HubConnectionState ConnectionState => _hubConnection.Value?.State ?? HubConnectionState.Disconnected;
        public Observable<HubConnection> HubConnection => _hubConnection;
        public string Endpoint { get; set; } = string.Empty;

        public ConnectionManager(ILogger<ConnectionManager> logger, Func<string, Task<HubConnection>> hubConnectionBuilder)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("Ctor. Version: {0}", Version);

            _hubConnectionBuilder = hubConnectionBuilder ?? throw new ArgumentNullException(nameof(hubConnectionBuilder));
        }

        public async Task InvokeAsync<TInput>(string methodName, TInput input, CancellationToken cancellationToken = default)
        {
            if (ConnectionState != HubConnectionState.Connected)
            {
                await Connect(cancellationToken);
                if (ConnectionState != HubConnectionState.Connected)
                {
                    _logger.LogError("Can't establish connection with Remote.");
                    return;
                }
            }

            await _hubConnection.Value.InvokeAsync(methodName, input, cancellationToken).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                    return;

                _logger.LogError("Failed to invoke method {0}: {1}", methodName, task.Exception?.Message);
            });
        }

        public async Task<TResult> InvokeAsync<TInput, TResult>(string methodName, TInput input, CancellationToken cancellationToken = default)
        {
            if (ConnectionState != HubConnectionState.Connected)
            {
                await Connect(cancellationToken);
                if (ConnectionState != HubConnectionState.Connected)
                {
                    _logger.LogError("Can't establish connection with Remote.");
                    return default!;
                }
            }

            return await _hubConnection.Value.InvokeAsync<TResult>(methodName, input, cancellationToken).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                    return task.Result;

                _logger.LogError("Failed to invoke method {0}: {1}", methodName, task.Exception?.Message);
                return default!;
            });
        }

        public async Task<bool> Connect(CancellationToken cancellationToken = default)
        {
            if (_hubConnection.Value == null)
            {
                var hubConnection = await _hubConnectionBuilder(Endpoint);
                if (hubConnection == null)
                {
                    _logger.LogError("Can't create connection instance. Something may be wrong with Connection Config.");
                    return false;
                }

                _hubConnection.Value = hubConnection;
                hubConnectionLogger?.Dispose();
                hubConnectionLogger = new(_logger, _hubConnection.Value);
            }

            if (ConnectionState == HubConnectionState.Connected)
                return true;

            if (connectionTask != null)
            {
                // Create a new task that waits for the existing task but can be canceled independently
                return await Task.Run(async () =>
                {
                    try
                    {
                        await connectionTask; // Wait for the existing connection task
                        return ConnectionState == HubConnectionState.Connected;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Connection task was canceled.");
                        return false;
                    }
                }, cancellationToken);
            }

            connectionTask = _hubConnection.Value.StartAsync(cancellationToken)
                .ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                        return true;

                    _logger.LogWarning("Failed to start connection: {0}", task.Exception?.Message);
                    return false;
                });
            return await connectionTask;
        }

        public Task Disconnect(CancellationToken cancellationToken = default)
        {
            if (_hubConnection.Value == null)
                return Task.CompletedTask;

            return _hubConnection.Value.StopAsync(cancellationToken);
        }

        public void Dispose()
        {
            hubConnectionLogger?.Dispose();

            if (_hubConnection.Value == null)
                return;

            _hubConnection.Value.StopAsync().Wait();
            _hubConnection.Value.DisposeAsync().AsTask().Wait();
        }

        ~ConnectionManager() => Dispose();
    }
}