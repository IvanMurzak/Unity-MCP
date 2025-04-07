#if !IGNORE
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Server;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    // [McpServerResourceType]
    public partial class Resource_Component : ServerResource
    {
        // [McpServerResource(Name = "Component.Add", Title = "Add Component")]
        [Description("Add new Component instance to a target GameObject.")]
        public Task<string> Add(
            [Description("Path to the GameObject.")]
            string path,
            [Description("Component class full name.")]
            string fullName)
        {
            return Execute(nameof(Add), commandData => commandData
                .SetOrAddParameter(nameof(path), path)
                .SetOrAddParameter(nameof(fullName), fullName));
        }
    }
}
#endif