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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    [InitializeOnLoad]
    public static partial class Tool_Script
    {
        static IEnumerable<Type> AllComponentTypes => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(UnityEngine.Component).IsAssignableFrom(type) && !type.IsAbstract);

        public static class Error
        {
            static string ComponentsPrinted => string.Join("\n", AllComponentTypes.Select(type => type.FullName));

            public static string ScriptPathIsEmpty()
                => "[Error] Script path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";

            public static string ScriptFileNotFound(params string[] files)
                => $"[Error] File(s) not found: {string.Join(", ", files.Select(f => $"'{f}'"))}. Please check the path(s) and try again.";

            public static string FilePathMustEndsWithCs()
                => "[Error] File path must end with \".cs\". Please provide a valid C# file path.";
        }
    }
}
