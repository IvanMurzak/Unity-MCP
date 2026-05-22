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
        public const string ProfilerLoadDataToolId = "profiler-load-data";

        // Cap to keep an accidental call against a huge file from OOMing the Editor.
        // Snapshots written by SaveData are typically a few KB; 10 MB leaves plenty of
        // headroom for future schema expansion while bounding worst-case memory.
        const long MaxLoadFileSizeBytes = 10L * 1024L * 1024L;

        [McpPluginTool
        (
            ProfilerLoadDataToolId,
            Title = "Profiler / Load Data",
            ReadOnlyHint = true,
            Enabled = false
        )]
        [McpPluginSkillDescription("Read back a previously-saved JSON snapshot from `profiler-save-data` and return its raw text.")]
        [McpPluginSkillBody("Reads `filePath` as UTF-8 text and returns the file body unchanged. Caller is responsible " +
            "for parsing.\n\n" +
            "## Inputs\n\n" +
            "- `filePath` (required) — path written by `profiler-save-data`.\n\n" +
            "## Errors\n\n" +
            "- Returns `[Error]` when `filePath` is empty, the file does not exist, exceeds the 10 MB size cap, or the read fails.\n\n" +
            "## Behavior\n\n" +
            "Uses `System.IO.File.ReadAllText` (BCL) — no external Unity package is required. " +
            "Files larger than 10 MB are rejected up-front to avoid OOM on a stray call.")]
        [Description("Reads a profiler snapshot JSON file and returns its raw text content.")]
        public string LoadData
        (
            [Description("Path to a profiler snapshot file previously written by 'profiler-save-data'.")]
            string filePath
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(filePath))
                    return Error.FilePathIsRequired();

                if (!File.Exists(filePath))
                    return Error.FileNotFound(filePath);

                try
                {
                    var info = new FileInfo(filePath);
                    if (info.Length > MaxLoadFileSizeBytes)
                        return Error.FileTooLarge(filePath, info.Length, MaxLoadFileSizeBytes);

                    var content = File.ReadAllText(filePath);
                    return content;
                }
                catch (Exception ex)
                {
                    return Error.FailedToLoadData(ex.Message);
                }
            });
        }
    }
}
