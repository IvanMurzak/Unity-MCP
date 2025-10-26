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
using System.IO;
using System.Text.Json;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin
    {
        public static string ResourcesFileName => "Unity-MCP-ConnectionConfig";
        public static string AssetsFilePath => $"Assets/Resources/{ResourcesFileName}.json";
#if UNITY_EDITOR
        public static TextAsset AssetFile => UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(AssetsFilePath);
        public static void InvalidateAssetFile() => UnityEditor.AssetDatabase.ImportAsset(AssetsFilePath, UnityEditor.ImportAssetOptions.ForceUpdate);
#endif

        static UnityMcpPlugin GetOrCreateInstance() => GetOrCreateInstance(out _);
        static UnityMcpPlugin GetOrCreateInstance(out bool wasCreated)
        {
            wasCreated = false;
            try
            {
#if UNITY_EDITOR
                var json = Application.isPlaying
                    ? Resources.Load<TextAsset>(ResourcesFileName).text
                    : File.Exists(AssetsFilePath)
                        ? File.ReadAllText(AssetsFilePath)
                        : null;
#else
                var json = Resources.Load<TextAsset>(ResourcesFileName).text;
#endif
                Data? config = null;
                try
                {
                    config = string.IsNullOrWhiteSpace(json)
                        ? null
                        : JsonSerializer.Deserialize<Data>(json);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError($"{DebugName} <color=red><b>{ResourcesFileName}</b> file is corrupted at <i>{AssetsFilePath}</i></color>");
                }
                if (config == null)
                {
                    Debug.Log($"{DebugName} <color=orange><b>Creating {ResourcesFileName}</b> file at <i>{AssetsFilePath}</i></color>");
                    config = new Data();
                    wasCreated = true;
                }
                return new UnityMcpPlugin() { data = config };
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"{DebugName} <color=red><b>{ResourcesFileName}</b> file can't be loaded from <i>{AssetsFilePath}</i></color>");
            }
            throw new InvalidOperationException($"Failed to get or create {nameof(UnityMcpPlugin)} instance. Check logs for details.");
        }

        public static void Save()
        {
#if UNITY_EDITOR
            Validate();
            try
            {
                var directory = Path.GetDirectoryName(AssetsFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var data = Instance.data ??= new Data();
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AssetsFilePath, json);

                var assetFile = AssetFile;
                if (assetFile != null)
                    UnityEditor.EditorUtility.SetDirty(assetFile);
                else
                    UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception e)
            {
                Debug.LogError($"{DebugName} <color=red><b>{ResourcesFileName}</b> file can't be saved at <i>{AssetsFilePath}</i></color>");
                Debug.LogException(e);
            }
#else
            return;
#endif
        }
    }
}
