#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Editor
    {
        [McpServerTool
        (
            Name = "Editor_SetApplicationState",
            Title = "Set Unity Editor application state"
        )]
        [Description("Control the Unity Editor application state. You can start, stop, or pause the 'playmode'.")]
        public Task<CallToolResponse> SetApplicationState
        (
            [Description("If true, the 'playmode' will be started. If false, the 'playmode' will be stopped.")]
            bool isPlaying = false,
            [Description("If true, the 'playmode' will be paused. If false, the 'playmode' will be resumed.")]
            bool isPaused = false
        )
        {
            return ToolRouter.Call("Editor_SetApplicationState", arguments =>
            {
                arguments[nameof(isPlaying)] = isPlaying;
                arguments[nameof(isPaused)] = isPaused;
            });
        }
    }
}
#endif