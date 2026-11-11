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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    /// <summary>
    /// The <c>assetStore</c> source: the Asset Store packages the user can import — the ones they own ("My Assets", read with
    /// the Editor's sign-in) merged with the asset packages already downloaded to the local cache, e.g.
    /// <c>assetStore | get packages | where name contains "chess" | call Import()</c>. Downloading a package not in the cache
    /// yet is the Package Manager window's job (<c>call OpenInPackageManager()</c>): the Editor exposes no public API for it.
    /// </summary>
    [RqlNoAutoSave] // reading the store or importing a package must not save the user's unrelated scene edits
    public sealed class AssetStoreEditorApi
    {
        public static readonly AssetStoreEditorApi Instance = new AssetStoreEditorApi();

        /// <summary>How long the owned-packages list is reused before the store is asked again.</summary>
        static readonly TimeSpan PurchasesTtl = TimeSpan.FromMinutes(10);

        List<AssetStorePurchase>? _purchases;
        string? _purchasesUser;
        DateTime _purchasesAt;
        DateTime _failedAt = DateTime.MinValue;

        /// <summary>How long a failed read of the owned packages is not retried (each try can wait for the network).</summary>
        static readonly TimeSpan FailureBackoff = TimeSpan.FromMinutes(1);
        string? _accountError;

        private AssetStoreEditorApi() { }

        // ------------------------------------------------------------ state

        [RqlDescription("a Unity account is signed in to the Editor: needed for owned packages (the local cache works without); " +
                        "false: call SignIn() to open Unity's sign-in for the user")]
        public bool signedIn => !string.IsNullOrEmpty(AccessToken());

        [RqlDescription("the signed-in Unity account's user name (empty when signed out)")]
        public string userName => signedIn ? CloudProjectSettings.userName ?? "" : "";

        [RqlDescription("the local cache the Editor downloads asset packages into (ASSETSTORE_CACHE_PATH or the OS default)")]
        public string cachePath => AssetStoreCache.Root.Replace('\\', '/');

        [RqlDescription("why the owned packages could not be read last time (sign-in: see SignIn(), network), null when they were")]
        public string? accountError
        {
            get
            {
                _ = OwnedPurchases();
                return _accountError;
            }
        }

        [RqlDescription("the packages you can import: owned ones (owned = true; downloaded = in the local cache) and cached " +
                        "downloads; members productId name publisher category owned downloaded sizeMB purchasedAt tags hidden " +
                        "storeUrl localPath; calls Import(), Contents(filter), OpenInPackageManager()")]
        public List<AssetStorePackage> packages => BuildPackages();

        // ------------------------------------------------------------ operations

        [RqlCallable("forget the cached owned-packages list and read it from the Asset Store again (after buying or adding a " +
                     "package, or downloading one in the Package Manager); returns the number of packages")]
        public int Refresh()
        {
            _purchases = null;
            _failedAt = DateTime.MinValue;
            return BuildPackages().Count;
        }

        [RqlCallable("open Unity's sign-in for the user when the Editor is not signed in to a Unity account (signedIn false; the " +
                     "owned packages need it; it goes through Unity Hub, so an Editor opened without the Hub is told how to reopen it); the " +
                     "user signs in, never the agent; returns whether it opened and the next step")]
        public AssetStoreSignIn SignIn() => AssetStoreSignInFlow.Run(signedIn, userName);

        [RqlCallable("open the Asset Store website's search for text in the user's browser, to find new packages (buying or " +
                     "adding one to My Assets is the user's step: then Refresh() and Import()); returns the URL")]
        public string OpenStoreSearch(string text)
        {
            var url = "https://assetstore.unity.com/?q=" + Uri.EscapeDataString(text ?? "") + "&orderBy=1";
            Application.OpenURL(url);
            return url;
        }

        [RqlCallable("import any .unitypackage file (a path in the project, e.g. a render-pipeline variant shipped inside an " +
                     "Asset Store package, or an absolute path) without the import dialog; returns what it imported")]
        public AssetStoreImportResult ImportFile(string path, bool allowProjectSettings = false)
        {
            var file = PackageFile(path);
            return AssetStorePackageImporter.Import(Path.GetFileNameWithoutExtension(file), file, allowProjectSettings);
        }

        // Call validator (RQL runs it before the call, on dry runs too).
        public string? ValidateImportFile(string path, bool allowProjectSettings = false)
            => AssetStorePackageImporter.Rejection(() => AssetStorePackageImporter.Check(PackageFile(path), allowProjectSettings));

        // ------------------------------------------------------------ internals

        /// <summary>The answer when the Editor has no Unity account signed in, with the way out.</summary>
        internal const string NotSignedIn = "not signed in to a Unity account in the Editor: only the local cache is listed; " +
                                            "assetStore | call SignIn() opens Unity's sign-in for the user";

        /// <summary>Test seam: true lists the local cache only, without asking the store.</summary>
        internal static bool AccountDisabled;

        internal static string AccessToken()
        {
            if (AccountDisabled) return "";
            try { return CloudProjectSettings.accessToken ?? ""; }
            catch (Exception) { return ""; } // no Unity Connect in this Editor session
        }

        static string PackageFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("a .unitypackage path is required, e.g. \"Assets/Vendor/URP.unitypackage\"");
            var full = Path.GetFullPath(path!.Trim());
            if (!full.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"'{path}' is not a .unitypackage file");
            if (!File.Exists(full))
                throw new ArgumentException($"no file at '{path}'");
            return full.Replace('\\', '/');
        }

        /// <summary>The owned packages, read from the store at most every <see cref="PurchasesTtl"/>; empty when signed out.</summary>
        internal List<AssetStorePurchase> OwnedPurchases()
        {
            var token = AccessToken();
            if (token.Length == 0)
            {
                _accountError = NotSignedIn;
                return new List<AssetStorePurchase>();
            }

            var user = CloudProjectSettings.userId ?? "";
            // Another account's list is never answered, not even while the store is unreachable.
            if (_purchasesUser != user)
            {
                _purchases = null;
                _failedAt = DateTime.MinValue;
                _purchasesUser = user;
            }
            if (_purchases != null && DateTime.UtcNow - _purchasesAt < PurchasesTtl)
                return _purchases;
            // The store was just unreachable: answer from what is known instead of waiting on the network again.
            if (DateTime.UtcNow - _failedAt < FailureBackoff)
                return _purchases ?? new List<AssetStorePurchase>();

            try
            {
                _purchases = AssetStoreAccount.FetchPurchases(token);
                _purchasesAt = DateTime.UtcNow;
                _accountError = null;
                return _purchases;
            }
            catch (Exception e)
            {
                _failedAt = DateTime.UtcNow;
                _accountError = e.Message;
                Debug.LogWarning($"[RQL] assetStore: owned packages unavailable: {_accountError}");
                return _purchases ?? new List<AssetStorePurchase>();
            }
        }

        List<AssetStorePackage> BuildPackages()
        {
            // Cached downloads by name key: each owned package claims the first file with its key, the rest stay cache-only.
            var unclaimed = new Dictionary<string, Queue<AssetStoreCachedFile>>();
            var cached = AssetStoreCache.Scan(AssetStoreCache.Root);
            foreach (var file in cached)
            {
                var key = AssetStoreCache.MatchKey(file.name);
                if (!unclaimed.TryGetValue(key, out var files)) unclaimed[key] = files = new Queue<AssetStoreCachedFile>();
                files.Enqueue(file);
            }
            var claimed = new HashSet<AssetStoreCachedFile>();
            var result = new List<AssetStorePackage>();
            foreach (var purchase in OwnedPurchases())
            {
                AssetStoreCachedFile? file = null;
                if (unclaimed.TryGetValue(AssetStoreCache.MatchKey(purchase.displayName), out var files) && files.Count > 0)
                    claimed.Add(file = files.Dequeue());
                result.Add(AssetStorePackage.Owned(purchase, file));
            }
            foreach (var file in cached)
                if (!claimed.Contains(file)) result.Add(AssetStorePackage.CachedOnly(file));
            return result;
        }
    }
}
