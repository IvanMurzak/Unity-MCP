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
using System.Reflection;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    /// <summary>
    /// Provides functionality to execute methods dynamically, supporting both static and instance methods.
    /// Allows for parameter passing by position or by name, with support for default parameter values.
    /// </summary>
    public partial class RunResourceContext : MethodWrapper, IRunResourceContext
    {
        /// <summary>
        /// Initializes the Command with the target method information.
        /// </summary>
        /// <param name="type">The type containing the static method.</param>
        public static RunResourceContext CreateFromStaticMethod(Reflector reflector, ILogger? logger, MethodInfo methodInfo)
            => new RunResourceContext(reflector, logger, methodInfo);

        /// <summary>
        /// Initializes the Command with the target instance method information.
        /// </summary>
        /// <param name="targetInstance">The instance of the object containing the method.</param>
        /// <param name="methodInfo">The MethodInfo of the instance method to execute.</param>
        public static RunResourceContext CreateFromInstanceMethod(Reflector reflector, ILogger? logger, object targetInstance, MethodInfo methodInfo)
            => new RunResourceContext(reflector, logger, targetInstance, methodInfo);

        /// <summary>
        /// Initializes the Command with the target instance method information.
        /// </summary>
        /// <param name="targetInstance">The instance of the object containing the method.</param>
        /// <param name="methodInfo">The MethodInfo of the instance method to execute.</param>
        public static RunResourceContext CreateFromClassMethod(Reflector reflector, ILogger? logger, Type targetType, MethodInfo methodInfo)
            => new RunResourceContext(reflector, logger, targetType, methodInfo);

        public RunResourceContext(Reflector reflector, ILogger? logger, MethodInfo methodInfo) : base(reflector, logger, methodInfo) { }
        public RunResourceContext(Reflector reflector, ILogger? logger, object targetInstance, MethodInfo methodInfo) : base(reflector, logger, targetInstance, methodInfo) { }
        public RunResourceContext(Reflector reflector, ILogger? logger, Type targetType, MethodInfo methodInfo) : base(reflector, logger, targetType, methodInfo) { }

        /// <summary>
        /// Executes the target static method with the provided arguments.
        /// </summary>
        /// <param name="parameters">The arguments to pass to the method.</param>
        /// <returns>The result of the method execution, or null if the method is void.</returns>
        public async Task<ResponseListResource[]> Run(params object?[] parameters)
        {
            var result = await Invoke(parameters);
            return result as ResponseListResource[] ?? throw new InvalidOperationException($"The method did not return a valid {nameof(ResponseListResource)}[]. Instead returned {result?.GetType().GetTypeShortName()}.");
        }

        /// <summary>
        /// Executes the target method with named parameters.
        /// Missing parameters will be filled with their default values or the type's default value if no default is defined.
        /// </summary>
        /// <param name="namedParameters">A dictionary mapping parameter names to their values.</param>
        /// <returns>The result of the method execution, or null if the method is void.</returns>
        public async Task<ResponseListResource[]> Run(IDictionary<string, object?>? namedParameters)
        {
            var result = await InvokeDict((IReadOnlyDictionary<string, object?>?)namedParameters);
            return result as ResponseListResource[] ?? throw new InvalidOperationException($"The method did not return a valid {nameof(ResponseListResource)}[]. Instead returned {result?.GetType().GetTypeShortName()}.");
        }
    }
}
