#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Scene
    {
        [McpServerTool
        (
            Name = "Scene_Save",
            Title = "Save scene"
        )]
        [Description("Save scene to asset file with specified path.")]
        public Task<CallToolResponse> Save
        (
            [Description("Path to the scene file.")]
            string path,
            [Description("Name of the opened scene. Could be empty if need to save current active scene. It is helpful when multiple scenes are opened.")]
            string? targetSceneName = null
        )
        {
            return ToolRouter.Call("Scene_Save", arguments =>
            {
                arguments[nameof(path)] = path;

                if (!string.IsNullOrEmpty(targetSceneName))
                    arguments[nameof(targetSceneName)] = targetSceneName;
            });
        }
    }
}
#endif