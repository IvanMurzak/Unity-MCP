/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using AIGD;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ToolFormat
{
    // Tools and types of the RqlToolBinding tests.
    public enum PresentationTestMode { Fast, Slow }

    public class PresentationTestItem
    {
        public string? name;
        public int count;
    }

    public class PresentationTestSettings
    {
        public string? title;
        public PresentationTestMode mode;
        public List<PresentationTestItem>? items;
    }

    public static class PresentationTestTools
    {
        public static PresentationTestSettings Echo(PresentationTestSettings settings, int times = 1, bool loud = false)
        {
            if (loud) settings.title = settings.title?.ToUpperInvariant();
            if (settings.items != null) foreach (var item in settings.items) item.count *= times;
            return settings;
        }

        public static List<PresentationTestItem> Items(string name) => new List<PresentationTestItem>
        {
            new PresentationTestItem { name = name, count = 1 },
            new PresentationTestItem { name = "b", count = 2 },
        };

        // Required: name only. string? / GameObjectRef? (NRT) and int? (Nullable<T>) without a default are optional, as is times.
        public static string Optionals(string name, string? note, GameObjectRef? gameObjectRef, int? count, int times = 1) =>
            name + "|" + (note ?? "null") + "|" + (gameObjectRef == null ? "null" : "ref") + "|" + (count?.ToString() ?? "null") + "|" + times;
    }
}
