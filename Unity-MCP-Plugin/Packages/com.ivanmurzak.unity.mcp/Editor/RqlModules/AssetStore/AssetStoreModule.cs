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
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    /// <summary>
    /// Asset Store: the <c>assetStore</c> source — owned packages (My Assets, via the Editor's sign-in) and the local download
    /// cache, with import. In core: the Asset Store is part of every Editor. Agents read it with <c>rql-schema "assetStore"</c>.
    /// </summary>
    public sealed class AssetStoreModule : IRqlModule
    {
        /// <summary>The source name agents type at the start of a query: <c>assetStore | …</c>.</summary>
        public const string SourceName = "assetStore";

        /// <summary>After the core module (0), like the other optional modules.</summary>
        public int Order => 100;

        public void Configure(RqlModuleContext ctx) =>
            ctx.AddSource(SourceName, () => AssetStoreEditorApi.Instance,
                "Asset Store packages you own (My Assets, signed in) or downloaded: assetStore | get packages | where name contains " +
                "\"tree\" | select productId name downloaded sizeMB, then call Import() (returns its prefabs / materials) or " +
                "OpenInPackageManager() to download; signedIn false: call SignIn() (opens Unity's sign-in for the user); calls Refresh, " +
                "OpenStoreSearch(text), ImportFile(path) (rql-schema \"assetStore\")",
                typeof(AssetStoreEditorApi));
    }
}
