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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class RemotePromptRunner : IPromptRunner, IDisposable
    {
        readonly ILogger _logger;
        readonly IHubContext<RemoteApp> _remoteAppContext;
        readonly IRequestTrackingService _requestTrackingService;
        readonly CancellationTokenSource cts = new();
        readonly CompositeDisposable _disposables = new();

        public RemotePromptRunner(ILogger<RemotePromptRunner> logger, IHubContext<RemoteApp> remoteAppContext, IRequestTrackingService requestTrackingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("Ctor.");
            _remoteAppContext = remoteAppContext ?? throw new ArgumentNullException(nameof(remoteAppContext));
            _requestTrackingService = requestTrackingService ?? throw new ArgumentNullException(nameof(requestTrackingService));
        }

        public async Task<IResponseData<ResponseCallTool>> RunCallTool(IRequestCallTool request, CancellationToken cancellationToken = default)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            var response = await _requestTrackingService.TrackRequestAsync(
                request.RequestID,
                async () =>
                {
                    var responseData = await ClientUtils.InvokeAsync<IRequestCallTool, ResponseCallTool, RemoteApp>(
                        logger: _logger,
                        hubContext: _remoteAppContext,
                        methodName: Consts.RPC.Client.RunCallTool,
                        request: request,
                        cancellationToken: linkedCts.Token);

                    return responseData.Value ?? ResponseCallTool.Error("Response data is null");
                },
                TimeSpan.FromMinutes(5),
                linkedCts.Token);

            // Wrap the ResponseCallTool back into IResponseData<ResponseCallTool>
            return response.Pack(request.RequestID);
        }

        public Task<IResponseData<ResponseListTool[]>> RunListTool(IRequestListTool request, CancellationToken cancellationToken = default)
            => ClientUtils.InvokeAsync<IRequestListTool, ResponseListTool[], RemoteApp>(
                logger: _logger,
                hubContext: _remoteAppContext,
                methodName: Consts.RPC.Client.RunListTool,
                request: request,
                cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token)
                .ContinueWith(task =>
            {
                var response = task.Result;
                if (response.Status == ResponseStatus.Error)
                    return ResponseData<ResponseListTool[]>.Error(request.RequestID, response.Message ?? "Got an error during listing tools");

                return response;
            }, cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token);

        public void Dispose()
        {
            _logger.LogTrace("{0} Dispose.", typeof(RemotePromptRunner).Name);
            _disposables.Dispose();

            if (!cts.IsCancellationRequested)
                cts.Cancel();

            cts.Dispose();
        }

        public async Task<IResponseData<ResponseGetPrompt>> RunGetPrompt(IRequestGetPrompt request, CancellationToken cancellationToken = default)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            var responseData = await ClientUtils.InvokeAsync<IRequestGetPrompt, ResponseGetPrompt, RemoteApp>(
                logger: _logger,
                hubContext: _remoteAppContext,
                methodName: Consts.RPC.Client.RunGetPrompt,
                request: request,
                cancellationToken: linkedCts.Token);

            if (responseData.Value != null)
                return responseData.Value.Pack(request.RequestID);

            return ResponseGetPrompt.Error("Response data is null").Pack(request.RequestID);
        }

        public Task<IResponseData<ResponseListPrompts>> RunListPrompts(IRequestListPrompts request, CancellationToken cancellationToken = default)
            => ClientUtils.InvokeAsync<IRequestListPrompts, ResponseListPrompts, RemoteApp>(
                logger: _logger,
                hubContext: _remoteAppContext,
                methodName: Consts.RPC.Client.RunListPrompts,
                request: request,
                cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token)
                .ContinueWith(task =>
            {
                var response = task.Result;
                if (response.Status == ResponseStatus.Error)
                    return ResponseData<ResponseListPrompts>.Error(request.RequestID, response.Message ?? "Got an error during listing tools");

                return response;
            }, cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token);
    }
}
