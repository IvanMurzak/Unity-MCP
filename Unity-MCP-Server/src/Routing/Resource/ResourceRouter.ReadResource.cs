/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Model;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static partial class ResourceRouter
    {
        public static async ValueTask<ReadResourceResult> Read(
            RequestContext<ReadResourceRequestParams> request,
            CancellationToken cancellationToken
        )
        {
            if (request?.Params?.Uri == null)
                return new ReadResourceResult().SetError("null", "[Error] Request or Uri is null");

            var mcpServerService = McpServerService.Instance;
            if (mcpServerService == null)
                return new ReadResourceResult().SetError(request.Params.Uri, "[Error] 'McpServerService' is null");

            var resourceRunner = mcpServerService.ResourceRunner;
            if (resourceRunner == null)
                return new ReadResourceResult().SetError(request.Params.Uri, $"[Error] '{nameof(resourceRunner)}' is null");

            var requestData = new RequestResourceContent(request.Params.Uri);

            var response = await resourceRunner.RunResourceContent(requestData, cancellationToken: cancellationToken);
            if (response == null)
                return new ReadResourceResult().SetError(request.Params.Uri, "[Error] Resource is null");

            if (response.Status == ResponseStatus.Error)
                return new ReadResourceResult().SetError(request.Params.Uri, response.Message ?? "[Error] Got an error during reading resources");

            if (response.Value == null)
                return new ReadResourceResult().SetError(request.Params.Uri, "[Error] Resource value is null");

            return new ReadResourceResult()
            {
                Contents = response.Value
                    .Where(x => x != null)
                    .Where(x => x!.text != null || x!.blob != null)
                    .Select(x => x!.ToResourceContents())
                    .ToList()
            };
        }
    }
}
