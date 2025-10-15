/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class ExtensionsString
    {
        public static string Join(this IEnumerable<string> strings, string separator = ", ")
            => string.Join(separator, strings);

        public static string JoinExcept(this IEnumerable<string> strings, string except, string separator = ", ")
            => string.Join(separator, strings
                .Where(s => s != except));

        public static string JoinEnclose(this IEnumerable<string> strings, string separator = ", ", string enclose = "'")
            => string.Join(separator, strings
                .Select(s => $"{enclose}{s}{enclose}"));

        public static string JoinEncloseExcept(this IEnumerable<string> strings, string except, string separator = ", ", string enclose = "'")
            => string.Join(separator, strings
                .Where(s => s != except)
                .Select(s => $"{enclose}{s}{enclose}"));
    }
}
