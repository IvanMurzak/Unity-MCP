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
            Name = "Script_Read",
            Title = "Read Script content"
        )]
        [Description("Reads the content of a script file and returns it as a string.")]
        public Task<CallToolResponse> Read
        (
            [Description("The path to the file. Sample: \"Assets/Scripts/MyScript.cs\".")]
            string filePath
        )
        {
            return ToolRouter.Call("Script_Read", arguments =>
            {
                arguments[nameof(filePath)] = filePath;
            });
        }
    }
}
#endif