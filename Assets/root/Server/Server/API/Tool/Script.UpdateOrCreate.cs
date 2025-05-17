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
            Name = "Script_CreateOrUpdate",
            Title = "Create or Update Script"
        )]
        [Description("Creates or updates a script file with the provided content. Does AssetDatabase.Refresh() at the end.")]
        public Task<CallToolResponse> UpdateOrCreate
        (
            [Description("The path to the file. Sample: \"Assets/Scripts/MyScript.cs\".")]
            string filePath,
            [Description("C# code - content of the file.")]
            string content
        )
        {
            return ToolRouter.Call("Script_CreateOrUpdate", arguments =>
            {
                arguments[nameof(filePath)] = filePath;
                arguments[nameof(content)] = content;
            });
        }
    }
}
#endif