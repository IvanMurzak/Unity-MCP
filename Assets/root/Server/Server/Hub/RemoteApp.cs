#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
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
    public class RemoteApp : BaseHub<RemoteApp>, IRemoteApp
    {

        public RemoteApp(ILogger<RemoteApp> logger, IHubContext<RemoteApp> hubContext) : base(logger, hubContext)
        {
        }

        public async Task<IResponseData<ResponseCallTool>> RunCallTool(IRequestCallTool data, CancellationToken cancellationToken = default)
        {
            if (data == null)
                return ResponseData<ResponseCallTool>.Error(Consts.Guid.Zero, "Tool data is null.")
                    .Log(_logger);

            if (string.IsNullOrEmpty(data.Name))
                return ResponseData<ResponseCallTool>.Error(data.RequestID, "Tool.Name is null.")
                    .Log(_logger);
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    var message = data.Arguments == null
                        ? $"Run tool '{data.Name}' with no parameters."
                        : $"Run tool '{data.Name}' with parameters[{data.Arguments.Count}]:\n{string.Join(",\n", data.Arguments)}";
                    _logger.LogInformation(message);
                }

                var client = GetActiveClient();
                if (client == null)
                    return ResponseData<ResponseCallTool>.Error(data.RequestID, $"No connected clients for {GetType().Name}.")
                        .Log(_logger);

                var result = await client.InvokeAsync<ResponseData<ResponseCallTool>>(Consts.RPC.Client.RunCallTool, data, cancellationToken);
                if (result == null)
                    return ResponseData<ResponseCallTool>.Error(data.RequestID, $"Tool '{data.Name}' returned null result.")
                        .Log(_logger);

                return result;
            }
            catch (Exception ex)
            {
                return ResponseData<ResponseCallTool>.Error(data.RequestID, $"Failed to run tool '{data.Name}'. Exception: {ex}")
                    .Log(_logger, ex);
            }
        }

        public async Task<IResponseData<ResponseListTool[]>> RunListTool(IRequestListTool data, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetActiveClient();
                if (client == null)
                    return ResponseData<ResponseListTool[]>.Error(data.RequestID, $"No connected clients for {GetType().Name}.")
                        .Log(_logger);

                var result = await client.InvokeAsync<ResponseData<ResponseListTool[]>>(Consts.RPC.Client.RunListTool, data, cancellationToken);
                if (result == null)
                    return ResponseData<ResponseListTool[]>.Error(data.RequestID, $"'{Consts.RPC.Client.RunListTool}' returned null result.")
                        .Log(_logger);

                return result;
            }
            catch (Exception ex)
            {
                return ResponseData<ResponseListTool[]>.Error(data.RequestID, $"Failed to run '{Consts.RPC.Client.RunListTool}'. Exception: {ex}")
                    .Log(_logger, ex);
            }
        }

        public async Task<IResponseData<ResponseResourceContent[]>> RunResourceContent(IRequestResourceContent data, CancellationToken cancellationToken = default)
        {
            if (data == null)
                return ResponseData<ResponseResourceContent[]>.Error(Consts.Guid.Zero, "Resource content data is null.")
                    .Log(_logger);

            if (string.IsNullOrEmpty(data.Uri))
                return ResponseData<ResponseResourceContent[]>.Error(data.RequestID, "Resource content Uri is null.")
                    .Log(_logger);

            try
            {
                var client = GetActiveClient();
                if (client == null)
                    return ResponseData<ResponseResourceContent[]>.Error(data.RequestID, $"No connected clients for {GetType().Name}.")
                        .Log(_logger);

                var result = await client.InvokeAsync<ResponseData<ResponseResourceContent[]>>(Consts.RPC.Client.RunResourceContent, data, cancellationToken);
                if (result == null)
                    return ResponseData<ResponseResourceContent[]>.Error(data.RequestID, $"Resource uri: '{data.Uri}' returned null result.")
                        .Log(_logger);

                return result;
            }
            catch (Exception ex)
            {
                return ResponseData<ResponseResourceContent[]>.Error(data.RequestID, $"Failed to get resource uri: '{data.Uri}'. Exception: {ex}")
                    .Log(_logger, ex);
            }
        }

        public async Task<IResponseData<ResponseListResource[]>> RunListResources(IRequestListResources data, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetActiveClient();
                if (client == null)
                    return ResponseData<ResponseListResource[]>.Error(data.RequestID, $"No connected clients for {GetType().Name}.")
                        .Log(_logger);

                var result = await client.InvokeAsync<ResponseData<ResponseListResource[]>>(Consts.RPC.Client.RunListResources, data, cancellationToken);
                if (result == null)
                    return ResponseData<ResponseListResource[]>.Error(data.RequestID, $"'{Consts.RPC.Client.RunListResources}' returned null result.")
                        .Log(_logger);

                return result;
            }
            catch (Exception ex)
            {
                return ResponseData<ResponseListResource[]>.Error(data.RequestID, $"Failed to run '{Consts.RPC.Client.RunListResources}'. Exception: {ex}")
                    .Log(_logger, ex);
            }
        }

        public async Task<IResponseData<ResponseResourceTemplate[]>> RunResourceTemplates(IRequestListResourceTemplates data, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetActiveClient();
                if (client == null)
                    return ResponseData<ResponseResourceTemplate[]>.Error(data.RequestID, $"No connected clients for {GetType().Name}.")
                        .Log(_logger);

                var result = await client.InvokeAsync<ResponseData<ResponseResourceTemplate[]>>(Consts.RPC.Client.RunListResourceTemplates, data, cancellationToken);
                if (result == null)
                    return ResponseData<ResponseResourceTemplate[]>.Error(data.RequestID, $"'{Consts.RPC.Client.RunListResourceTemplates}' returned null result.")
                        .Log(_logger);

                return result;
            }
            catch (Exception ex)
            {
                return ResponseData<ResponseResourceTemplate[]>.Error(data.RequestID, $"Failed to run '{Consts.RPC.Client.RunListResourceTemplates}'. Exception: {ex}")
                    .Log(_logger, ex);
            }
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}