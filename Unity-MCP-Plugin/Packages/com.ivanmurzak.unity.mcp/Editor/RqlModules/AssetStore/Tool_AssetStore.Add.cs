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
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    public sealed partial class Tool_AssetStore
    {
        [AiTool(AddToolId, Title = "Asset Store / Add", ReadOnlyHint = false, DestructiveHint = false, IdempotentHint = false, OpenWorldHint = true, Visible = true)]
        [Description("Import a downloaded Asset Store package into the project, all of it, without the import dialog. Returns its prefabs, " +
            "materials, models, scenes and notes (render pipeline, scripts compiling, nested pipeline packages). Status 'awaiting-user': " +
            "Unity shows 'Missing signature' (Asset Store packages are unsigned) and waits for the user's Import Anyway; ask the user, " +
            "never press it yourself, then check '" + LibraryToolId + "' Imported (or ImportError). If the user cancels, stop: never import an Asset Store " +
            "package another way (script-execute, AssetDatabase.ImportPackage, unpacking the .unitypackage). Find the productId with '" + LibraryToolId + "'.")]
        public AssetStoreImportResult Add
        (
            [Description("The package's productId from '" + LibraryToolId + "', or its exact name.")]
            string package,
            [Description("Also let the package overwrite ProjectSettings (tags, layers, input). Only after the user agreed. Default: false.")]
            bool allowProjectSettings = false
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(package))
                    throw new ArgumentException("package is empty: pass a productId or a name from " + LibraryToolId);
                var key = package.Trim();
                var all = AssetStoreEditorApi.Instance.packages;
                // A cache-only download has productId 0, shared by all of them: it is chosen by name, never by that id.
                var match = long.TryParse(key, out var id) && id > 0
                    ? all.FirstOrDefault(p => p.productId == id)
                    : all.FirstOrDefault(p => string.Equals(p.name, key, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                    throw new ArgumentException($"no Asset Store package '{key}' in the account or the cache: find it with {LibraryToolId} " +
                                                "(a package with productId 0, a download the account does not list, is passed by its name)");
                return match.Import(allowProjectSettings, $"{LibraryToolId} with query \"{match.name}\" (Imported: true)");
            });
        }
    }
}
