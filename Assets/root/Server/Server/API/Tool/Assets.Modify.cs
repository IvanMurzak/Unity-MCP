#if !UNITY_5_3_OR_NEWER
using com.IvanMurzak.Unity.MCP.Common.Data.Unity;
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
            Name = "Assets_Modify",
            Title = "Modify asset file"
        )]
        [Description(@"Modify asset in the project. Not allowed to modify asset in 'Packages/' folder. Please modify it in 'Assets/' folder.")]
        public Task<CallToolResponse> Modify
        (
            [Description("The asset content. It overrides the existing asset content.")]
            SerializedMember content,
            [Description("Path to the asset. See 'Assets_Search' for more details. Starts with 'Assets/'. Priority: 1. (Recommended)")]
            string? assetPath = null,
            [Description("GUID of the asset. Priority: 2.")]
            string? assetGuid = null
        )
        {
            return ToolRouter.Call("Assets_Modify", arguments =>
            {
                if (content != null)
                    arguments[nameof(content)] = content;

                arguments[nameof(assetPath)] = assetPath ?? string.Empty;
                arguments[nameof(assetGuid)] = assetGuid ?? string.Empty;
            });
        }
    }
}
#endif