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
            Name = "Scene_Load",
            Title = "Load scene"
        )]
        [Description("Load scene from the project assets.")]
        public Task<CallToolResponse> Load
        (
            [Description("Path to the scene file.")]
            string path,
            [Description("Load scene mode. 0 - Single, 1 - Additive.")]
            int loadSceneMode = 0
        )
        {
            return ToolRouter.Call("Scene_Load", arguments =>
            {
                arguments[nameof(path)] = path;
                arguments[nameof(loadSceneMode)] = loadSceneMode;
            });
        }
    }
}
#endif