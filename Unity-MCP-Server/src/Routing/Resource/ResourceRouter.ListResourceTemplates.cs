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
        public static async ValueTask<ListResourceTemplatesResult> ListTemplates(RequestContext<ListResourceTemplatesRequestParams> request, CancellationToken cancellationToken)
        {
            var mcpServerService = McpServerService.Instance;
            if (mcpServerService == null)
                return new ListResourceTemplatesResult().SetError("[Error] 'McpServerService' is null");

            var resourceRunner = mcpServerService.ResourceRunner;
            if (resourceRunner == null)
                return new ListResourceTemplatesResult().SetError($"[Error] '{nameof(resourceRunner)}' is null");

            var requestData = new RequestListResourceTemplates();

            var response = await resourceRunner.RunResourceTemplates(requestData, cancellationToken: cancellationToken);
            if (response == null)
                return new ListResourceTemplatesResult().SetError("[Error] Resource is null");

            if (response.Status == ResponseStatus.Error)
                return new ListResourceTemplatesResult().SetError(response.Message ?? "[Error] Got an error during getting resource templates");

            if (response.Value == null)
                return new ListResourceTemplatesResult().SetError("[Error] Resource template value is null");

            return new ListResourceTemplatesResult()
            {
                ResourceTemplates = response.Value
                    .Where(x => x != null)
                    .Select(x => x!.ToResourceTemplate())
                    .ToList()
            };

            // -------------------------------------------------------------------------------------
            // -------------------------- STATIC ---------------------------------------------------
            // -------------------------------------------------------------------------------------
            // return Task.FromResult(new ListResourceTemplatesResult()
            // {
            //     ResourceTemplates = new List<ResourceTemplate>()
            //     {
            //         new ResourceTemplate()
            //         {
            //             UriTemplate = Consts.Route.GameObject_CurrentScene,
            //             Name = "GameObject",
            //             Description = "GameObject template",
            //             MimeType = Consts.MimeType.TextPlain
            //         },
            //         new ResourceTemplate()
            //         {
            //             UriTemplate = "component://{name}",
            //             Name = "Component",
            //             Description = "Component is attachable to GameObject C# class",
            //             MimeType = Consts.MimeType.TextPlain
            //         }
            //     }
            // });
        }
    }
}
