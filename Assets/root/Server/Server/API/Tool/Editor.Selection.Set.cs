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
            Name = "Editor_Selection_Set",
            Title = "Set Selection in Unity Editor"
        )]
        [Description(@"'UnityEditor.Selection'. Access to the selection in the editor.
Use it to select Assets or GameObjects in a scene. Set empty array to clear selection.
Selection.instanceIDs - The actual unfiltered selection from the Scene returned as instance ids.
Selection.activeInstanceID -  The 'instanceID' of the actual object selection. Includes Prefabs, non-modifiable objects.")]
        public Task<CallToolResponse> Set
        (
            [Description("The 'instanceID' array of the target GameObjects.")]
            int[]? instanceIDs = null,
            [Description("The 'instanceID' of the target Object.")]
            int activeInstanceID = 0
        )
        {
            return ToolRouter.Call("Editor_Selection_Set", arguments =>
            {
                if (instanceIDs != null && instanceIDs.Length > 0)
                    arguments[nameof(instanceIDs)] = instanceIDs;

                arguments[nameof(activeInstanceID)] = activeInstanceID;
            });
        }
    }
}
#endif