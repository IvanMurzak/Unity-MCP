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
            Name = "Scene_GetLoaded",
            Title = "Get list of currently loaded scenes"
        )]
        [Description("Returns the list of currently loaded scenes.")]
        public Task<CallToolResponse> GetLoaded()
        {
            return ToolRouter.Call("Scene_GetLoaded");
        }
    }
}
#endif