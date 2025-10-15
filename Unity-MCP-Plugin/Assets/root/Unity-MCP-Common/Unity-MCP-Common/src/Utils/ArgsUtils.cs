/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class ArgsUtils
    {
        public static Dictionary<string, string> ParseLineArguments(string[] args)
        {
            var providedArguments = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++)
            {
                var currentArg = args[i];

                // Handle arguments with '=' syntax (--key=value, -key=value, key=value)
                if (currentArg.Contains('='))
                {
                    var parts = currentArg.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = ExtractArgumentName(parts[0]);
                        providedArguments[key] = parts[1];
                        continue;
                    }
                }

                // Extract argument name (supports --, -, or no prefix)
                var argumentName = ExtractArgumentName(currentArg);

                // Check if next argument is a value (not starting with - unless it's a negative number)
                var value = string.Empty;
                if (i + 1 < args.Length)
                {
                    var nextArg = args[i + 1];
                    var isNextArgument = IsArgumentName(nextArg);

                    if (!isNextArgument)
                    {
                        value = nextArg;
                        i++; // Skip the next argument as it's the value
                    }
                }

                providedArguments[argumentName] = value;
            }

            return providedArguments;
        }

        private static string ExtractArgumentName(string arg)
        {
            if (arg.StartsWith("--"))
                return arg.Substring(2);
            if (arg.StartsWith("-"))
                return arg.Substring(1);
            return arg;
        }

        private static bool IsArgumentName(string arg)
        {
            // Check if it's an argument name (starts with - but not a negative number)
            if (!arg.StartsWith("-"))
                return false;

            // If it starts with - but is followed by a digit, it's likely a negative number
            if (arg.Length > 1 && char.IsDigit(arg[1]))
                return false;

            return true;
        }

        public static Dictionary<string, string> ParseCommandLineArguments()
        {
            var args = Environment.GetCommandLineArgs();
            return ParseLineArguments(args);
        }
    }
}
