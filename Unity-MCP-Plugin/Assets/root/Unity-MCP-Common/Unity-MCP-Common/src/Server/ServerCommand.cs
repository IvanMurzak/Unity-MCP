/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.Threading;

namespace com.IvanMurzak.Unity.MCP.Common.Server
{
    public abstract class ServerCommand<TRequest, TResponse> // : IServerCommand<TRequest, TResponse>
    {
        public virtual string Class => GetType().FullName ?? "UnknownClass";
        public virtual int Retry => 3;
        public virtual string? Method => null;

        protected CancellationTokenSource? _cancellationTokenSource { get; private set; } = new();

        // public Task<TResponse> Call(Action<TRequest> configCommand)
        //     => Call(Method, configCommand);

        // public async Task<TResponse> Call(string name, Func<Dictionary<string, JsonElement>, Dictionary<string, JsonElement>> arguments)
        // {
        //     if (_cancellationTokenSource == null)
        //         return "[Error] Command already executed. Please create a new instance of the command.";

        //     var remoteApp = McpServerService.Instance?.RemoteApp;
        //     if (remoteApp == null)
        //         return "[Error] No connection with Unity established. Please try to establish connection first and try again.";

        //     var finalMethod = method ?? Method;
        //     if (string.IsNullOrEmpty(finalMethod))
        //         return "[Error] Method name is not specified. Please specify a method name and try again.";


        //     var request = new RequestCallTool(finalMethod, arguments(new()));
        //     var r = RequestContext < CallToolRequestParams >

        //     var response = await ToolRouter.Call(request, default);

        //     try
        //     {
        //         return await send.Invoke(remoteApp, finalMethod, Retry, _cancellationTokenSource.Token);
        //     }
        //     catch (Exception ex)
        //     {
        //         return $"[Error] Failed to call remote method: {ex.Message}";
        //     }
        // }

        public virtual void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
