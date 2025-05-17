#if !UNITY_5_3_OR_NEWER
using com.IvanMurzak.Unity.MCP.Common.Data.Unity;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Scene
    {
        [McpServerTool
        (
            Name = "Reflection_MethodFind",
            Title = "Find method using reflection"
        )]
        [Description(@"Find method in the project using C# Reflection.
It looks for all assemblies in the project and finds method by its name, class name and parameters.
Even private methods are available. Use 'Reflection_MethodCall' to call the method after finding it.")]
        public Task<CallToolResponse> MethodFind
        (
            MethodPointerRef filter,

            [Description("Set to true if 'Namespace' is known and full namespace name is specified in the 'filter.Namespace' property. Otherwise, set to false.")]
            bool knownNamespace = false,

            [Description(@"Minimal match level for 'typeName'.
0 - ignore 'filter.typeName',
1 - contains ignoring case (default value),
2 - contains case sensitive,
3 - starts with ignoring case,
4 - starts with case sensitive,
5 - equals ignoring case,
6 - equals case sensitive.")]
            int typeNameMatchLevel = 1,

            [Description(@"Minimal match level for 'MethodName'.
0 - ignore 'filter.MethodName',
1 - contains ignoring case (default value),
2 - contains case sensitive,
3 - starts with ignoring case,
4 - starts with case sensitive,
5 - equals ignoring case,
6 - equals case sensitive.")]
            int methodNameMatchLevel = 1,

            [Description(@"Minimal match level for 'Parameters'.
0 - ignore 'filter.Parameters' (default value),
1 - parameters count is the same,
2 - equals.")]
            int parametersMatchLevel = 0
        )
        {
            return ToolRouter.Call("Reflection_MethodFind", arguments =>
            {
                arguments[nameof(filter)] = filter;
                arguments[nameof(knownNamespace)] = knownNamespace;
                arguments[nameof(typeNameMatchLevel)] = typeNameMatchLevel;
                arguments[nameof(methodNameMatchLevel)] = methodNameMatchLevel;
                arguments[nameof(parametersMatchLevel)] = parametersMatchLevel;
            });
        }
    }
}
#endif