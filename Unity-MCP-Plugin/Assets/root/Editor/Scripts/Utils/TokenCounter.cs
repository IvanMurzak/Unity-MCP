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
using System.Text.Json.Nodes;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Utility class for estimating token counts for MCP tool schemas.
    /// Uses a simple approximation based on the JSON string representation of the schema.
    /// </summary>
    public static class TokenCounter
    {
        /// <summary>
        /// Estimates the token count for a tool based on its name, description, and schemas.
        /// This is an approximation using the rule of thumb: 1 token ≈ 4 characters for English text.
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="description">The tool description</param>
        /// <param name="inputSchema">The input schema JSON node</param>
        /// <param name="outputSchema">The output schema JSON node (optional)</param>
        /// <returns>Estimated token count</returns>
        public static int EstimateToolTokens(string? toolName, string? description, JsonNode? inputSchema, JsonNode? outputSchema = null)
        {
            var totalChars = 0;

            // Add tool name
            if (!string.IsNullOrEmpty(toolName))
            {
                totalChars += toolName.Length;
            }

            // Add description
            if (!string.IsNullOrEmpty(description))
            {
                totalChars += description.Length;
            }

            // Add input schema
            if (inputSchema != null)
            {
                var inputJson = inputSchema.ToJsonString();
                totalChars += inputJson.Length;
            }

            // Add output schema (if provided)
            if (outputSchema != null)
            {
                var outputJson = outputSchema.ToJsonString();
                totalChars += outputJson.Length;
            }

            // Rough approximation: 1 token ≈ 4 characters
            // Add some overhead for structure/formatting
            var estimatedTokens = (int)Math.Ceiling(totalChars / 4.0);
            
            // Add a small overhead for JSON structure tokens
            estimatedTokens += 10;

            return estimatedTokens;
        }

        /// <summary>
        /// Formats a token count as a human-readable string with K suffix for thousands.
        /// </summary>
        /// <param name="tokens">The token count</param>
        /// <returns>Formatted string (e.g., "1.2K" or "345")</returns>
        public static string FormatTokenCount(int tokens)
        {
            if (tokens >= 1000)
            {
                var thousands = tokens / 1000.0;
                return $"{thousands:0.#}K";
            }
            return tokens.ToString();
        }
    }
}
