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
using System.Collections.Generic;
using System.IO;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Rql;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    public sealed partial class RqlEditorApi
    {
        [RqlCallable("copy an asset to a new path (not an existing one); returns the copy")]
        public Object CopyAsset(string path, string newPath)
        {
            newPath = NewAssetPath(newPath, Path.GetExtension(path));
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

        [RqlCallable("move an asset (or folder) to the OS trash, so it can be recovered; refuses while objects in the open scenes or prefab use it, unless force = true (their references become missing)")]
        public void DeleteAsset(string path, bool force = false)
        {
            path = Normalize(path);
            if (!force && Users(path) is { Count: > 0 } users)
                throw new ArgumentException($"'{path}' is used by {users.Count} object(s) in the open scenes: {Preview(users)}. " +
                                            $"Clear those references first, or DeleteAsset(\"{path}\", true) to delete it anyway (the references become missing)");
            if (!AssetDatabase.MoveAssetToTrash(path)) throw new ArgumentException($"could not delete '{path}'");
        }

        [RqlCallable("create a folder (and missing parents), e.g. \"Assets/Art/Materials\"; returns its path")]
        public string CreateFolder(string path)
        {
            EnsureFolder(path);
            return Normalize(path);
        }

        [RqlCallable("create a material asset at a new path (\"Assets/…/X.mat\") with a shader (default: the render pipeline's default); returns it")]
        public Material CreateMaterial(string path, string? shader = null)
        {
            path = NewAssetPath(path, ".mat");
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

        internal static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');

        /// <summary>
        /// <paramref name="path"/> normalized, after checking that it is free and has <paramref name="extension"/> (when one is
        /// given). Unity's create / copy / save calls replace an existing asset silently, which breaks every reference to it.
        /// </summary>
        internal static string NewAssetPath(string path, string? extension)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("a path such as \"Assets/Folder/Name" + extension + "\" is required");
            path = Normalize(path);
            if (!string.IsNullOrEmpty(extension) && !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"'{path}' must end with {extension}");
            if (AssetDatabase.IsValidFolder(path) || File.Exists(path))
                throw new ArgumentException($"'{path}' already exists: pick a new path, or DeleteAsset(\"{path}\") first");
            return path;
        }

        internal static void EnsureFolder(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            path = Normalize(path!);
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent)) throw new ArgumentException($"'{path}' is not under Assets/");
            parent = Normalize(parent);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>
        /// The GameObjects of the open scenes and prefab that use the asset at <paramref name="path"/> (for a folder: any asset
        /// in it): a prefab instance of it, or a component field that references it. Other project assets are not searched.
        /// </summary>
        internal static List<string> Users(string path)
        {
            bool Inside(string p) => p == path || p.StartsWith(path + "/", StringComparison.Ordinal);
            var objects = RqlUnity.AllGameObjects();
            if (OpenPrefabRoot() is GameObject prefabRoot)
                foreach (var t in prefabRoot.GetComponentsInChildren<Transform>(true)) objects.Add(t.gameObject);
            var users = new List<string>();
            var components = new List<Component>();
            foreach (var go in objects)
                if (Uses(go, Inside, components)) users.Add(go.GetPath() ?? go.name);
            return users;
        }

        private static bool Uses(GameObject go, Func<string, bool> inside, List<Component> components)
        {
            if (PrefabUtility.IsOutermostPrefabInstanceRoot(go) && inside(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go))) return true;
            go.GetComponents(components);
            foreach (var c in components)
                if (c != null && References(c, inside)) return true;
            return false;
        }

        private static bool References(Component c, Func<string, bool> inside)
        {
            using var so = new SerializedObject(c);
            var it = so.GetIterator();
            while (it.Next(true))
                if (it.propertyType == SerializedPropertyType.ObjectReference && !PrefabLinks.Contains(it.name)
                    && it.objectReferenceValue is Object o && o != null
                    && EditorUtility.IsPersistent(o) && inside(AssetDatabase.GetAssetPath(o)))
                    return true;
            return false;
        }

        // Every object inside a prefab instance links to its source through these: the instance root already reports the use.
        private static readonly HashSet<string> PrefabLinks = new HashSet<string> { "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset" };

        private static string Preview(List<string> names) =>
            names.Count <= 5 ? string.Join(", ", names) : string.Join(", ", names.GetRange(0, 5)) + $" … (+{names.Count - 5})";
    }
}
