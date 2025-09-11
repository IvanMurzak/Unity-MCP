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
using System.ComponentModel;
using System.IO;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Common;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_Script
    {
        [McpPluginTool
        (
            "Script_Read",
            Title = "Read Script content"
        )]
        [Description("Reads the content of a script file and returns it as a string.")]
        public static string Read
        (
            [Description("The path to the file. Sample: \"Assets/Scripts/MyScript.cs\".")]
            string filePath,
            [Description("The line number to start reading from (1-based).")]
            int lineFrom = 1,
            [Description("The line number to stop reading at (1-based, -1 for all lines).")]
            int lineTo = -1
        )
        {
            if (string.IsNullOrEmpty(filePath))
                return Error.ScriptPathIsEmpty();

            if (!filePath.EndsWith(".cs"))
                return Error.FilePathMustEndsWithCs();

            if (File.Exists(filePath) == false)
                return Error.ScriptFileNotFound(filePath);

            var lines = File.ReadAllLines(filePath);

            if (lineFrom < 1 || lineFrom > lines.Length)
                lineFrom = 1;
            if (lineTo == -1 || lineTo > lines.Length)
                lineTo = lines.Length;
            if (lineTo < 1)
                lineTo = lines.Length;
            if (lineFrom > lineTo)
                lineFrom = lineTo;

            // Convert from 1-based to 0-based indexing
            int startIndex = lineFrom - 1;
            int endIndex = lineTo; // lineTo is inclusive in 1-based, so this works for slice

            return string.Join("\n", lines.Skip(startIndex).Take(endIndex - startIndex));
        }
    }
}
