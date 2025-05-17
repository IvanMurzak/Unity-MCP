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
            Name = "Assets_Prefab_Read",
            Title = "Read prefab content"
        )]
        [Description("Read a prefab content. Use it for get started with prefab editing. There are two options to open prefab:\n" +
                    "1. Read prefab from asset using 'prefabAssetPath'.\n" +
                    "2. Read prefab from GameObject in loaded scene using 'instanceID' of the GameObject.\n" +
                    "   The GameObject should be connected to a prefab.")]
        public Task<CallToolResponse> Read
        (
            [Description("'instanceID' of GameObject in a scene.")]
            int instanceID = 0,
            [Description("Prefab asset path. Should be in the format 'Assets/Path/To/Prefab.prefab'.")]
            string? prefabAssetPath = null,
            [Description("Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below.")]
            int includeChildrenDepth = 3
        )
        {
            return ToolRouter.Call("Assets_Prefab_Read", arguments =>
            {
                arguments[nameof(instanceID)] = instanceID;
                arguments[nameof(prefabAssetPath)] = prefabAssetPath ?? string.Empty;
                arguments[nameof(includeChildrenDepth)] = includeChildrenDepth;
            });
        }
    }
}
#endif