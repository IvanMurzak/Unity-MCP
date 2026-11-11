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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;

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
        [Description("Why the last assetstore-add of it failed to start after answering awaiting-user (no 'Missing signature' window appears); null otherwise.")]
        public string? ImportError { get; set; }
    }

    [AiToolType]
    public sealed partial class Tool_AssetStore
    {
        public const string LibraryToolId = "assetstore-library";
        public const string AddToolId = "assetstore-add";

        /// <summary>Rows whose Imported is checked: each check reads the package file's table of contents.</summary>
        const int MaxImportedChecks = 5;
    }
}
