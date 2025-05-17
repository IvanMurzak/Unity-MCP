#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Component
    {
        [McpServerTool
        (
            Name = "Component_GetAll",
            Title = "Get list of all Components"
        )]
        [Description("Returns the list of all available components in the project.")]
        public Task<CallToolResponse> GetAll
        (
            [Description("Substring for searching components. Could be empty.")]
            string search
        )
        {
            return ToolRouter.Call("Component_GetAll", arguments =>
            {
                arguments[nameof(search)] = search;
            });
        }
    }
}
#endif