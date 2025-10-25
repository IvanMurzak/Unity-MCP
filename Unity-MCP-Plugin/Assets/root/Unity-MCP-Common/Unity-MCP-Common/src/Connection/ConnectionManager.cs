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
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class ConnectionManager : IConnectionManager
    {
        readonly string _guid = Guid.NewGuid().ToString();
        readonly ILogger<ConnectionManager> _logger;
        readonly ReactiveProperty<HubConnection?> _hubConnection = new();
        readonly IHubEndpointConnectionBuilder _hubConnectionBuilder;
        readonly ReactiveProperty<HubConnectionState> _connectionState = new(HubConnectionState.Disconnected);
        readonly ReactiveProperty<bool> _continueToReconnect = new(false);
        readonly CompositeDisposable _disposables = new();

        volatile Task<bool>? connectionTask;
        HubConnectionLogger? hubConnectionLogger;
        HubConnectionObservable? hubConnectionObservable;
        CancellationTokenSource? internalCts;
        public ReadOnlyReactiveProperty<HubConnectionState> ConnectionState => _connectionState.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<HubConnection?> HubConnection => _hubConnection.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<bool> KeepConnected => _continueToReconnect.ToReadOnlyReactiveProperty();
        public string Endpoint { get; set; } = string.Empty;

        public ConnectionManager(ILogger<ConnectionManager> logger, IHubEndpointConnectionBuilder hubConnectionBuilder)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("{0} Ctor.", _guid);

            _hubConnectionBuilder = hubConnectionBuilder ?? throw new ArgumentNullException(nameof(hubConnectionBuilder));
            _hubConnection
                .Subscribe(hubConnection =>
                {
                    if (hubConnection == null)
                    {
                        _connectionState.Value = HubConnectionState.Disconnected;
                        return;
                    }

                    hubConnection.ToObservable().State
                        .Subscribe(state => _connectionState.Value = state)
                        .AddTo(_disposables);
                })
                .AddTo(_disposables);

            _connectionState
                .Where(state => state == HubConnectionState.Reconnecting && _continueToReconnect.CurrentValue)
                .Subscribe(async state =>
                {
                    _logger.LogInformation("{0} Connection state changed to Reconnecting. Initiating reconnection to: {1}", _guid, Endpoint);
                    await Connect(_disposables.ToCancellationToken());
                })
                .AddTo(_disposables);
        }

        public async Task InvokeAsync<TInput>(string methodName, TInput input, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConnection(cancellationToken))
                return;

            await ExecuteHubMethodAsync(methodName, hubConnection =>
                hubConnection.InvokeAsync(methodName, input, cancellationToken));
        }

        public async Task<TResult> InvokeAsync<TInput, TResult>(string methodName, TInput input, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConnection(cancellationToken))
                return default!;

            return await ExecuteHubMethodAsync(methodName, hubConnection =>
                hubConnection.InvokeAsync<TResult>(methodName, input, cancellationToken));
        }

        public async Task<bool> Connect(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{0} Connect.", _guid);

            if (_hubConnection.Value?.State == HubConnectionState.Connected)
            {
                _logger.LogDebug("{0} Already connected. Ignoring.", _guid);
                return true;
            }

            _continueToReconnect.Value = false;

            // Dispose the previous internal CancellationTokenSource if it exists
            CancelInternalToken(dispose: true);

            if (_hubConnection.Value != null)
                await _hubConnection.Value.StopAsync();

            _continueToReconnect.Value = true;

            internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (connectionTask != null)
            {
                _logger.LogDebug("{0} Connection task already exists. Waiting for the completion... {1}.", _guid, Endpoint);
                // Create a new task that waits for the existing task but can be canceled independently
                return await Task.Run(async () =>
                {
                    try
                    {
                        await connectionTask; // Wait for the existing connection task
                        return _hubConnection.Value?.State == HubConnectionState.Connected;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("{0} Connection task was canceled {1}.", _guid, Endpoint);
                        return false;
                    }
                }, internalCts.Token);
            }

            try
            {
                connectionTask = InternalConnect(internalCts.Token);
                return await connectionTask;
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} Error during connection: {1}\n{2}", _guid, ex.Message, ex.StackTrace);
                return false;
            }
            finally
            {
                connectionTask = null;
            }
        }

        void CancelInternalToken(bool dispose = false)
        {
            if (internalCts != null)
            {
                if (!internalCts.IsCancellationRequested)
                    internalCts.Cancel();

                if (dispose)
                {
                    internalCts.Dispose();
                    internalCts = null;
                }
            }
        }

        async Task<bool> InternalConnect(CancellationToken cancellationToken)
        {
            _logger.LogTrace("{0} InternalConnect", _guid);

            if (!await CreateHubConnectionIfNeeded(cancellationToken))
                return false;

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            return await StartConnectionLoop(cancellationToken);
        }

        public Task Disconnect(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{0} Disconnect.", _guid);
            connectionTask = null;
            _continueToReconnect.Value = false;

            // Cancel the internal token to stop any ongoing connection attempts
            CancelInternalToken(dispose: false);

            if (_hubConnection.Value == null)
                return Task.CompletedTask;

            return _hubConnection.Value.StopAsync(cancellationToken).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    _logger.LogInformation("{0} HubConnection stopped successfully.", _guid);
                }
                else if (task.Exception != null)
                {
                    _logger.LogError("{0} Error while stopping HubConnection: {1}\n{2}", _guid, task.Exception.Message, task.Exception.StackTrace);
                }
                _connectionState.Value = HubConnectionState.Disconnected;
            });
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
            _logger.LogDebug("{0} DisposeAsync.", _guid);

            if (!_continueToReconnect.IsDisposed)
                _continueToReconnect.Value = false;

            _disposables.Dispose();
            connectionTask = null;

            hubConnectionLogger?.Dispose();
            hubConnectionObservable?.Dispose();

            _connectionState.Dispose();
            _continueToReconnect.Dispose();

            CancelInternalToken(dispose: true);

            if (_hubConnection.CurrentValue != null)
            {
                try
                {
                    var tempHubConnection = _hubConnection.Value;

                    _hubConnection.Value = null;
                    _hubConnection.Dispose();

                    if (tempHubConnection != null)
                    {
                        await tempHubConnection.StopAsync()
                            .ContinueWith(task =>
                            {
                                try
                                {
                                    tempHubConnection.DisposeAsync();
                                }
                                catch { }
                            });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error during async disposal: {0}\n{1}", ex.Message, ex.StackTrace);
                }
            }

            _hubConnection.Dispose();
        }

        // New helper methods for better separation of concerns
        private async Task<bool> EnsureConnection(CancellationToken cancellationToken)
        {
            if (_hubConnection.CurrentValue?.State == HubConnectionState.Connected)
                return true;

            if (!_continueToReconnect.CurrentValue)
            {
                _logger.LogWarning("{0} Connection not available and auto-reconnect disabled for endpoint: {1}", _guid, Endpoint);
                return false;
            }

            _logger.LogDebug("{0} Connection is not established. Attempting to connect to: {1}", _guid, Endpoint);
            await Connect(cancellationToken);

            if (_hubConnection.CurrentValue?.State != HubConnectionState.Connected)
            {
                _logger.LogError("{0} Failed to establish connection to remote endpoint: {1}", _guid, Endpoint);
                return false;
            }

            return true;
        }

        private async Task ExecuteHubMethodAsync(string methodName, Func<HubConnection, Task> hubMethod)
        {
            if (_hubConnection.CurrentValue == null)
            {
                _logger.LogError("{0} HubConnection is null. Cannot invoke method '{1}' on endpoint: {2}", _guid, methodName, Endpoint);
                return;
            }

            try
            {
                await hubMethod(_hubConnection.CurrentValue);
                _logger.LogInformation("{0} Successfully invoked method '{1}' on endpoint: {2}", _guid, methodName, Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{0} Failed to invoke method '{1}' on endpoint: {2}. Error: {3}", _guid, methodName, Endpoint, ex.Message);
                throw;
            }
        }

        private async Task<TResult> ExecuteHubMethodAsync<TResult>(string methodName, Func<HubConnection, Task<TResult>> hubMethod)
        {
            if (_hubConnection.CurrentValue == null)
            {
                _logger.LogError("{0} HubConnection is null. Cannot invoke method '{1}' on endpoint: {2}", _guid, methodName, Endpoint);
                return default!;
            }

            try
            {
                var result = await hubMethod(_hubConnection.CurrentValue);
                _logger.LogInformation("{0} Successfully invoked method '{1}' on endpoint: {2}", _guid, methodName, Endpoint);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{0} Failed to invoke method '{1}' on endpoint: {2}. Error: {3}", _guid, methodName, Endpoint, ex.Message);
                return default!;
            }
        }

        private async Task<bool> CreateHubConnectionIfNeeded(CancellationToken cancellationToken)
        {
            if (_hubConnection.Value != null)
                return true;

            hubConnectionLogger?.Dispose();
            hubConnectionObservable?.Dispose();

            _logger.LogDebug("{0} Creating new HubConnection instance for endpoint: {1}", _guid, Endpoint);

            var hubConnection = await _hubConnectionBuilder.CreateConnectionAsync(Endpoint);
            if (hubConnection == null)
            {
                _logger.LogError("{0} Failed to create HubConnection instance. Check connection configuration for endpoint: {1}", _guid, Endpoint);
                return false;
            }

            _logger.LogDebug("{0} Successfully created HubConnection instance for endpoint: {1}", _guid, Endpoint);
            _hubConnection.Value = hubConnection;

            SetupHubConnectionLogging(hubConnection);
            SetupHubConnectionObservables(hubConnection, cancellationToken);

            return true;
        }

        private void SetupHubConnectionLogging(HubConnection hubConnection)
        {
            hubConnectionLogger = new(_logger, hubConnection, guid: _guid);
        }

        private void SetupHubConnectionObservables(HubConnection hubConnection, CancellationToken cancellationToken)
        {
            hubConnectionObservable = new(hubConnection);

            hubConnectionObservable.Closed
                .Subscribe(_ => connectionTask = null)
                .RegisterTo(cancellationToken);

            hubConnectionObservable.Closed
                .Where(_ => _continueToReconnect.CurrentValue)
                .Where(_ => !cancellationToken.IsCancellationRequested)
                .Subscribe(async _ =>
                {
                    _logger.LogWarning("{0} Connection closed unexpectedly. Attempting to reconnect to: {1}", _guid, Endpoint);
                    await InternalConnect(cancellationToken);
                })
                .RegisterTo(cancellationToken);
        }

        private async Task<bool> StartConnectionLoop(CancellationToken cancellationToken)
        {
            _logger.LogDebug("{0} Starting connection loop for endpoint: {1}", _guid, Endpoint);

            while (_continueToReconnect.CurrentValue && !cancellationToken.IsCancellationRequested)
            {
                if (await AttemptConnection(cancellationToken))
                    return true;

                await WaitBeforeRetry(cancellationToken);
            }

            _logger.LogWarning("{0} Connection loop terminated for endpoint: {1}", _guid, Endpoint);
            return false;
        }

        private async Task<bool> AttemptConnection(CancellationToken cancellationToken)
        {
            var connection = _hubConnection.CurrentValue;
            if (connection == null)
                return false;

            _logger.LogInformation("{0} Starting connection attempt to: {1}", _guid, Endpoint);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var connectionTask = connection.StartAsync(cts.Token);

            try
            {
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                var completedTask = await Task.WhenAny(connectionTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("{0} Connection attempt timed out after 30 seconds for endpoint: {1}", _guid, Endpoint);
                    return false;
                }

                if (connectionTask.IsCompletedSuccessfully)
                {
                    _logger.LogInformation("{0} Connection established successfully to: {1}", _guid, Endpoint);
                    _connectionState.Value = HubConnectionState.Connected;
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{0} Connection attempt failed for endpoint: {1}. Error: {2}", _guid, Endpoint, ex.Message);
            }

            return false;
        }

        private async Task WaitBeforeRetry(CancellationToken cancellationToken)
        {
            if (_continueToReconnect.CurrentValue && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("{0} Waiting 5 seconds before retry for endpoint: {1}", _guid, Endpoint);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        ~ConnectionManager() => Dispose();
    }
}
