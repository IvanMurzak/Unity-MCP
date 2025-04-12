#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common.Data
{
    public static class ResponseCallToolExtensions
    {
        public static IResponseCallTool Log(this IResponseCallTool target, ILogger logger, Exception? ex = null)
        {
            if (target.IsError)
                logger.LogError(ex, target.Content.FirstOrDefault()?.Text ?? "Tool execution error.");
            else
                logger.LogInformation(ex, target.Content.FirstOrDefault()?.Text ?? "Tool executed successfully.");

            return target;
        }

        public static IResponseData<IResponseCallTool> Pack(this IResponseCallTool target, string requestId, string? message = null)
        {
            if (target.IsError)
                return ResponseData<IResponseCallTool>.Error(requestId, message ?? target.Content.FirstOrDefault()?.Text ?? "Tool execution error.")
                    .SetData(target);
            else
                return ResponseData<IResponseCallTool>.Success(requestId, message ?? target.Content.FirstOrDefault()?.Text ?? "Tool executed successfully.")
                    .SetData(target);
        }
    }
}