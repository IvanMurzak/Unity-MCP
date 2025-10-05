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
    public static class IResponseResourceTemplateExtensions
    {
        public static ResponseResourceTemplate[] Log(this ResponseResourceTemplate[] target, ILogger logger, Exception? ex = null)
        {
            if (!logger.IsEnabled(LogLevel.Information))
                return target;

            foreach (var item in target)
                logger.LogInformation(ex, "Resource Template: {0}", item.uriTemplate);

            return target;
        }

        public static IResponseData<ResponseResourceTemplate[]> Pack(this ResponseResourceTemplate[] target, string requestId, string? message = null)
            => ResponseData<ResponseResourceTemplate[]>.Success(requestId, message ?? "List Tool execution completed.")
                .SetData(target);
    }
}
