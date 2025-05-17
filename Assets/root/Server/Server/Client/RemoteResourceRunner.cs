#if !UNITY_5_3_OR_NEWER
using System;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class RemoteResourceRunner : IResourceRunner, IDisposable
    {
        readonly ILogger<RemoteToolRunner> _logger;
        readonly IHubContext<RemoteApp> _remoteAppContext;
        readonly CancellationTokenSource cts = new();
        readonly CompositeDisposable _disposables = new();

        public RemoteResourceRunner(ILogger<RemoteToolRunner> logger, IHubContext<RemoteApp> remoteAppContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("Ctor.");
            _remoteAppContext = remoteAppContext ?? throw new ArgumentNullException(nameof(remoteAppContext));
        }

        public Task<IResponseData<ResponseResourceContent[]>> RunResourceContent(IRequestResourceContent requestData, CancellationToken cancellationToken = default)
            => ClientUtils.InvokeAsync<IRequestResourceContent, ResponseResourceContent[], RemoteApp>(
                logger: _logger,
                hubContext: _remoteAppContext,
                methodName: Consts.RPC.Client.RunResourceContent,
                requestData: requestData,
                cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token)
                .ContinueWith(task =>
            {
                var response = task.Result;
                if (response.IsError)
                    return ResponseData<ResponseResourceContent[]>.Error(requestData.RequestID, response.Message ?? "Got an error during invoking tool");

                return response;
            }, cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token);

        public Task<IResponseData<ResponseListResource[]>> RunListResources(IRequestListResources requestData, CancellationToken cancellationToken = default)
            => ClientUtils.InvokeAsync<IRequestListResources, ResponseListResource[], RemoteApp>(
                logger: _logger,
                hubContext: _remoteAppContext,
                methodName: Consts.RPC.Client.RunListResources,
                requestData: requestData,
                cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token)
                .ContinueWith(task =>
            {
                var response = task.Result;
                if (response.IsError)
                    return ResponseData<ResponseListResource[]>.Error(requestData.RequestID, response.Message ?? "Got an error during invoking tool");

                return response;
            }, cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token);

        public Task<IResponseData<ResponseResourceTemplate[]>> RunResourceTemplates(IRequestListResourceTemplates requestData, CancellationToken cancellationToken = default)
            => ClientUtils.InvokeAsync<IRequestListResourceTemplates, ResponseResourceTemplate[], RemoteApp>(
                logger: _logger,
                hubContext: _remoteAppContext,
                methodName: Consts.RPC.Client.RunListResourceTemplates,
                requestData: requestData,
                cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token)
                .ContinueWith(task =>
            {
                var response = task.Result;
                if (response.IsError)
                    return ResponseData<ResponseResourceTemplate[]>.Error(requestData.RequestID, response.Message ?? "Got an error during invoking tool");

                return response;
            }, cancellationToken: CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token);

        public void Dispose()
        {
            _logger.LogTrace("Dispose.");
            _disposables.Dispose();

            if (!cts.IsCancellationRequested)
                cts.Cancel();

            cts.Dispose();
        }
    }
}
#endif