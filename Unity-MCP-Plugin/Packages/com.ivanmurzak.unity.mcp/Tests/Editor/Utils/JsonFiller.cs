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
using System.Text.Json;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public static class JsonTestUtils
    {
        public static string Prettify(string json)
        {
            if (json == null)
                return string.Empty;

            var input = json.Trim();
            if (input.Length == 0)
                return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(input, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                return JsonSerializer.Serialize(doc.RootElement, options);
            }
            catch (JsonException)
            {
                // Not valid JSON; return as-is
                return json;
            }
        }
        public static string Fill(string json, IDictionary<string, object?> properties)
        {
            foreach (var kvp in properties)
            {
                var occurrences = CountOccurrences(json, kvp.Key);
                if (occurrences == 0)
                    throw new KeyNotFoundException($"Key '{kvp.Key}' not found in JSON.");

                if (occurrences > 1)
                    throw new InvalidOperationException($"Key '{kvp.Key}' found {occurrences} times in JSON, expected only once.");

                var value = kvp.Value?.ToString();

                if (value == null)
                    value = "null";

                if (value.Length == 0)
                    value = "\"\"";

                json = json.Replace(kvp.Key, value);
            }
            return json;
        }
        static int CountOccurrences(string text, string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
