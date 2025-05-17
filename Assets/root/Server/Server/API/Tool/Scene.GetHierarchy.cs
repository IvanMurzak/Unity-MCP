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
            Name = "Scene_GetHierarchy",
            Title = "Get Scene Hierarchy"
        )]
        [Description("This tool retrieves the list of root GameObjects in the specified scene.")]
        public Task<CallToolResponse> GetHierarchy
        (
            [Description("Determines the depth of the hierarchy to include.")]
            int includeChildrenDepth = 3,
            [Description("Name of the loaded scene. If empty string, the active scene will be used.")]
            string? loadedSceneName = null
        )
        {
            return ToolRouter.Call("Scene_GetHierarchy", arguments =>
            {
                arguments[nameof(includeChildrenDepth)] = includeChildrenDepth;

                if (loadedSceneName != null && loadedSceneName.Length > 0)
                    arguments[nameof(loadedSceneName)] = loadedSceneName;
            });
        }
    }
}
#endif