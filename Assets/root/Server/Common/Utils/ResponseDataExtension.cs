#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using com.IvanMurzak.Unity.MCP.Common.Data;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class ResponseDataExtension
    {
        public static IResponseData Log(this IResponseData response, ILogger logger, Exception? ex = null)
        {
            if (response.IsSuccess)
                logger.LogInformation(ex, response.Message ?? "Command executed successfully.");
            else
                logger.LogError(ex, response.Message ?? "Command execution failed.");

            return response;
        }
    }
}