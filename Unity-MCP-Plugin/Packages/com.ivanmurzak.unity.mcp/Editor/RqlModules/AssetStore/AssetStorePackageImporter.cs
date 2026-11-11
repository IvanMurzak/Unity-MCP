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
using System.Text.RegularExpressions;
using Rql;
using UnityEditor;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    [RqlDescription("what an Asset Store package import brought into the project")]
    public sealed class AssetStoreImportResult
    {
        [RqlDescription("the package's name")]
        public string package { get; internal set; } = "";

        [RqlDescription("imported; queued (the Editor finishes it on its next update: query again in a moment); or awaiting-user " +
                        "(Unity asks the user to confirm the package first, see notes; poll with imported / unity.asset(path))")]
        public string status { get; internal set; } = "";

        [RqlDescription("what the agent does next when the import waits for the user (status awaiting-user), else null")]
        public string? next { get; internal set; }

        [RqlDescription("files in the package")]
        public int files { get; internal set; }

        [RqlDescription("files that were already in the project (same GUID) and now hold the package's version")]
        public int replaced { get; internal set; }

        [RqlDescription("the top folders it imported into, e.g. \"Assets/Publisher/Pack\"")]
        public List<string> folders { get; internal set; } = new List<string>();

        [RqlDescription("prefab paths (first 40; Contents(\".prefab\") lists more): place one with editor | call InstantiatePrefab(path, {position: [x, y, z]})")]
        public List<string> prefabs { get; internal set; } = new List<string>();

        [RqlDescription("material paths (first 40)")]
        public List<string> materials { get; internal set; } = new List<string>();

        [RqlDescription("scene paths (demo scenes; first 20)")]
        public List<string> scenes { get; internal set; } = new List<string>();

        [RqlDescription("model files: .fbx .obj .blend … (first 40)")]
        public List<string> models { get; internal set; } = new List<string>();

        [RqlDescription("C# scripts imported (they compile right after the import)")]
        public int scripts { get; internal set; }

        [RqlDescription("textures imported")]
        public int textures { get; internal set; }

        [RqlDescription("what to know before using it: render pipeline mismatch, scripts compiling, nested packages …")]
        public List<string> notes { get; internal set; } = new List<string>();
    }

    /// <summary>Imports a .unitypackage without the dialog and reports what it brought, from the package's own table of contents.</summary>
    public static class AssetStorePackageImporter
    {
        static readonly string[] ModelExtensions = { ".fbx", ".obj", ".blend", ".dae", ".3ds", ".max", ".ma", ".mb", ".gltf", ".glb" };
        static readonly string[] TextureExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".exr", ".hdr", ".bmp", ".gif" };

        public static string? Rejection(Action check)
        {
            try { check(); return null; }
            catch (ArgumentException e) { return e.Message; }
            catch (InvalidOperationException e) { return e.Message; }
            catch (IOException e) { return e.Message; }
        }

        /// <summary>Throws when the package cannot be imported now, or would overwrite Project Settings unasked.</summary>
        public static void Check(string file, bool allowProjectSettings)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("importing a package is not possible in Play Mode (editor | set isPlaying = false)");
            if (EditorApplication.isCompiling)
                throw new InvalidOperationException("scripts are compiling: retry the import when they finished");
            var entries = UnityPackageArchive.ReadEntries(file);
            if (!allowProjectSettings)
            {
                var settings = entries.Where(e => e.isFile && e.path.StartsWith("ProjectSettings/", StringComparison.OrdinalIgnoreCase)).Select(e => e.path).ToList();
                if (settings.Count > 0)
                    throw new ArgumentException($"the package would overwrite {settings.Count} Project Settings file(s) ({string.Join(", ", settings.Take(4))}): " +
                                                "ask the user first, then Import(true) (or Import({allowProjectSettings: true}))");
            }
        }

        /// <summary>
        /// Unity 6.6 checks a package's signature before importing it. Asset Store .unitypackage files carry none, so a
        /// windowed Editor shows the modal "Missing signature" window and waits for the user (Import Anyway / Cancel); a
        /// batch-mode Editor imports without asking. The window holds the Editor's main thread, so no tool call is answered
        /// while it is open. Test seam.
        /// </summary>
        internal static Func<bool> AsksBeforeImport = () =>
#if UNITY_6000_6_OR_NEWER
            !UnityEngine.Application.isBatchMode;
#else
            false;
#endif

        /// <summary>Test seam: starts Unity's import of a .unitypackage file, without Unity's import dialog.</summary>
        internal static Action<string> StartImport = file =>
#if UNITY_6000_6_OR_NEWER
            UnityEditor.AssetPackage.Package.Import(file, false);
#else
            AssetDatabase.ImportPackage(file, false);
