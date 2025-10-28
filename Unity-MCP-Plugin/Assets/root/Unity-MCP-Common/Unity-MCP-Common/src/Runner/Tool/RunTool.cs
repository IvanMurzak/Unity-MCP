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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    /// <summary>
    /// Provides functionality to execute methods dynamically, supporting both static and instance methods.
    /// Allows for parameter passing by position or by name, with support for default parameter values.
    /// </summary>
    public partial class RunTool : MethodWrapper, IRunTool
    {
        public bool Enabled { get; set; } = true;
        public string? Title { get; protected set; }
        public MethodInfo? Method { get; private set; }

        protected string? RequestID { get; set; }

        /// <summary>
        /// Initializes the Command with the target method information.
        /// </summary>
        /// <param name="type">The type containing the static method.</param>
        public static RunTool CreateFromStaticMethod(Reflector reflector, ILogger? logger, MethodInfo methodInfo, string? title = null)
            => new RunTool(reflector, logger, methodInfo)
            {
                Title = title
            };

        /// <summary>
        /// Initializes the Command with the target instance method information.
        /// </summary>
        /// <param name="targetInstance">The instance of the object containing the method.</param>
        /// <param name="methodInfo">The MethodInfo of the instance method to execute.</param>
        public static RunTool CreateFromInstanceMethod(Reflector reflector, ILogger? logger, object targetInstance, MethodInfo methodInfo, string? title = null)
            => new RunTool(reflector, logger, targetInstance, methodInfo)
            {
                Title = title
            };

        /// <summary>
        /// Initializes the Command with the target instance method information.
        /// </summary>
        /// <param name="targetInstance">The instance of the object containing the method.</param>
        /// <param name="methodInfo">The MethodInfo of the instance method to execute.</param>
        public static RunTool CreateFromClassMethod(Reflector reflector, ILogger? logger, Type classType, MethodInfo methodInfo, string? title = null)
            => new RunTool(reflector, logger, classType, methodInfo)
            {
                Title = title
            };

        public RunTool(Reflector reflector, ILogger? logger, MethodInfo methodInfo) : base(reflector, logger, methodInfo)
        {
            Method = methodInfo;
        }

        public RunTool(Reflector reflector, ILogger? logger, object targetInstance, MethodInfo methodInfo) : base(reflector, logger, targetInstance, methodInfo)
        {
            Method = methodInfo;
        }

        public RunTool(Reflector reflector, ILogger? logger, Type classType, MethodInfo methodInfo) : base(reflector, logger, classType, methodInfo)
        {
            Method = methodInfo;
        }

        protected override object? GetParameterValue(Reflector reflector, ParameterInfo paramInfo, object? value)
        {
            if (paramInfo.GetCustomAttribute<RequestIDAttribute>() != null)
            {
                _logger?.LogTrace("Injecting RequestID parameter: {RequestID}", RequestID);
                return RequestID;
            }
            return base.GetParameterValue(reflector, paramInfo, value);
        }
        protected override object? GetParameterValue(Reflector reflector, ParameterInfo paramInfo, IReadOnlyDictionary<string, object?>? namedParameters)
        {
            if (paramInfo.GetCustomAttribute<RequestIDAttribute>() != null)
            {
                _logger?.LogTrace("Injecting RequestID parameter: {RequestID}", RequestID);
                return RequestID;
            }
            return base.GetParameterValue(reflector, paramInfo, namedParameters);
        }
        protected override object? GetDefaultParameterValue(Reflector reflector, ParameterInfo methodParameter)
        {
            if (methodParameter.GetCustomAttribute<RequestIDAttribute>() != null)
            {
                _logger?.LogTrace("Injecting RequestID parameter: {RequestID}", RequestID);
                return RequestID;
            }
            return base.GetDefaultParameterValue(reflector, methodParameter);
        }

        protected ResponseCallTool ProcessInvokeResult(string requestId, object? result)
        {
            if (result is ResponseCallTool response)
                return response.SetRequestID(requestId);

            if (result == null)
                return ResponseCallTool.Success(null).SetRequestID(requestId);

            var type = result.GetType();
            if (TypeUtils.IsPrimitive(type))
                return ResponseCallTool.Success(result.ToString()).SetRequestID(requestId);

            var node = System.Text.Json.JsonSerializer.SerializeToNode(result, _reflector.JsonSerializer.JsonSerializerOptions);
            var json = node?.ToJsonString(_reflector.JsonSerializer.JsonSerializerOptions);

            return ResponseCallTool.SuccessStructured(
                structuredContent: node,
                message: json ?? "[Success] null" // needed for MCP backward compatibility: https://modelcontextprotocol.io/specification/2025-06-18/server/tools#structured-content
            ).SetRequestID(requestId);
        }

        /// <summary>
        /// Executes the target static method with the provided arguments.
        /// </summary>
        /// <param name="requestId">The unique identifier for this request.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <param name="parameters">The arguments to pass to the method.</param>
        /// <returns>The result of the method execution, or null if the method is void.</returns>
        public async Task<ResponseCallTool> Run(string requestId, CancellationToken cancellationToken = default, params object?[] parameters)
        {
            var validationResult = ValidateRunParameters(requestId, parameters);
            if (validationResult != null)
                return validationResult;

            RequestID = requestId;
            try
            {
                // Invoke the method (static or instance)
                var result = await Invoke(cancellationToken, parameters);
                return ProcessInvokeResult(requestId, result);
            }
            catch (ArgumentException ex)
            {
                var errorMessage = $"Parameter validation failed for tool '{Title ?? this.Method?.Name}': {ex.Message}";
                _logger?.LogError(ex, errorMessage);
                return ResponseCallTool.Error(errorMessage).SetRequestID(requestId);
            }
            catch (TargetParameterCountException ex)
            {
                var errorMessage = $"Parameter count mismatch for tool '{Title ?? this.Method?.Name}'. Expected {this.Method?.GetParameters().Length} parameters, but received {parameters?.Length}";
                _logger?.LogError(ex, errorMessage);
                return ResponseCallTool.Error(errorMessage).SetRequestID(requestId);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Tool execution failed for '{Title ?? this.Method?.Name}': {(ex.InnerException ?? ex).Message}";
                _logger?.LogError(ex, $"{errorMessage}\n{ex.StackTrace}");
                return ResponseCallTool.Error(errorMessage).SetRequestID(requestId);
            }
        }

        /// <summary>
        /// Executes the target method with named parameters.
        /// Missing parameters will be filled with their default values or the type's default value if no default is defined.
        /// </summary>
        /// <param name="requestId">The unique identifier for this request.</param>
        /// <param name="namedParameters">A dictionary mapping parameter names to their values.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The result of the method execution, or null if the method is void.</returns>
        public async Task<ResponseCallTool> Run(string requestId, IReadOnlyDictionary<string, JsonElement>? namedParameters, CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateRunParameters(requestId, namedParameters);
            if (validationResult != null)
                return validationResult;

            RequestID = requestId;
            try
            {
                var finalParameters = ConvertNamedParameters(namedParameters);

                // Invoke the method (static or instance)
                var result = await InvokeDict(finalParameters, cancellationToken);
                return ProcessInvokeResult(requestId, result);
            }
            catch (ArgumentException ex)
            {
                var errorMessage = $"Parameter validation failed for tool '{Title ?? this.Method?.Name}': {ex.Message}";
                _logger?.LogError(ex, errorMessage);
                return ResponseCallTool.Error(errorMessage).SetRequestID(requestId);
            }
            catch (JsonException ex)
            {
                var errorMessage = $"JSON parameter parsing failed for tool '{Title ?? this.Method?.Name}': {ex.Message}";
                _logger?.LogError(ex, errorMessage);
                return ResponseCallTool.Error(errorMessage).SetRequestID(requestId);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Tool execution failed for '{Title ?? this.Method?.Name}': {(ex.InnerException ?? ex).Message}";
                _logger?.LogError(ex, $"{errorMessage}\n{ex.StackTrace}");
                return ResponseCallTool.Error(errorMessage).SetRequestID(requestId);
            }
        }


        /// <summary>
        /// Validates common parameters for tool execution.
        /// </summary>
        /// <param name="requestId">The request identifier to validate.</param>
        /// <param name="parameters">Additional parameters for context.</param>
        /// <returns>An error response if validation fails, null if validation passes.</returns>
        private ResponseCallTool? ValidateRunParameters(string requestId, object? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                var errorMessage = $"Request ID cannot be null or empty for tool '{Title ?? this.Method?.Name}'";
                _logger?.LogError(errorMessage);
                return ResponseCallTool.Error(errorMessage);
            }

            if (this.Method == null)
            {
                var errorMessage = $"Method information is not available for tool '{Title}'";
                _logger?.LogError(errorMessage);
                return ResponseCallTool.Error(errorMessage).SetRequestID(requestId);
            }

            // Validate method is accessible
            if (!this.Method.IsPublic && !this.Method.IsFamily)
            {
                var errorMessage = $"Method '{this.Method.Name}' in tool '{Title}' is not accessible (must be public or protected)";
                _logger?.LogError(errorMessage);
                return ResponseCallTool.Error(errorMessage).SetRequestID(requestId);
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Converts named parameters from JsonElement dictionary to object dictionary with improved error handling.
        /// </summary>
        /// <param name="namedParameters">The named parameters to convert.</param>
        /// <returns>A dictionary with object values.</returns>
        private Dictionary<string, object?>? ConvertNamedParameters(IReadOnlyDictionary<string, JsonElement>? namedParameters)
        {
            if (namedParameters == null)
                return null;

            try
            {
                return namedParameters.ToDictionary(
                    keySelector: kvp => kvp.Key,
                    elementSelector: kvp => (object?)kvp.Value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to convert named parameters: {ex.Message}", ex);
            }
        }
    }
}
