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
            Name = "Assets_Read",
            Title = "Read asset file content"
        )]
        [Description(@"Read file asset in the project.")]
        public Task<CallToolResponse> Read
        (
            [Description("Path to the asset. See 'Assets_Search' for more details. Starts with 'Assets/'. Priority: 1. (Recommended)")]
            string? assetPath = null,
            [Description("GUID of the asset. Priority: 2.")]
            string? assetGuid = null
        )
        {
            return ToolRouter.Call("Assets_Read", arguments =>
            {
                if (assetPath != null && assetPath.Length > 0)
                    arguments[nameof(assetPath)] = assetPath;

                if (assetGuid != null && assetGuid.Length > 0)
                    arguments[nameof(assetGuid)] = assetGuid;
            });
        }
    }
}
#endif