#endif

        internal const string AwaitingUser = "awaiting-user";

        public static AssetStoreImportResult Import(string packageName, string file, bool allowProjectSettings)
        {
            Check(file, allowProjectSettings);
            var entries = UnityPackageArchive.ReadEntries(file);
            var assetFiles = entries.Where(e => e.isFile).ToList();
            var replaced = assetFiles.Count(e => !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(e.guid)) && File.Exists(AssetDatabase.GUIDToAssetPath(e.guid)));

            if (AsksBeforeImport())
            {
                // Started after this call answered, so Unity's confirmation window never holds the agent's call.
                var start = StartImport; // as set now: a test that swapped it restores it before the next update
                AssetStoreEditorUpdate.Next(() => start(file));
                var pending = Describe(packageName, assetFiles, replaced, AwaitingUser);
                pending.next = "Unity checks the package's signature first and Asset Store packages have none: it shows " +
                    "\"Missing signature\" and waits for the user, and until they answer the Editor answers no tool call. Ask the user " +
                    "to review it and press Import Anyway (or Cancel); the agent never presses Import Anyway. Then assetStore | get " +
                    "packages | where name == \"" + packageName + "\" | select imported (or unity.asset(path) of a file listed here) " +
                    "shows it arrived. Nobody at the Editor: unity-mcp-cli dialog --press Cancel closes the window, nothing is imported";
                return pending;
            }

            StartImport(file);

            var probe = assetFiles.FirstOrDefault(e => e.path.StartsWith("Assets/", StringComparison.Ordinal));
            var imported = probe == null || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(probe.path, AssetPathToGUIDOptions.OnlyExistingAssets));
            return Describe(packageName, assetFiles, replaced, imported ? "imported" : "queued");
        }

        /// <summary>Every file of the package is in the project (by GUID): its import arrived.</summary>
        public static bool IsImported(string file)
        {
            var files = UnityPackageArchive.ReadEntries(file).Where(e => e.isFile).ToList();
            return files.Count > 0 && files.All(e => !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(e.guid)) &&
                                                      File.Exists(AssetDatabase.GUIDToAssetPath(e.guid)));
        }

        /// <summary>The import report for a package's files (exposed for tests).</summary>
        public static AssetStoreImportResult Describe(string packageName, List<UnityPackageEntry> assetFiles, int replaced, string status)
        {
            static bool Ext(UnityPackageEntry e, params string[] extensions)
                => extensions.Any(x => e.path.EndsWith(x, StringComparison.OrdinalIgnoreCase));

            var result = new AssetStoreImportResult
            {
                package = packageName,
                status = status,
                files = assetFiles.Count,
                replaced = replaced,
                folders = TopFolders(assetFiles.Select(e => e.path)),
                prefabs = assetFiles.Where(e => Ext(e, ".prefab")).Select(e => e.path).Take(40).ToList(),
                materials = assetFiles.Where(e => Ext(e, ".mat")).Select(e => e.path).Take(40).ToList(),
                scenes = assetFiles.Where(e => Ext(e, ".unity")).Select(e => e.path).Take(20).ToList(),
                models = assetFiles.Where(e => Ext(e, ModelExtensions)).Select(e => e.path).Take(40).ToList(),
                scripts = assetFiles.Count(e => Ext(e, ".cs")),
                textures = assetFiles.Count(e => Ext(e, TextureExtensions))
            };

            var made = PackagePipeline(packageName, assetFiles);
            var project = ProjectPipeline();
            if (made != null && made != project)
                result.notes.Add($"made for {made}, but this project renders with {project}: its materials will show magenta until " +
                                 $"converted (or switch the project to {made})");
            if (result.scripts > 0)
                result.notes.Add($"{result.scripts} scripts: the Editor compiles them now and reloads scripts (retry a call that fails right " +
                                 "after; console | where logType == \"Error\" shows compile errors)");
            var nested = assetFiles.Where(e => Ext(e, ".unitypackage")).Select(e => e.path).Take(10).ToList();
            if (nested.Count > 0)
                result.notes.Add("it ships nested packages (often render-pipeline or sample variants): " + string.Join(", ", nested) +
                                 "; import one with assetStore | call ImportFile(path)");
            if (replaced > 0)
                result.notes.Add($"{replaced} files already in the project now hold the package's version");
            if (assetFiles.Count > 0 && result.prefabs.Count == 0 && result.models.Count == 0 && result.scripts == 0)
                result.notes.Add("no prefabs, models or scripts: see Contents() for what it holds");
            return result;
        }

        static List<string> TopFolders(IEnumerable<string> paths)
        {
            // Two levels under Assets/ ("Assets/Publisher/Pack"), one for anything else ("Packages/x").
            return paths.Select(p =>
                {
                    var parts = p.Split('/');
                    var depth = parts[0] == "Assets" ? 3 : 2;
                    return parts.Length > depth ? string.Join("/", parts.Take(depth)) : string.Join("/", parts.Take(parts.Length - 1));
                })
                .Where(f => f.Length > 0)
                .GroupBy(f => f)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(8)
                .ToList();
        }

        static readonly Regex Hdrp = new Regex(@"(^|[^A-Za-z])HDRP([^A-Za-z]|$)", RegexOptions.Compiled);
        static readonly Regex Urp = new Regex(@"(^|[^A-Za-z])(URP|LWRP)([^A-Za-z]|$)", RegexOptions.Compiled);

        /// <summary>The render pipeline a package names itself for ("Free Fire VFX - URP"), or null.</summary>
        public static string? PackagePipeline(string packageName, List<UnityPackageEntry> assetFiles)
        {
            if (Hdrp.IsMatch(packageName)) return "HDRP";
            if (Urp.IsMatch(packageName)) return "URP";
            // Only a pipeline that every material folder agrees on: packs that ship variants for several say both.
            var materialPaths = assetFiles.Where(e => e.path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)).Select(e => e.path).ToList();
            if (materialPaths.Count == 0) return null;
            var hd = materialPaths.Count(p => Hdrp.IsMatch(p));
            var ur = materialPaths.Count(p => Urp.IsMatch(p));
            if (hd == materialPaths.Count) return "HDRP";
            if (ur == materialPaths.Count) return "URP";
            return null;
        }

        static string ProjectPipeline()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null) return "Built-in";
            var type = pipeline.GetType().Name;
            if (type.Contains("Universal")) return "URP";
            if (type.Contains("HDRender")) return "HDRP";
            return type;
        }
    }
}
