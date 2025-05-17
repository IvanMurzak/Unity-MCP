#if !UNITY_5_3_OR_NEWER
using com.IvanMurzak.Unity.MCP.Common.Data.Unity;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_GameObject
    {
        [McpServerTool
        (
            Name = "GameObject_Duplicate",
            Title = "Duplicate GameObjects in opened Prefab and in a Scene"
        )]
        [Description(@"Duplicate GameObjects in opened Prefab and in a Scene by 'instanceID' (int) array.")]
        public Task<CallToolResponse> Duplicate
        (
            GameObjectRefList gameObjectRefs
        )
        {
            return ToolRouter.Call("GameObject_Duplicate", arguments =>
            {
                arguments[nameof(gameObjectRefs)] = gameObjectRefs;
            });
        }
    }
}
#endif