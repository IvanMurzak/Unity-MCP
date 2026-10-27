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
using System.Linq;
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
            // A folder keeps no extension, even one named like "com.foo".
            newPath = NewAssetPath(newPath, AssetDatabase.IsValidFolder(Normalize(path)) ? null : Path.GetExtension(path));
            EnsureFolder(Path.GetDirectoryName(newPath));
            if (!AssetDatabase.CopyAsset(path, newPath)) throw new ArgumentException($"could not copy '{path}' to '{newPath}'");
            return AssetDatabase.LoadMainAssetAtPath(newPath);
        }

        // Call validators (companions RQL finds by name): the rejection is reported for every target before the first call,
        // on dry runs too, so a call that would fail changes nothing (RQL spec §8). They only read; the calls repeat the checks.

        public string? ValidateCopyAsset(string path, string newPath) => Rejection(() =>
        {
            var folder = AssetDatabase.IsValidFolder(Normalize(path));
            if (!folder && AssetDatabase.LoadMainAssetAtPath(Normalize(path)) == null) throw new ArgumentException($"no asset at '{path}'");
            CheckFolder(Path.GetDirectoryName(NewAssetPath(newPath, folder ? null : Path.GetExtension(path))));
        });

        public string? ValidateDeleteAsset(string path, bool force) => Rejection(() =>
        {
            var p = Normalize(path);
            if (!AssetDatabase.IsValidFolder(p) && AssetDatabase.LoadMainAssetAtPath(p) == null) throw new ArgumentException($"no asset at '{path}'");
            if (!force && Users(p) is { Count: > 0 } users) throw InUse(p, users);
        });

        public string? ValidateCreateMaterial(string path, string? shader) => Rejection(() =>
        {
            path = NewAssetPath(path, ".mat");
            if (shader != null && Shader.Find(shader) == null) throw new ArgumentException($"no shader named '{shader}'");
            CheckFolder(Path.GetDirectoryName(path));
        });

        /// <summary>The message a check throws (<see cref="ArgumentException"/>), or null when it passes.</summary>
        internal static string? Rejection(Action check)
        {
            try { check(); return null; }
            catch (ArgumentException e) { return e.Message; }
        }

        /// <summary>Throws where <see cref="EnsureFolder"/> would (a path not under Assets/), without creating anything.</summary>
        internal static void CheckFolder(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            for (path = Normalize(path!); !AssetDatabase.IsValidFolder(path); path = Normalize(path))
            {
                var parent = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(parent)) throw new ArgumentException($"'{path}' is not under Assets/");
                path = parent;
            }
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
            if (!force && Users(path) is { Count: > 0 } users) throw InUse(path, users);
            if (!AssetDatabase.MoveAssetToTrash(path)) throw new ArgumentException($"could not delete '{path}'");
        }

        static ArgumentException InUse(string path, List<string> users) =>
            new ArgumentException($"'{path}' is used by {users.Count} object(s) in the open scenes: {Preview(users)}. " +
                                  $"Clear those references first, or DeleteAsset(\"{path}\", true) to delete it anyway (the references become missing)");

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
            // File.Exists alone misses assets of a local "file:" package, whose Packages/ path is not a project file.
            if (AssetDatabase.IsValidFolder(path) || File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
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
            var prefabUses = new Dictionary<string, bool>(); // prefab asset path -> it or its dependencies are inside
            foreach (var go in objects)
                if (Uses(go, Inside, components, prefabUses)) users.Add(go.GetPath() ?? go.name);
            return users;
        }

        private static bool Uses(GameObject go, Func<string, bool> inside, List<Component> components, Dictionary<string, bool> prefabUses)
        {
            // An instance uses its prefab and everything the prefab depends on: nested prefabs, a variant's base, its materials.
            if (PrefabUtility.IsOutermostPrefabInstanceRoot(go) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) is { Length: > 0 } prefab)
            {
                if (!prefabUses.TryGetValue(prefab, out var uses))
                    prefabUses[prefab] = uses = AssetDatabase.GetDependencies(prefab, recursive: true).Any(inside);
                if (uses) return true;
            }
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
