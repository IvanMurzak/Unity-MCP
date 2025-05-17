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
            Name = "GameObject_Modify",
            Title = "Modify GameObjects in opened Prefab or in a Scene"
        )]
        [Description(@"Modify GameObjects and/or attached component's field and properties.
You can modify multiple GameObjects at once. Just provide the same number of GameObject references and SerializedMember objects.")]
        public Task<CallToolResponse> Modify
        (
            [Description(@"Json Object with required readonly 'instanceID' and 'type' fields.
Each field and property requires to have 'type' and 'name' fields to identify the exact modification target.
Follow the object schema to specify what to change, ignore values that should not be modified. Keep the original data structure.
Any unknown or wrong located fields and properties will be ignored.
Check the result of this command to see what was changed. The ignored fields and properties will be listed.")]
            SerializedMemberList gameObjectDiffs,
            GameObjectRefList gameObjectRefs
        )
        {
            return ToolRouter.Call("GameObject_Modify", arguments =>
            {
                arguments[nameof(gameObjectDiffs)] = gameObjectDiffs ?? new();
                arguments[nameof(gameObjectRefs)] = gameObjectRefs ?? new();
            });
        }
    }
}
#endif