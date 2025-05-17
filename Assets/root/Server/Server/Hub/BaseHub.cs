#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#if !UNITY_5_3_OR_NEWER
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
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
            _logger.LogTrace("{0} Ctor.", _guid);
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        public override Task OnConnectedAsync()
        {
            ClientUtils.AddClient(GetType(), Context.ConnectionId, _logger);
            // DisconnectOtherClients(clients, currentConnectionId: Context.ConnectionId);
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
                    _logger.LogInformation("{0} Client '{1}' removed from connected clients for {2}.", _guid, connectionId, GetType().Name);
                    var client = Clients.Client(connectionId);
                    client.SendAsync(Consts.RPC.Client.ForceDisconnect);
                }
                else
                {
                    _logger.LogWarning("{0} Client '{1}' was not found in connected clients for {2}.", _guid, connectionId, GetType().Name);
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
#endif