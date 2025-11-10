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
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public partial class RunTool
    {
        private JsonNode? _cachedOutputSchema;
        private bool _outputSchemaComputed = false;

        /// <summary>
        /// Implements OutputSchema to return null for methods that return ResponseCallTool.
        /// ResponseCallTool is the MCP protocol wrapper itself, not user data, so it should not have an output schema.
        /// </summary>
        JsonNode? IRunTool.OutputSchema
        {
            get
            {
                if (_outputSchemaComputed)
                    return _cachedOutputSchema;

                _outputSchemaComputed = true;

                if (Method == null)
                {
                    _cachedOutputSchema = null;
                    return null;
                }

                // Get the actual return type, unwrapping Task<T> if necessary
                var returnType = Method.ReturnType;
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    returnType = returnType.GetGenericArguments()[0];
                }

                // If the return type is ResponseCallTool, don't generate an output schema
                if (returnType == typeof(ResponseCallTool) || returnType.IsSubclassOf(typeof(ResponseCallTool)))
                {
                    _cachedOutputSchema = null;
                    return null;
                }

                // For all other types, delegate to MethodWrapper (access via reflection since it's in external DLL)
                var methodWrapperType = typeof(RunTool).BaseType;
                if (methodWrapperType != null)
                {
                    var outputSchemaProperty = methodWrapperType.GetProperty("OutputSchema");
                    if (outputSchemaProperty != null)
                    {
                        _cachedOutputSchema = outputSchemaProperty.GetValue(this) as JsonNode;
                        return _cachedOutputSchema;
                    }
                }

                _cachedOutputSchema = null;
                return null;
            }
        }
    }
}
