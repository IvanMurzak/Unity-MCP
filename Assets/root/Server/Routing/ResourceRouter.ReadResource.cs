using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Data;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static partial class ResourceRouter
    {
        public static async Task<ReadResourceResult> ReadResource(RequestContext<ReadResourceRequestParams> request, CancellationToken cancellationToken)
        {
            if (request?.Params?.Uri == null)
                return new ReadResourceResult().SetError("null", "[Error] Request or Uri is null");

            var connector = Connector.Instance;
            if (connector == null)
                return new ReadResourceResult().SetError(request.Params.Uri, "[Error] Connector is null");

            var requestData = new RequestData(new RequestResourceContent(request.Params.Uri));

            var resource = await connector.Send(requestData, cancellationToken: cancellationToken);
            if (resource == null)
                return new ReadResourceResult().SetError(request.Params.Uri, "[Error] Resource is null");

            if (!resource.IsSuccess)
                return new ReadResourceResult().SetError(request.Params.Uri, resource.Message ?? "[Error] Unknown error");

            if (resource.ResourceContents == null)
                return new ReadResourceResult().SetError(request.Params.Uri, "[Error] Resource data is null");

            return new ReadResourceResult()
            {
                Contents = resource.ResourceContents
                    .Where(x => x != null)
                    .Where(x => x!.text != null || x!.blob != null)
                    .Select(x => x!.ToResourceContents())
                    .ToList()
            };
        }
    }
}