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
                        SizeMB = p.sizeMB,
                        ImportError = p.importError
                    }).ToList()
                };
            });
        }
    }
}
