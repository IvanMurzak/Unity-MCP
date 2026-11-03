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
        [RqlCallable("copy an asset to a new path (not an existing one; missing folders are created); returns the copy")]
        public Object CopyAsset(string path, string newPath)
        {
            newPath = CopyPath(path, newPath, AssetDatabase.IsValidFolder(Normalize(path)));
            return InFolderOf(newPath, () => AssetDatabase.CopyAsset(path, newPath)
                ? AssetDatabase.LoadMainAssetAtPath(newPath)
                : throw new ArgumentException($"could not copy '{path}' to '{newPath}'"));
        }

        // The copy's path: new, with the asset's extension (a folder keeps none, even one named like "com.foo"), and not inside
        // the folder being copied (Unity would copy the new folder into the copy as well).
        private static string CopyPath(string path, string newPath, bool folder)
        {
            newPath = NewAssetPath(newPath, folder ? null : Path.GetExtension(path));
            NotIntoItself("copy", path, newPath, folder);
            return newPath;
        }

        private static void NotIntoItself(string verb, string path, string newPath, bool folder)
        {
            if (folder && newPath.StartsWith(Normalize(path) + "/", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"cannot {verb} the folder '{Normalize(path)}' into itself ('{newPath}')");
        }

        // Call validators (companions RQL finds by name): the rejection is reported for every target before the first call,
        // on dry runs too, so a call that would fail changes nothing (RQL spec §8). They only read; the calls repeat the checks.

        public string? ValidateCopyAsset(string path, string newPath) => Rejection(() =>
        {
            var folder = AssetDatabase.IsValidFolder(Normalize(path));
            if (!folder && AssetDatabase.LoadMainAssetAtPath(Normalize(path)) == null) throw new ArgumentException($"no asset at '{path}'");
            EnsureFolder(Path.GetDirectoryName(CopyPath(path, newPath, folder)), create: false);
        });

        public string? ValidateMoveAsset(string path, string newPath) => Rejection(() =>
        {
            var p = Normalize(path);
            var folder = AssetDatabase.IsValidFolder(p);
            if (!folder && AssetDatabase.LoadMainAssetAtPath(p) == null) throw new ArgumentException($"no asset at '{path}'");
            newPath = Normalize(newPath); // a taken path is Unity's error (a case-only rename is not one)
            NotIntoItself("move", p, newPath, folder);
            EnsureFolder(Path.GetDirectoryName(newPath), create: false);
        });

        public string? ValidateDeleteAsset(string path, bool force) => Rejection(() =>
        {
            var p = Normalize(path);
            if (!AssetDatabase.IsValidFolder(p) && AssetDatabase.LoadMainAssetAtPath(p) == null) throw new ArgumentException($"no asset at '{path}'");
            if (!force && Users(p) is { Count: > 0 } users) throw InUse(p, users);
        });

        public string? ValidateCreateFolder(string path) => Rejection(() => EnsureFolder(path, create: false));

        public string? ValidateCreateMaterial(string path, string? shader) => Rejection(() =>
        {
            path = NewAssetPath(path, ".mat");
            if (shader != null && Shader.Find(shader) == null) throw new ArgumentException($"no shader named '{shader}'");
            EnsureFolder(Path.GetDirectoryName(path), create: false);
        });

        /// <summary>The message a check throws (<see cref="ArgumentException"/>), or null when it passes.</summary>
        public static string? Rejection(Action check)
        {
            try { check(); return null; }
            catch (ArgumentException e) { return e.Message; }
        }

        [RqlCallable("move or rename an asset (missing folders are created); returns its new path")]
        public string MoveAsset(string path, string newPath)
        {
            newPath = Normalize(newPath);
            return InFolderOf(newPath, () =>
                AssetDatabase.MoveAsset(path, newPath) is { Length: > 0 } error ? throw new ArgumentException(error) : newPath);
        }

        [RqlCallable("move an asset (or folder) to the OS trash, so it can be recovered; refuses while objects in the open scenes or prefab use it, unless force = true (their references become missing). " +
                     RqlGuide.DeleteRule)]
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

        [RqlCallable("create a material asset at a new path (\"Assets/…/X.mat\") with a shader (default: the render pipeline's default; missing folders are created); returns it")]
        public Material CreateMaterial(string path, string? shader = null)
        {
            path = NewAssetPath(path, ".mat");
            var s = shader == null ? DefaultShader() : Shader.Find(shader) ?? throw new ArgumentException($"no shader named '{shader}'");
            return SaveNewAsset(new Material(s) { name = Path.GetFileNameWithoutExtension(path) }, path);
        }

        [RqlCallable("write every modified asset to disk")]
        public void SaveAssets() => AssetDatabase.SaveAssets();

        [RqlCallable("import the files changed outside the Editor (call it after editing project files on disk); an open scene whose " +
                     "file changed is reloaded, one with unsaved changes in the Editor is kept and reported; returns what it did")]
        [RqlNoAutoSave] // the save after a mutation would write a dirty scene over the file that changed on disk
        public string RefreshAssets()
        {
            var before = OpenSceneHashes();
            AssetDatabase.Refresh();
            return ReloadChangedScenes(before);
        }

        internal static Shader DefaultShader()
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

        /// <summary>
        /// The new-asset path check of the modules' create calls (and their call validators): under Assets/, with
        /// <paramref name="extension"/>, nothing there yet (imported or on disk), and folders that can be created. The folders
        /// themselves are created by the call (<see cref="SaveNewAsset{T}"/> / <see cref="InFolderOf{T}"/>), so a failed save
        /// can remove them again.
        /// </summary>
        public static string NewModuleAssetPath(string? path, string extension)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is empty");
            path = Normalize(path!);
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException($"'{path}' must start with Assets/");
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"'{path}' must end with {extension}");
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path) || Directory.Exists(path))
                throw new ArgumentException($"an asset already exists at '{path}' (choose another path)");
            EnsureFolder(Path.GetDirectoryName(path), create: false);
            return path;
        }

        /// <summary>
        /// Creates the folder and its missing parents; with <paramref name="create"/> = false it only throws where it would
        /// (call validators, so dry runs report it). Returns the outermost folder it created, or null when the folder exists.
        /// A missing folder whose path is taken on disk (a file, or a folder Unity has not imported yet) is refused before
        /// anything is created: Unity would make "X 1" instead, and a rollback by the requested path would delete the user's folder.
        /// </summary>
        public static string? EnsureFolder(string? path, bool create = true)
        {
            if (string.IsNullOrEmpty(path)) return null;
            path = Normalize(path!);
            if (AssetDatabase.IsValidFolder(path)) return null;
            // Checked before the disk: "Library", "ProjectSettings" … exist on disk but are no asset folders.
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) && !path.StartsWith("Packages/", StringComparison.Ordinal))
                throw new ArgumentException($"'{path}' is not under Assets/");
            var file = File.Exists(path);
            if (file || Directory.Exists(path))
                throw new ArgumentException($"could not create the folder '{path}': it is taken on disk by a " + (file ? "file" :
                    "folder Unity has not imported: a new one (RefreshAssets() imports it), one Unity ignores (\"X~\", \".X\") or one whose name differs in case"));
            var parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent)) throw new ArgumentException($"'{path}' is not under Assets/");
            parent = Normalize(parent);
            var outermost = EnsureFolder(parent, create);
            if (!create) return outermost ?? path;
            // Unity still picks another name ("X 1") when the path differs only in case from an existing folder on a
            // case-sensitive disk: that folder is not the one asked for.
            var made = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)));
            if (made == path) return outermost ?? path;
            if (!string.IsNullOrEmpty(made)) AssetDatabase.DeleteAsset(made);
            if (outermost != null) AssetDatabase.DeleteAsset(outermost);
            throw new ArgumentException($"could not create the folder '{path}' (a file or a folder that is not imported may be in the way)");
        }

        /// <summary>
        /// Runs <paramref name="write"/> after creating the missing folders of <paramref name="assetPath"/>; when it throws, the
        /// folders created here are deleted again, so a failed call leaves no empty folders behind.
        /// </summary>
        public static T InFolderOf<T>(string assetPath, Func<T> write) => InFolder(Path.GetDirectoryName(assetPath), write);

        /// <summary><see cref="InFolderOf{T}"/> for a folder: creates it and its missing parents, removes them when <paramref name="write"/> throws.</summary>
        public static T InFolder<T>(string? folder, Func<T> write)
        {
            var created = EnsureFolder(folder);
            try { return write(); }
            catch
            {
                if (created != null)
                    try { AssetDatabase.DeleteAsset(created); }
                    catch (Exception e) { Debug.LogWarning($"[RQL] could not remove the folder '{created}' after a failed write: {e.Message}"); }
                throw;
            }
        }

        /// <summary>
        /// Saves the in-memory <paramref name="asset"/> as a new asset at <paramref name="path"/> (already checked to be free),
        /// creating the missing folders. When Unity cannot create it, the folders created here are removed and the object is
        /// destroyed, so a failed call leaves nothing behind.
        /// </summary>
        public static T SaveNewAsset<T>(T asset, string path) where T : Object => InFolderOf(path, () =>
        {
            var saved = false;
            try
            {
                AssetDatabase.CreateAsset(asset, path); // throws for most bad paths, only logs for some
                saved = AssetDatabase.Contains(asset);
            }
            finally
            {
                if (!saved && !EditorUtility.IsPersistent(asset)) Object.DestroyImmediate(asset); // an asset cannot be destroyed
            }
            return saved ? asset : throw new ArgumentException($"could not create an asset at '{path}'");
        });

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
