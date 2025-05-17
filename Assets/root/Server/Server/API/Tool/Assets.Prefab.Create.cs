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
            Name = "Assets_Prefab_Create",
            Title = "Create prefab from a GameObject in a scene"
        )]
        [Description("Create a prefab from a GameObject in a scene. The prefab will be saved in the project assets at the specified path.")]
        public Task<CallToolResponse> Create
        (
            [Description("Prefab asset path. Should be in the format 'Assets/Path/To/Prefab.prefab'.")]
            string prefabAssetPath,
            [Description("'instanceID' of GameObject in a scene.")]
            int instanceID,
            [Description("If true, the prefab will replace the GameObject in the scene.")]
            bool replaceGameObjectWithPrefab = true
        )
        {
            return ToolRouter.Call("Assets_Prefab_Create", arguments =>
            {
                arguments[nameof(prefabAssetPath)] = prefabAssetPath ?? string.Empty;
                arguments[nameof(instanceID)] = instanceID;
                arguments[nameof(replaceGameObjectWithPrefab)] = replaceGameObjectWithPrefab;
            });
        }
    }
}
#endif