#if !UNITY_5_3_OR_NEWER
using com.IvanMurzak.Unity.MCP.Server.API.Data;
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
            Name = "Assets_Prefab_Instantiate",
            Title = "Instantiate prefab in the current active scene"
        )]
        [Description("Instantiates prefab in a scene.")]
        public Task<CallToolResponse> Instantiate
        (
            [Description("Prefab asset path.")]
            string prefabAssetPath,
            [Description("GameObject path in the current active scene.")]
            string gameObjectPath,
            [Description("Transform position of the GameObject.")]
            Vector3? position = default,
            [Description("Transform rotation of the GameObject. Euler angles in degrees.")]
            Vector3? rotation = default,
            [Description("Transform scale of the GameObject.")]
            Vector3? scale = default,
            [Description("World or Local space of transform.")]
            bool isLocalSpace = false
        )
        {
            return ToolRouter.Call("Assets_Prefab_Instantiate", arguments =>
            {
                arguments[nameof(prefabAssetPath)] = prefabAssetPath;
                arguments[nameof(gameObjectPath)] = gameObjectPath;

                if (position != null)
                    arguments[nameof(position)] = position;

                if (rotation != null)
                    arguments[nameof(rotation)] = rotation;

                if (scale != null)
                    arguments[nameof(scale)] = scale;

                arguments[nameof(isLocalSpace)] = isLocalSpace;
            });
        }
    }
}
#endif