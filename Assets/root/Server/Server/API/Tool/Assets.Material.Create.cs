#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Assets_Material
    {
        [McpServerTool
        (
            Name = "Assets_Material_Create",
            Title = "Create Material asset"
        )]
        [Description(@"Create new material asset with default parameters. Right 'shaderName' should be set. To find the shader, use 'Shader.Find' method.")]
        public Task<CallToolResponse> Create
        (
            [Description("Asset path. Starts with 'Assets/'. Ends with '.mat'.")]
            string assetPath,
            [Description("Name of the shader that need to be used to create the material.")]
            string shaderName
        )
        {
            return ToolRouter.Call("Assets_Material_Create", arguments =>
            {
                arguments[nameof(assetPath)] = assetPath ?? string.Empty;
                arguments[nameof(shaderName)] = shaderName ?? string.Empty;
            });
        }
    }
}
#endif