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
            Name = "Assets_Prefab_Open",
            Title = "Open prefab"
        )]
        [Description("Open a prefab. Use it for get started with prefab editing. There are two options to open prefab:\n" +
                    "1. Open prefab from asset using 'prefabAssetPath'.\n" +
                    "2. Open prefab from GameObject in loaded scene using 'instanceID' of the GameObject.\n" +
                    "   The GameObject should be connected to a prefab.\n\n" +
                    "Note: Please 'Close' the prefab after editing.")]
        public Task<CallToolResponse> Open
        (
            [Description("'instanceID' of GameObject in a scene.")]
            int instanceID = 0,
            [Description("Prefab asset path. Should be in the format 'Assets/Path/To/Prefab.prefab'.")]
            string? prefabAssetPath = null
        )
        {
            return ToolRouter.Call("Assets_Prefab_Open", arguments =>
            {
                arguments[nameof(instanceID)] = instanceID;
                arguments[nameof(prefabAssetPath)] = prefabAssetPath ?? string.Empty;
            });
        }
    }
}
#endif