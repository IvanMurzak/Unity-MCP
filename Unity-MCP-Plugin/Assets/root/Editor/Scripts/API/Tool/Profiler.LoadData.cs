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
using System.ComponentModel;
using System.IO;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_LoadData",
            Title = "Load Profiler Data"
        )]
        [Description(@"Loads profiler data from a JSON file.
Note: To load full profiler captures, use Unity's Profiler window load feature.")]
        public string LoadData
        (
            [Description("The file path to load the profiler data from.")]
            string filePath
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(filePath))
                return Error.FilePathIsRequired();

            if (!File.Exists(filePath))
                return Error.FileNotFound(filePath);

            try
            {
                var json = File.ReadAllText(filePath);

                return $"[Success] Profiler data loaded from: {filePath}\nNote: To load full profiler captures, use Unity's Profiler window load feature.\n\nData:\n{json}";
            }
            catch (Exception ex)
            {
                return Error.FailedToLoadData(ex.Message);
            }
        });
    }
}

