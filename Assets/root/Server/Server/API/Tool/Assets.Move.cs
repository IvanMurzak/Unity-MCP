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
            Name = "Assets_Move",
            Title = "Assets Move"
        )]
        [Description(@"Move the assets at paths in the project. Should be used for asset rename. Does AssetDatabase.Refresh() at the end.")]
        public Task<CallToolResponse> Move
        (
            [Description("The paths of the assets to move.")]
            string[] sourcePaths,
            [Description("The paths of moved assets.")]
            string[] destinationPaths
        )
        {
            return ToolRouter.Call("Assets_Move", arguments =>
            {
                arguments[nameof(sourcePaths)] = sourcePaths ?? new string[0];
                arguments[nameof(destinationPaths)] = destinationPaths ?? new string[0];
            });
        }
    }
}
#endif