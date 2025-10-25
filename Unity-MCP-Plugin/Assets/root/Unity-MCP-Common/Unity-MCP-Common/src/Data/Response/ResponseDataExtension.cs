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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public static class ResponseDataExtension
    {
        public static IResponseData<T> Log<T>(this IResponseData<T> response, ILogger logger, string? operationContext = null, Exception? ex = null)
        {
            var contextPrefix = string.IsNullOrEmpty(operationContext)
                ? string.Empty
                : $"[{operationContext}] ";

            switch (response.Status)
            {
                case ResponseStatus.Error:
                    var errorMsg = response.Message ?? "Operation failed without specific error details";
                    logger.LogError(ex, "{Context}Error: {Message}", contextPrefix, errorMsg);
                    break;

                case ResponseStatus.Success:
                    var successMsg = response.Message ?? "Operation completed successfully";
                    logger.LogInformation("{Context}Success: {Message}", contextPrefix, successMsg);
                    break;

                case ResponseStatus.Processing:
                    var processingMsg = response.Message ?? "Operation is currently being processed";
                    logger.LogInformation("{Context}Processing: {Message}", contextPrefix, processingMsg);
                    break;

                default:
                    logger.LogWarning("{Context}Unknown status '{Status}' with message: {Message}",
                        contextPrefix, response.Status, response.Message ?? "No message provided");
                    break;
            }

            return response;
        }
        public static IResponseData<T> SetData<T>(this IResponseData<T> response, T? data)
        {
            response.Value = data;
            return response;
        }
        public static IResponseData<T> SetError<T>(this IResponseData<T> response, string? message = null, Exception? exception = null)
        {
            response.Status = ResponseStatus.Error;

            if (exception != null && string.IsNullOrEmpty(message))
                response.Message = $"Operation failed: {exception.Message}";
            else if (exception != null && !string.IsNullOrEmpty(message))
                response.Message = $"{message}. Details: {exception.Message}";
            else
                response.Message = message ?? "Operation failed without specific error details";

            return response;
        }
        public static IResponseData<T> SetSuccess<T>(this IResponseData<T> response, string? message = null)
        {
            response.Status = ResponseStatus.Success;
            response.Message = message ?? "Operation completed successfully";
            return response;
        }
        public static IResponseData<T> SetProcessing<T>(this IResponseData<T> response, string? message = null)
        {
            response.Status = ResponseStatus.Processing;
            response.Message = message ?? "Operation is currently being processed";
            return response;
        }

        public static Task<IResponseData<T>> TaskFromResult<T>(this IResponseData<T> response)
            => Task.FromResult(response);
    }
}
