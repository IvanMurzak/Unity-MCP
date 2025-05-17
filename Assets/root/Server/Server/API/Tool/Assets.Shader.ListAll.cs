#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Assets_Shader
    {
        [McpServerTool
        (
            Name = "Assets_Shader_ListAll",
            Title = "List all shader names"
        )]
        [Description(@"Scans the project assets to find all shaders and to get the name from each of them. Returns the list of shader names.")]
        public Task<CallToolResponse> ListAll()
        {
            return ToolRouter.Call("Assets_Shader_ListAll");
        }
    }
}
#endif