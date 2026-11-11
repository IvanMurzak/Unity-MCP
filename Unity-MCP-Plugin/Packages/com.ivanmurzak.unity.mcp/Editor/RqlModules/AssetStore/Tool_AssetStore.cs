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
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    [Description("Asset Store packages of the signed-in account and the local download cache.")]
    public sealed class AssetStoreLibraryResult
    {
        [Description("A Unity account is signed in to the Editor (owned packages need it; the local cache works without).")]
        public bool SignedIn { get; set; }

        [Description("Why the owned packages could not be read (sign-in, network); null when they were.")]
        public string? AccountError { get; set; }

        [Description("Packages matching the query, before maxResults.")]
        public int Total { get; set; }

        public List<AssetStoreLibraryItem> Packages { get; set; } = new List<AssetStoreLibraryItem>();
    }

    public sealed class AssetStoreLibraryItem
    {
        public long ProductId { get; set; }
        public string Name { get; set; } = "";
        public string? Publisher { get; set; }
        public string? Category { get; set; }
        [Description("In the signed-in account's My Assets.")]
        public bool Owned { get; set; }
        [Description("In the local cache: assetstore-add can import it. False: the user downloads it in the Package Manager first.")]
        public bool Downloaded { get; set; }
        [Description("Already in this project; null when not checked: only a result of up to 5 rows is checked (it reads the package file), ask with a narrower query.")]
        public bool? Imported { get; set; }
        public double SizeMB { get; set; }
    }

    [AiToolType]
    public sealed class Tool_AssetStore
    {
        public const string LibraryToolId = "assetstore-library";
        public const string AddToolId = "assetstore-add";

        /// <summary>Rows whose Imported is checked: each check reads the package file's table of contents.</summary>
        const int MaxImportedChecks = 5;

        [AiTool(LibraryToolId, Title = "Asset Store / Library", ReadOnlyHint = true, DestructiveHint = false, IdempotentHint = true, OpenWorldHint = true, Visible = true)]
        [Description("List the Unity Asset Store packages the user owns (My Assets, read with the Editor's Unity sign-in) and the ones " +
            "already downloaded to the local cache, filtered by words in the name, publisher or category. Each row says whether it " +
            "is downloaded (importable now) and already imported. Then call '" + AddToolId + "' with its productId.")]
        public AssetStoreLibraryResult Library
        (
            [Description("Words that must all appear in the name, publisher or category (case-insensitive), e.g. \"chess\", \"low poly tree\". Empty: everything.")]
            string? query = null,
            [Description("Only packages already downloaded to the local cache. Default: false.")]
            bool onlyDownloaded = false,
            [Description("Maximum rows. Default: 20.")]
            int maxResults = 20
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var api = AssetStoreEditorApi.Instance;
                var words = (query ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var matches = api.packages
                    .Where(p => !onlyDownloaded || p.downloaded)
                    .Where(p =>
                    {
                        var text = $"{p.name} {p.publisher} {p.category}";
                        return words.All(w => text.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
                    })
                    .ToList();
                var rows = matches.Take(Math.Max(1, maxResults)).ToList();
                var checkImported = rows.Count <= MaxImportedChecks;
                return new AssetStoreLibraryResult
                {
                    SignedIn = api.signedIn,
                    AccountError = api.accountError,
                    Total = matches.Count,
                    Packages = rows.Select(p => new AssetStoreLibraryItem
                    {
                        ProductId = p.productId,
                        Name = p.name,
                        Publisher = p.publisher,
                        Category = p.category,
                        Owned = p.owned,
                        Downloaded = p.downloaded,
                        Imported = !p.downloaded ? false : checkImported ? p.imported : (bool?)null,
                        SizeMB = p.sizeMB
                    }).ToList()
                };
            });
        }

        [AiTool(AddToolId, Title = "Asset Store / Add", ReadOnlyHint = false, DestructiveHint = false, IdempotentHint = false, OpenWorldHint = true, Visible = true)]
        [Description("Import a downloaded Asset Store package into the project, all of it, without the import dialog. Returns its prefabs, " +
            "materials, models, scenes and notes (render pipeline, scripts compiling, nested pipeline packages). Status 'awaiting-user': " +
            "Unity shows 'Missing signature' (Asset Store packages are unsigned) and waits for the user's Import Anyway; ask the user, " +
            "never press it yourself, then check '" + LibraryToolId + "' Imported. If the user cancels, stop: never import an Asset Store " +
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
                var match = long.TryParse(key, out var id)
                    ? all.FirstOrDefault(p => p.productId == id)
                    : all.FirstOrDefault(p => string.Equals(p.name, key, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                    throw new ArgumentException($"no Asset Store package '{key}' in the account or the cache: find it with {LibraryToolId}");
                return match.Import(allowProjectSettings, $"{LibraryToolId} with query \"{match.name}\" (Imported: true)");
            });
        }
    }
}
