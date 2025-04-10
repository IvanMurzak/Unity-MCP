#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common.MCP
{
    public abstract class MethodWrapper
    {
        protected readonly MethodInfo _methodInfo;
        protected readonly object? _targetInstance;
        protected readonly Type? _targetType;
        protected readonly ILogger _logger;

        protected MethodWrapper(ILogger logger, MethodInfo methodInfo)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            if (!methodInfo.IsStatic)
                throw new ArgumentException("The provided method must be static.");

            _logger = logger;
            _methodInfo = methodInfo;
        }

        protected MethodWrapper(ILogger logger, object targetInstance, MethodInfo methodInfo)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (targetInstance == null)
                throw new ArgumentNullException(nameof(targetInstance));
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            if (methodInfo.IsStatic)
                throw new ArgumentException("The provided method must be an instance method. Use the other constructor for static methods.");

            _logger = logger;
            _targetInstance = targetInstance;
            _methodInfo = methodInfo;
        }

        protected MethodWrapper(ILogger logger, Type targetType, MethodInfo methodInfo)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            if (methodInfo.IsStatic)
                throw new ArgumentException("The provided method must be an instance method. Use the other constructor for static methods.");

            _logger = logger;
            _targetType = targetType;
            _methodInfo = methodInfo;
        }

        protected virtual object? Invoke(params object?[] parameters)
        {
            if (_methodInfo == null)
                throw new InvalidOperationException("The method information is not initialized.");

            // If _targetInstance is null and _targetType is set, create an instance of the target type
            var instance = _targetInstance ?? (_targetType != null ? Activator.CreateInstance(_targetType) : null);

            // Build the final parameters array, filling in default values where necessary
            var finalParameters = BuildParameters(parameters);
            if (finalParameters != null)
            {
                foreach (var parameter in finalParameters)
                    _logger.LogTrace("Parameter: {0}", parameter);
            }
            else
            {
                _logger.LogTrace("No parameters provided.");
            }
            // Invoke the method (static or instance)
            return _methodInfo.Invoke(instance, finalParameters);
        }

        protected virtual object? Invoke(IDictionary<string, object?>? namedParameters)
        {
            if (_methodInfo == null)
                throw new InvalidOperationException("The method information is not initialized.");

            // If _targetInstance is null and _targetType is set, create an instance of the target type
            var instance = _targetInstance ?? (_targetType != null ? Activator.CreateInstance(_targetType) : null);

            // Build the final parameters array, filling in default values where necessary
            var finalParameters = BuildParameters(namedParameters);
            PrintParameters(finalParameters);

            // Invoke the method (static or instance)
            return _methodInfo.Invoke(instance, finalParameters);
        }

        protected object?[]? BuildParameters(object?[]? parameters)
        {
            if (parameters == null)
                return null;

            var methodParameters = _methodInfo.GetParameters();

            // Prepare the final arguments array, filling in default values where necessary
            var finalParameters = new object?[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i < parameters.Length)
                {
                    // Handle JsonElement conversion
                    if (parameters[i] is JsonElement jsonElement)
                    {
                        finalParameters[i] = JsonSerializer.Deserialize(jsonElement.GetRawText(), methodParameters[i].ParameterType);
                    }
                    else
                    {
                        // Use the provided parameter value
                        finalParameters[i] = parameters[i];
                    }
                }
                else if (methodParameters[i].HasDefaultValue)
                {
                    // Use the default value if no value is provided
                    finalParameters[i] = methodParameters[i].DefaultValue;
                }
                else
                {
                    throw new ArgumentException($"No value provided for parameter '{methodParameters[i].Name}' and no default value is defined.");
                }
            }

            return finalParameters;
        }

        protected object?[]? BuildParameters(IDictionary<string, object?>? namedParameters)
        {
            if (namedParameters == null)
                return null;

            var methodParameters = _methodInfo.GetParameters();

            // Prepare the final arguments array
            var finalParameters = new object?[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];

                if (namedParameters != null && namedParameters.TryGetValue(parameter.Name!, out var value))
                {
                    if (value is JsonElement jsonElement)
                    {
                        finalParameters[i] = JsonSerializer.Deserialize(jsonElement.GetRawText(), methodParameters[i].ParameterType);
                    }
                    else
                    {
                        // Use the provided parameter value
                        finalParameters[i] = value;
                    }
                }
                else if (parameter.HasDefaultValue)
                {
                    // Use the default value if no value is provided
                    finalParameters[i] = parameter.DefaultValue;
                }
                else
                {
                    // Use the type's default value if no value is provided
                    finalParameters[i] = parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
                }
            }

            return finalParameters;
        }
        void PrintParameters(object?[]? parameters)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
                return;

            _logger.LogDebug("Invoke method: {0} {1}, Class: {2}", _methodInfo.ReturnType.Name, _methodInfo.Name, _targetType?.Name ?? "null");

            var methodParameters = _methodInfo.GetParameters();
            var maxLength = Math.Max(methodParameters.Length, parameters?.Length ?? 0);
            var result = new string[maxLength];

            for (var i = 0; i < maxLength; i++)
            {
                var parameterType = i < methodParameters.Length ? methodParameters[i].ParameterType.ToString() : "N/A";
                var parameterName = i < methodParameters.Length ? methodParameters[i].Name : "N/A";
                var parameterValue = i < (parameters?.Length ?? 0) ? parameters?[i]?.ToString() ?? "null" : "null";

                result[i] = $"{parameterType} {parameterName} = {parameterValue}";
            }

            var parameterLogs = string.Join(Environment.NewLine, result);
            _logger.LogDebug("Invoke method: Parameters. Input: {0}, Provided: {1}\n{2}", methodParameters.Length, parameters?.Length, parameterLogs);
        }
    }
}