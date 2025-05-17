#if !UNITY_5_3_OR_NEWER
using com.IvanMurzak.Unity.MCP.Common.Data.Unity;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Scene
    {
        [McpServerTool
        (
            Name = "Reflection_MethodCall",
            Title = "Call method using C# reflection"
        )]
        [Description(@"Call C# method. Any method could be called, even private methods.
It requires to receive proper method schema.
Use 'Reflection_MethodFind' to find available method before using it.
Receives input parameters and returns result.")]
        public Task<CallToolResponse> MethodCall
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
0 - ignore 'filter.Parameters',
1 - parameters count is the same,
2 - equals (default value).")]
            int parametersMatchLevel = 2,

            [Description(@"Specify target object to call method on. Should be null if the method is static or if the is no specific target instance.
New instance of the specified class will be created if the method is instance method and the targetObject is null.
Required:
- type - full type name of the object to call method on.
- value - serialized object value. It will be deserialized to the specified type.")]
            SerializedMember? targetObject = null,

            [Description(@"Method input parameters. Per each parameter specify:
- type - full type name of the object to call method on.
- name - parameter name.
- value - serialized object value. It will be deserialized to the specified type.")]
            List<SerializedMember>? inputParameters = null,

            [Description(@"Set to true if the method should be executed in the main thread. Otherwise, set to false.")]
            bool executeInMainThread = true
        )
        {
            return ToolRouter.Call("Reflection_MethodCall", arguments =>
            {
                arguments[nameof(filter)] = filter;
                arguments[nameof(knownNamespace)] = knownNamespace;
                arguments[nameof(typeNameMatchLevel)] = typeNameMatchLevel;
                arguments[nameof(methodNameMatchLevel)] = methodNameMatchLevel;
                arguments[nameof(parametersMatchLevel)] = parametersMatchLevel;

                if (targetObject != null)
                    arguments[nameof(targetObject)] = targetObject;

                if (inputParameters != null)
                    arguments[nameof(inputParameters)] = inputParameters;

                arguments[nameof(executeInMainThread)] = executeInMainThread;
            });
        }
    }
}
#endif