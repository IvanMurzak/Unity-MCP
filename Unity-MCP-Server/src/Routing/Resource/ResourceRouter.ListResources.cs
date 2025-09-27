/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
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
        public static async ValueTask<ListResourcesResult> List(RequestContext<ListResourcesRequestParams> request, CancellationToken cancellationToken)
        {
            var mcpServerService = McpServerService.Instance;
            if (mcpServerService == null)
                return new ListResourcesResult().SetError("[Error] 'McpServerService' is null");

            var resourceRunner = mcpServerService.ResourceRunner;
            if (resourceRunner == null)
                return new ListResourcesResult().SetError($"[Error] '{nameof(resourceRunner)}' is null");

            var requestData = new RequestListResources(cursor: request?.Params?.Cursor);

            var response = await resourceRunner.RunListResources(requestData, cancellationToken: cancellationToken);
            if (response == null)
                return new ListResourcesResult().SetError("[Error] Resource is null");

            if (response.Status == ResponseStatus.Error)
                return new ListResourcesResult().SetError(response.Message ?? "[Error] Got an error during getting resources");

            if (response.Value == null)
                return new ListResourcesResult().SetError("[Error] Resource value is null");

            return new ListResourcesResult()
            {
                Resources = response.Value
                    .Where(x => x != null)
                    .Select(x => x!.ToResource())
                    .ToList() ?? new List<Resource>(),
            };
        }
    }
}
