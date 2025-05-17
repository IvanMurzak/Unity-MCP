#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Editor_Selection
    {
        [McpServerTool
        (
            Name = "Editor_Selection_Get",
            Title = "Get current Selection value in Unity Editor"
        )]
        [Description(@"'UnityEditor.Selection'. Access to the selection in the editor.
Use it to get information about selected Assets or GameObjects in a scene.
Selection.transforms - Returns the top level selection instanceIDs, excluding Prefabs.
Selection.instanceIDs - The actual unfiltered selection from the Scene returned as instance ids instead of objects.
Selection.gameObjects - Returns the actual game object selection. Includes Prefabs, non-modifiable objects. (Read Only)
Selection.assetGUIDs - Returns the guids of the selected assets. (Read Only)
Selection.activeGameObject - Returns the active game object. (The one shown in the inspector). (Read Only)
Selection.activeInstanceID - Returns the instanceID of the actual object selection. Includes Prefabs, non-modifiable objects.
Selection.activeObject - Returns the actual object selection. Includes Prefabs, non-modifiable objects.
Selection.activeTransform - Returns the active transform. (The one shown in the inspector).")]
        public Task<CallToolResponse> Get()
        {
            return ToolRouter.Call("Editor_Selection_Get");
        }
    }
}
#endif