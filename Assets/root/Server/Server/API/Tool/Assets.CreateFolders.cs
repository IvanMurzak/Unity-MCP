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
            Name = "Assets_CreateFolders",
            Title = "Assets Create Folders"
        )]
        [Description(@"Create folders at specific locations in the project.
Use it to organize scripts and assets in the project. Does AssetDatabase.Refresh() at the end.")]
        public Task<CallToolResponse> CreateFolders
        (
            [Description("The paths for the folders to create.")]
            string[] paths
        )
        {
            return ToolRouter.Call("Assets_CreateFolders", arguments =>
            {
                arguments[nameof(paths)] = paths ?? new string[0];
            });
        }
    }
}
#endif