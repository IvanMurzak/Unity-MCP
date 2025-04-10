#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Common.Data;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class CommandDataExtensions
    {
        public static IRequestTool SetName(this IRequestTool data, string name)
        {
            data.Method = name;
            return data;
        }
        public static IRequestTool SetOrAddParameter(this IRequestTool data, string name, object? value)
        {
            data.Parameters ??= new Dictionary<string, object?>();
            data.Parameters[name] = value;
            return data;
        }
        public static IRequestData BuildRequest(this IRequestTool data)
            => new RequestData(data as RequestTool ?? throw new System.InvalidOperationException("CommandData is null"));
    }
}