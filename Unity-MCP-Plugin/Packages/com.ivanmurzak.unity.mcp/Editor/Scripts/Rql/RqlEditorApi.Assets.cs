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
using System;
using System.IO;
using Rql;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    public sealed partial class RqlEditorApi
    {
        [RqlCallable("copy an asset to a new path; returns the copy")]
        public Object CopyAsset(string path, string newPath)
        {
            EnsureFolder(Path.GetDirectoryName(newPath));
            if (!AssetDatabase.CopyAsset(path, newPath)) throw new ArgumentException($"could not copy '{path}' to '{newPath}'");
            return AssetDatabase.LoadMainAssetAtPath(newPath);
        }

        [RqlCallable("move or rename an asset; returns its new path")]
        public string MoveAsset(string path, string newPath)
        {
            EnsureFolder(Path.GetDirectoryName(newPath));
            var error = AssetDatabase.MoveAsset(path, newPath);
            if (!string.IsNullOrEmpty(error)) throw new ArgumentException(error);
            return newPath;
        }

        [RqlCallable("move an asset (or folder) to the OS trash, so it can be recovered")]
        public void DeleteAsset(string path)
        {
            if (!AssetDatabase.MoveAssetToTrash(path)) throw new ArgumentException($"could not delete '{path}'");
        }

        [RqlCallable("create a folder (and missing parents), e.g. \"Assets/Art/Materials\"; returns its path")]
        public string CreateFolder(string path)
        {
            EnsureFolder(path);
            return path;
        }

        [RqlCallable("create a material asset at path (\"Assets/…/X.mat\") with a shader (default: the render pipeline's default); returns it")]
        public Material CreateMaterial(string path, string? shader = null)
        {
            var s = shader == null ? DefaultShader() : Shader.Find(shader) ?? throw new ArgumentException($"no shader named '{shader}'");
            EnsureFolder(Path.GetDirectoryName(path));
            var material = new Material(s) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        [RqlCallable("write every modified asset to disk")]
        public void SaveAssets() => AssetDatabase.SaveAssets();

        [RqlCallable("re-import changed files (after writing files outside the Editor)")]
        public void RefreshAssets() => AssetDatabase.Refresh();

        private static Shader DefaultShader()
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            return pipeline != null && pipeline.defaultShader != null ? pipeline.defaultShader : Shader.Find("Standard");
        }

        private static void EnsureFolder(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            path = path!.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) throw new ArgumentException($"'{path}' is not under Assets/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
