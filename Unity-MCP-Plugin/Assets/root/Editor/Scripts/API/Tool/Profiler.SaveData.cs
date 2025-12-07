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
using UnityEngine;
using UnityEngine.Profiling;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_SaveData",
            Title = "Save Profiler Data"
        )]
        [Description(@"Saves current profiler statistics snapshot to a JSON file.
Note: This saves current statistics snapshot. For full profiler data, use Unity's Profiler window save feature.")]
        public string SaveData
        (
            [Description("The file path to save the profiler data. Should end with '.json'.")]
            string filePath
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(filePath))
                return Error.FilePathIsRequired();

            try
            {
                var data = new
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    enabled = profilerEnabled,
                    memory = new
                    {
                        totalReservedMemoryMB = Profiler.GetTotalReservedMemoryLong() / 1048576f,
                        totalAllocatedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f,
                        monoHeapSizeMB = Profiler.GetMonoHeapSizeLong() / 1048576f,
                        monoUsedSizeMB = Profiler.GetMonoUsedSizeLong() / 1048576f
                    },
                    performance = new
                    {
                        frameTimeMs = Time.deltaTime * 1000f,
                        fps = Time.deltaTime > 0 ? 1f / Time.deltaTime : 0f,
                        frameCount = Time.frameCount
                    }
                };

                var jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(data, jsonOptions);
                File.WriteAllText(filePath, json);

                return $"[Success] Profiler data saved to: {filePath}\nNote: This saves current statistics snapshot. For full profiler data, use Unity's Profiler window save feature.";
            }
            catch (Exception ex)
            {
                return Error.FailedToSaveData(ex.Message);
            }
        });
    }
}

