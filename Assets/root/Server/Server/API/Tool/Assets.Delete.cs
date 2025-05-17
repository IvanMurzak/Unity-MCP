#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Assets
    {
        [McpServerTool
        (
            Name = "Assets_Delete",
            Title = "Assets Delete"
        )]
        [Description(@"Delete the assets at paths from the project. Does AssetDatabase.Refresh() at the end.")]
        public Task<CallToolResponse> Delete
        (
            [Description("The paths of the assets")]
            string[] paths
        )
        {
            return ToolRouter.Call("Assets_Delete", arguments =>
            {
                arguments[nameof(paths)] = paths ?? new string[0];
            });
        }
    }
}
#endif