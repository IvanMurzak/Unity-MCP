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
        public static async ValueTask<ListResourceTemplatesResult> ListResourceTemplates(RequestContext<ListResourceTemplatesRequestParams> request, CancellationToken cancellationToken)
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

            if (response.IsError)
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
#endif