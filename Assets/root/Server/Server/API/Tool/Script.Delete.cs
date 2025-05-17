#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Script
    {
        [McpServerTool
        (
            Name = "Script_Delete",
            Title = "Delete Script content"
        )]
        [Description("Delete the script file. Does AssetDatabase.Refresh() at the end.")]
        public Task<CallToolResponse> Delete
        (
            [Description("The path to the file. Sample: \"Assets/Scripts/MyScript.cs\".")]
            string filePath
        )
        {
            return ToolRouter.Call("Script_Delete", arguments =>
            {
                arguments[nameof(filePath)] = filePath;
            });
        }
    }
}
#endif