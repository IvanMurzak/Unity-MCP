/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.Text.RegularExpressions;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class ErrorUtils
    {
        /// <summary>
        /// Extracts a process ID from file lock error messages.
        /// </summary>
        /// <param name="error">The error message to parse.</param>
        /// <param name="processId">The extracted process ID, or -1 if not found.</param>
        /// <returns>True if a process ID was successfully extracted, false otherwise.</returns>
        public static bool ExtractProcessId(string error, out int processId)
        {
            processId = -1;

            if (string.IsNullOrWhiteSpace(error))
                return false;

            try
            {
                // Define a regex pattern to match the process ID in file lock messages
                var pattern = @"The file is locked by: ""[^""]+ \((\d+)\)""";
                var match = Regex.Match(error, pattern);

                return match.Success && int.TryParse(match.Groups[1].Value, out processId);
            }
            catch (RegexMatchTimeoutException)
            {
                // Handle regex timeout gracefully
                return false;
            }
        }
    }
}
