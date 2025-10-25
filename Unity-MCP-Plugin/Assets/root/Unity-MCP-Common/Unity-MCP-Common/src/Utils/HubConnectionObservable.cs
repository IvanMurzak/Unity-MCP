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
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class HubConnectionObservable : IDisposable
    {
        protected readonly HubConnection _hubConnection;

        readonly Subject<Exception?> _closedSubject = new();
        readonly Subject<Exception?> _reconnectingSubject = new();
        readonly Subject<string?> _reconnectedSubject = new();

        public Observable<Exception?> Closed => _closedSubject;
        public Observable<Exception?> Reconnecting => _reconnectingSubject;
        public Observable<string?> Reconnected => _reconnectedSubject;

        public Observable<HubConnectionState> State => Observable.Merge(
            _closedSubject.Select(x => HubConnectionState.Disconnected),
            _reconnectingSubject.Select(x => HubConnectionState.Reconnecting),
            _reconnectedSubject.Select(x => HubConnectionState.Connected));

        public HubConnectionObservable(HubConnection hubConnection)
        {
            _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
            _hubConnection.Closed += OnClosedConnection;
            _hubConnection.Reconnecting += OnReconnecting;
            _hubConnection.Reconnected += OnReconnected;
        }

        Task OnClosedConnection(Exception? ex)
        {
            _closedSubject.OnNext(ex);
            return Task.CompletedTask;
        }
        Task OnReconnecting(Exception? ex)
        {
            _reconnectingSubject.OnNext(ex);
            return Task.CompletedTask;
        }
        Task OnReconnected(string? connectionId)
        {
            _reconnectedSubject.OnNext(connectionId);
            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            _hubConnection.Closed -= OnClosedConnection;
            _hubConnection.Reconnecting -= OnReconnecting;
            _hubConnection.Reconnected -= OnReconnected;

            _closedSubject.Dispose();
            _reconnectingSubject.Dispose();
            _reconnectedSubject.Dispose();
        }
    }
    public static class HubConnectionObservableExtensions
    {
        public static HubConnectionObservable ToObservable(this HubConnection hubConnection)
            => new HubConnectionObservable(hubConnection);
    }
}
