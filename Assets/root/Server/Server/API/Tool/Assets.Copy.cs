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
            Name = "Assets_Copy",
            Title = "Assets Copy"
        )]
        [Description(@"Copy the asset at path and stores it at newPath. Does AssetDatabase.Refresh() at the end.")]
        public Task<CallToolResponse> Copy
        (
            [Description("The paths of the asset to copy.")]
            string[] sourcePaths,
            [Description("The paths to store the copied asset.")]
            string[] destinationPaths
        )
        {
            return ToolRouter.Call("Assets_Copy", arguments =>
            {
                arguments[nameof(sourcePaths)] = sourcePaths ?? new string[0];
                arguments[nameof(destinationPaths)] = destinationPaths ?? new string[0];
            });
        }
    }
}
#endif