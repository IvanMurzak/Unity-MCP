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
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class ExtensionsObject
    {
        public static Task<T> TaskFromResult<T>(this T response)
            => Task.FromResult(response);

        public static T[] MakeArray<T>(this T item) => new T[] { item };
        public static List<T> MakeList<T>(this T item) => new List<T> { item };

        public static string JoinString(this IEnumerable<string> items, string separator)
            => string.Join(separator, items);
        public static string JoinString(this IEnumerable<int> items, string separator)
            => string.Join(separator, items);
    }
}
