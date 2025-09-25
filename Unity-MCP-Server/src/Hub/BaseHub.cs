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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class BaseHub<T> : Hub, IDisposable where T : Hub
    {
        protected readonly ILogger _logger;
        protected readonly IHubContext<T> _hubContext;
        protected readonly CompositeDisposable _disposables = new();
        protected readonly string _guid = Guid.NewGuid().ToString();

        protected BaseHub(ILogger logger, IHubContext<T> hubContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("Ctor. {guid}", _guid);
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        public override Task OnConnectedAsync()
        {
            ClientUtils.AddClient(GetType(), Context.ConnectionId, _logger);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            ClientUtils.RemoveClient(GetType(), Context.ConnectionId, _logger);
            return base.OnDisconnectedAsync(exception);
        }

        protected virtual void DisconnectOtherClients(ConcurrentDictionary<string, bool> clients, string currentConnectionId)
        {
            if (clients.IsEmpty)
                return;

            foreach (var connectionId in clients.Keys.Where(c => c != currentConnectionId).ToList())
            {
                if (clients.TryRemove(connectionId, out _))
                {
                    _logger.LogInformation("{0} Client '{1}' removed from connected clients for {2}.", _guid, connectionId, GetType().GetTypeShortName());
                    var client = Clients.Client(connectionId);
                    client.SendAsync(SignalRMethodNames.Client.ForceDisconnect);
                }
                else
                {
                    _logger.LogWarning("{0} Client '{1}' was not found in connected clients for {2}.", _guid, connectionId, GetType().GetTypeShortName());
                }
            }
        }

        public virtual new void Dispose()
        {
            _logger.LogTrace("Dispose. {0}", _guid);
            base.Dispose();
            _disposables.Dispose();
        }
    }
}
