using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public interface IConnectionManager : IConnection, IDisposable
    {
        string Endpoint { get; set; }
        ReadOnlyReactiveProperty<HubConnection?> HubConnection { get; }
        Task InvokeAsync<TInput>(string methodName, TInput input, CancellationToken cancellationToken = default);
        Task<TResult> InvokeAsync<TInput, TResult>(string methodName, TInput input, CancellationToken cancellationToken = default);
    }
}