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
using Rql;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    [RqlDescription("an Asset Store package: owned by the signed-in account and / or downloaded to the local cache")]
    [RqlNoAutoSave]
    public sealed class AssetStorePackage
    {
        [RqlDescription("the Asset Store product id (0 for a cached download the signed-in account does not list)")]
        public long productId { get; private set; }

        [RqlDescription("the package's name")]
        public string name { get; private set; } = "";

        [RqlDescription("the publisher (known once downloaded: from the cache folder)")]
        public string? publisher { get; private set; }

        [RqlDescription("the store category (known once downloaded), e.g. \"3D ModelsProps\"")]
        public string? category { get; private set; }

        [RqlDescription("in the signed-in account's My Assets")]
        public bool owned { get; private set; }

        [RqlDescription("in the local cache, ready to Import(); false: download it first (OpenInPackageManager())")]
        public bool downloaded => localPath != null;

        [RqlDescription("size of the downloaded .unitypackage, megabytes (0 when not downloaded)")]
        public double sizeMB { get; private set; }

        [RqlDescription("when the account got it (ISO 8601), null for a cache-only package")]
        public string? purchasedAt { get; private set; }

        [RqlDescription("the account's labels on it")]
        public List<string> tags { get; private set; } = new List<string>();

        [RqlDescription("hidden in My Assets")]
        public bool hidden { get; private set; }

        [RqlDescription("the package's Asset Store page")]
        public string? storeUrl => productId > 0 ? $"https://assetstore.unity.com/packages/slug/{productId}" : null;

        [RqlDescription("the downloaded .unitypackage file, null when not downloaded")]
        public string? localPath { get; private set; }

        internal static AssetStorePackage Owned(AssetStorePurchase purchase, AssetStoreCachedFile? file)
        {
            var package = new AssetStorePackage
            {
                productId = purchase.productId,
                name = purchase.displayName,
                owned = true,
                purchasedAt = purchase.purchasedAt,
                tags = purchase.tags,
                hidden = purchase.hidden
            };
            if (file != null) package.UseFile(file);
            return package;
        }

        internal static AssetStorePackage CachedOnly(AssetStoreCachedFile file)
        {
            var package = new AssetStorePackage { name = file.name };
            package.UseFile(file);
            return package;
        }

        void UseFile(AssetStoreCachedFile file)
        {
            localPath = file.path;
            publisher = file.publisher;
            category = file.category;
            sizeMB = Math.Round(file.sizeBytes / (1024.0 * 1024.0), 1);
        }

        // ------------------------------------------------------------ operations

        [RqlCallable("the project paths the downloaded package would import, without importing it: files only, those whose path " +
                     "contains filter (e.g. \".prefab\", \"Materials/\"; empty: all), at most max")]
        public List<string> Contents(string filter = "", int max = 200)
        {
            var entries = UnityPackageArchive.ReadEntries(RequireFile());
            return entries.Where(e => e.isFile && (string.IsNullOrEmpty(filter) || e.path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
                          .Take(Math.Max(1, max))
                          .Select(e => e.path)
                          .ToList();
        }

        [RqlCallable("import the downloaded package into the project, all of it, without the import dialog (the same as Import " +
                     "in the Package Manager); returns what it imported: prefabs, materials, scenes, models and notes (render " +
                     "pipeline, scripts compiling). allowProjectSettings: also let it overwrite ProjectSettings (tags, layers, input …)")]
        public AssetStoreImportResult Import(bool allowProjectSettings = false)
            => AssetStorePackageImporter.Import(name, RequireFile(), allowProjectSettings);

        // Call validator (RQL runs it before the call, on dry runs too).
        public string? ValidateImport(bool allowProjectSettings = false)
            => AssetStorePackageImporter.Rejection(() => AssetStorePackageImporter.Check(RequireFile(), allowProjectSettings));

        [RqlCallable("open the Package Manager window on this package, where the user presses Download or " +
                     "Update; afterwards assetStore | call Refresh() lists it as downloaded; returns what to do next")]
        public string OpenInPackageManager()
        {
            UnityEditor.PackageManager.UI.Window.Open(productId > 0 ? productId.ToString() : name);
            return downloaded
                ? $"the Package Manager shows '{name}'; it is already downloaded: call Import()"
                : $"the Package Manager shows '{name}': press Download there (My Assets), then assetStore | call Refresh() and Import()" +
                  (AssetStoreEditorApi.AccessToken().Length == 0 ? SignInFirst : "");
        }

        /// <summary>Appended to a download hint while the Editor has no Unity account signed in (My Assets needs one).</summary>
        internal const string SignInFirst = "; the Editor is not signed in to a Unity account, which My Assets needs: " +
                                            "assetStore | call SignIn() first";

        /// <summary>The package file to read, or why there is none (with the way to get it).</summary>
        string RequireFile()
        {
            if (localPath != null && File.Exists(localPath))
                return localPath;

            // The name match against the cache can miss a renamed package: ask the store where its download lands.
            if (owned && productId > 0)
            {
                var token = AssetStoreEditorApi.AccessToken();
                if (token.Length > 0)
                {
                    try
                    {
                        var target = AssetStoreAccount.FetchDownloadTarget(token, productId);
                        var path = AssetStoreCache.DownloadPath(AssetStoreCache.Root, target);
                        if (File.Exists(path))
                        {
                            localPath = path;
                            publisher = target.publisher;
                            category = target.category;
                            sizeMB = Math.Round(new FileInfo(path).Length / (1024.0 * 1024.0), 1);
                            return path;
                        }
                    }
                    catch (Exception)
                    {
                        // Fall through to the not-downloaded answer: the store lookup is only a second chance.
                    }
                }
            }
            throw new ArgumentException($"'{name}' is not downloaded yet: call OpenInPackageManager() on it and press Download " +
                                        "(the Editor has no public download API), then assetStore | call Refresh() and Import()" +
                                        (AssetStoreEditorApi.AccessToken().Length == 0 ? SignInFirst : ""));
        }
    }
}
