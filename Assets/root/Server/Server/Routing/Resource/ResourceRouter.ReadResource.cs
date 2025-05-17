#if !UNITY_5_3_OR_NEWER
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
        public static async ValueTask<ReadResourceResult> ReadResource(
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

            if (response.IsError)
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
#endif