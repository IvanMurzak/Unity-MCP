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
        public static async Task<ListResourceTemplatesResult> ListResourceTemplates(RequestContext<ListResourceTemplatesRequestParams> request, CancellationToken cancellationToken)
        {
            var connector = Connector.Instance;
            if (connector == null)
                return new ListResourceTemplatesResult().SetError("[Error] Connector is null");

            var requestData = new RequestData(new RequestListResourceTemplates());

            var resource = await connector.Send(requestData, cancellationToken: cancellationToken);
            if (resource == null)
                return new ListResourceTemplatesResult().SetError("[Error] Resource is null");

            if (!resource.IsSuccess)
                return new ListResourceTemplatesResult().SetError(resource.Message ?? "[Error] Unknown error");

            if (resource.ListResourceTemplates == null)
                return new ListResourceTemplatesResult().SetError("[Error] ResourcesTemplate data is null");

            return new ListResourceTemplatesResult()
            {
                ResourceTemplates = resource.ListResourceTemplates
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