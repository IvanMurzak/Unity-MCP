#if !UNITY_5_3_OR_NEWER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static class ClientUtils
    {
        const int maxRetries = 10; // Maximum number of retries
        const int retryDelayMs = 1000; // Delay between retries in milliseconds

        // Thread-safe collection to store connected clients, grouped by hub type
        static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, bool>> ConnectedClients = new();
        static readonly ConcurrentDictionary<Type, string> LastSuccessfulClients = new();

        static IEnumerable<string> AllConnections => ConnectedClients.TryGetValue(typeof(RemoteApp), out var clients)
            ? clients?.Keys ?? new string[0]
            : Enumerable.Empty<string>();

        static string? GetBestConnectionId(Type type, int offset = 0)
        {
            var clients = default(ConcurrentDictionary<string, bool>);
            if (offset == 0)
            {
                if (LastSuccessfulClients.TryGetValue(type, out var connectionId))
                {
                    if (ConnectedClients.TryGetValue(type, out clients) && clients.ContainsKey(connectionId))
                    {
                        return connectionId;
                    }
                    else
                    {
                        LastSuccessfulClients.TryRemove(type, out _);
                    }
                }
            }
            if (ConnectedClients.TryGetValue(type, out clients))
            {
                var connectionIds = clients.Keys.ToList();
                if (connectionIds.Count == 0)
                    return null;
                return connectionIds[offset % connectionIds.Count];
            }
            return null;
        }
        // static string? FirstConnectionId => ConnectedClients.TryGetValue(typeof(RemoteApp), out var clients)
        //     ? clients?.FirstOrDefault().Key
        //     : null;

        public static void AddClient<T>(string connectionId, ILogger? logger)
            => AddClient(typeof(T), connectionId, logger);
        public static void AddClient(Type type, string connectionId, ILogger? logger)
        {
            var clients = ConnectedClients.GetOrAdd(type, _ => new());
            if (clients.TryAdd(connectionId, true))
            {
                logger?.LogInformation($"Client '{connectionId}' connected to {type.Name}. Total connected clients: {clients.Count}");
            }
            else
            {
                logger?.LogWarning($"Client '{connectionId}' is already connected to {type.Name}.");
            }
        }
        public static void RemoveClient<T>(string connectionId, ILogger? logger)
            => RemoveClient(typeof(T), connectionId, logger);
        public static void RemoveClient(Type type, string connectionId, ILogger? logger)
        {
            if (ConnectedClients.TryGetValue(type, out var clients))
            {
                if (clients.TryRemove(connectionId, out _))
                {
                    logger?.LogInformation($"Client '{connectionId}' disconnected from {type.Name}. Total connected clients: {clients.Count}");
                }
                else
                {
                    logger?.LogWarning($"Client '{connectionId}' was not found in connected clients for {type.Name}.");
                }
            }
            else
            {
                logger?.LogWarning($"No connected clients found for {type.Name}.");
            }
        }

        public static async Task<IResponseData<TResponse>> InvokeAsync<TRequest, TResponse, THub>(
            ILogger logger,
            IHubContext<THub> hubContext,
            string methodName,
            TRequest requestData,
            CancellationToken cancellationToken = default)
            where TRequest : IRequestID
            where THub : Hub
        {
            if (hubContext == null)
                return ResponseData<TResponse>.Error(requestData.RequestID, $"'{nameof(hubContext)}' is null.").Log(logger);

            if (string.IsNullOrEmpty(methodName))
                return ResponseData<TResponse>.Error(requestData.RequestID, $"'{nameof(methodName)}' is null.").Log(logger);

            try
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var allConnections = string.Join(", ", AllConnections);
                    logger.LogTrace("Invoke '{0}': {1}\nAvailable connections: {2}", methodName, requestData.ToString(), allConnections);
                }

                var retryCount = 0;
                while (retryCount < maxRetries)
                {
                    retryCount++;

                    var connectionId = GetBestConnectionId(typeof(RemoteApp), retryCount - 1);
                    var client = string.IsNullOrEmpty(connectionId)
                        ? null
                        : hubContext.Clients.Client(connectionId);

                    if (client == null)
                    {
                        logger.LogWarning($"No connected clients. Retrying [{retryCount}/{maxRetries}]...");
                        await Task.Delay(retryDelayMs, cancellationToken); // Wait before retrying
                        continue;
                    }

                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        var allConnections = string.Join(", ", AllConnections);
                        logger.LogTrace("Invoke '{0}', ConnectionId ='{1}'. RequestData:\n{2}\n{3}", methodName, connectionId, requestData, allConnections);
                    }
                    var invokeTask = client.InvokeAsync<ResponseData<TResponse>>(methodName, requestData, cancellationToken);
                    var completedTask = await Task.WhenAny(invokeTask, Task.Delay(TimeSpan.FromSeconds(Consts.Hub.TimeoutSeconds), cancellationToken));
                    if (completedTask == invokeTask)
                    {
                        try
                        {
                            var result = await invokeTask;
                            if (result == null)
                                return ResponseData<TResponse>.Error(requestData.RequestID, $"Invoke '{requestData}' returned null result.")
                                    .Log(logger);

                            LastSuccessfulClients[typeof(RemoteApp)] = connectionId!;
                            return result;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Error invoking '{requestData}' on client '{connectionId}': {ex.Message}");
                            // RemoveCurrentClient(client);
                            await Task.Delay(retryDelayMs, cancellationToken); // Wait before retrying
                            continue;
                        }
                    }

                    // Timeout occurred
                    logger.LogWarning($"Timeout: Client '{connectionId}' did not respond in {Consts.Hub.TimeoutSeconds} seconds. Removing from ConnectedClients.");
                    // RemoveCurrentClient(client);
                    await Task.Delay(retryDelayMs, cancellationToken); // Wait before retrying
                    // Restart the loop to try again with a new client
                }
                return ResponseData<TResponse>.Error(requestData.RequestID, $"Failed to invoke '{requestData}' after {retryCount} retries.")
                    .Log(logger);
            }
            catch (Exception ex)
            {
                return ResponseData<TResponse>.Error(requestData.RequestID, $"Failed to invoke '{requestData}'. Exception: {ex}")
                    .Log(logger, ex);
            }
        }
    }
}
#endif