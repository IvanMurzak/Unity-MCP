/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using System;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public static class ResponseCallToolExtensions
    {
        public static ResponseCallTool Log(this ResponseCallTool target, ILogger logger, Exception? ex = null)
        {
            if (target.Status == ResponseStatus.Error)
                logger.LogError(ex, $"Error Response to AI:\n{target.GetMessage()}");
            else if (target.Status == ResponseStatus.Success)
                logger.LogInformation(ex, $"Success Response to AI:\n{target.GetMessage()}");
            else if (target.Status == ResponseStatus.Processing)
                logger.LogInformation(ex, $"Processing Response to AI:\n{target.GetMessage()}");

            return target;
        }

        public static IResponseData<ResponseCallTool> Pack(this ResponseCallTool target, string requestId, string? message = null)
        {
            if (target.Status == ResponseStatus.Error)
                return ResponseData<ResponseCallTool>.Error(requestId, message ?? target.GetMessage() ?? "Tool execution error.")
                    .SetData(target);
            else if (target.Status == ResponseStatus.Success)
                return ResponseData<ResponseCallTool>.Success(requestId, message ?? target.GetMessage() ?? "Tool executed successfully.")
                    .SetData(target);
            else if (target.Status == ResponseStatus.Processing)
                return ResponseData<ResponseCallTool>.Processing(requestId, message ?? target.GetMessage() ?? "Tool is processing.")
                    .SetData(target);

            return ResponseData<ResponseCallTool>.Error(requestId, $"Unknown tool status `{target.Status}`.")
                .SetData(target);
        }
    }
}
