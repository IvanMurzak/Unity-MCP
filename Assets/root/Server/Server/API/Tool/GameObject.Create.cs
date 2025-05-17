#if !UNITY_5_3_OR_NEWER
using com.IvanMurzak.Unity.MCP.Common.Data.Unity;
using com.IvanMurzak.Unity.MCP.Server.API.Data;
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
            Name = "GameObject_Create",
            Title = "Create a new GameObject in opened Prefab or in a Scene"
        )]
        [Description(@"Create a new GameObject at specific path.
if needed - provide proper 'position', 'rotation' and 'scale' to reduce amount of operations.")]
        public Task<CallToolResponse> Create
        (
            [Description("Name of the new GameObject.")]
            string name,
            GameObjectRef? parentGameObjectRef = null,
            [Description("Transform position of the GameObject.")]
            Vector3? position = default,
            [Description("Transform rotation of the GameObject. Euler angles in degrees.")]
            Vector3? rotation = default,
            [Description("Transform scale of the GameObject.")]
            Vector3? scale = default,
            [Description("World or Local space of transform.")]
            bool isLocalSpace = false,
            [Description("-1 - No primitive type; 0 - Cube; 1 - Sphere; 2 - Capsule; 3 - Cylinder; 4 - Plane; 5 - Quad.")]
            int primitiveType = -1
        )
        {
            return ToolRouter.Call("GameObject_Create", arguments =>
            {
                arguments[nameof(name)] = name;

                if (parentGameObjectRef != null)
                    arguments[nameof(parentGameObjectRef)] = parentGameObjectRef;

                if (position != null)
                    arguments[nameof(position)] = position;

                if (rotation != null)
                    arguments[nameof(rotation)] = rotation;

                if (scale != null)
                    arguments[nameof(scale)] = scale;

                arguments[nameof(isLocalSpace)] = isLocalSpace;
                arguments[nameof(primitiveType)] = primitiveType;
            });
        }
    }
}
#endif