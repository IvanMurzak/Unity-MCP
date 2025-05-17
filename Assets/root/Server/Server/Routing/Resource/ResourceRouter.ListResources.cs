#if !UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Data;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static partial class ResourceRouter
    {
        public static async ValueTask<ListResourcesResult> ListResources(RequestContext<ListResourcesRequestParams> request, CancellationToken cancellationToken)
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

            if (response.IsError)
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
#endif