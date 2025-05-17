#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Assets_Prefab
    {
        [McpServerTool
        (
            Name = "Assets_Prefab_Save",
            Title = "Save prefab"
        )]
        [Description("Save a prefab. Use it when you are in prefab editing mode in Unity Editor.")]
        public Task<CallToolResponse> Save()
        {
            return ToolRouter.Call("Assets_Prefab_Save");
        }
    }
}
#endif