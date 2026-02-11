
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

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public static class UIMcpUtils
    {
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