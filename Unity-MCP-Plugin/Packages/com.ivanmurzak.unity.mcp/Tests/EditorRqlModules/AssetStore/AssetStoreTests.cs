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
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters;
using NUnit.Framework;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.AssetStore
{
    /// <summary>Builds .unitypackage files (gzipped tar of guid/pathname, guid/asset, guid/asset.meta) for the tests.</summary>
    internal static class TestPackages
    {
        /// <summary>
        /// A fresh path under the project's Temp/ folder: the Editor's own temp folder can be one it may not delete in
        /// (seen: C:\Windows\Temp, where the tests could not delete their folders).
        /// </summary>
        public static string TempPath(string name) => Path.GetFullPath(Path.Combine("Temp", $"{name}-{Guid.NewGuid():N}"));

        public static byte[] Tar(IEnumerable<(string name, byte[] data)> entries)
        {
            using var ms = new MemoryStream();
            foreach (var (name, data) in entries)
            {
                var header = new byte[512];
                Encoding.ASCII.GetBytes(name).CopyTo(header, 0);
                Encoding.ASCII.GetBytes("0000644\0").CopyTo(header, 100);
                Encoding.ASCII.GetBytes(Convert.ToString(data.Length, 8).PadLeft(11, '0') + "\0").CopyTo(header, 124);
                header[156] = (byte)'0';
                Encoding.ASCII.GetBytes("ustar\0").CopyTo(header, 257);
                // The header checksum (Unity's own decompressor checks it): the byte sum with the field read as spaces.
                for (var i = 148; i < 156; i++) header[i] = (byte)' ';
                var sum = 0;
                foreach (var b in header) sum += b;
                Encoding.ASCII.GetBytes(Convert.ToString(sum, 8).PadLeft(6, '0') + "\0 ").CopyTo(header, 148);
                ms.Write(header, 0, 512);
                ms.Write(data, 0, data.Length);
                var pad = (512 - data.Length % 512) % 512;
                ms.Write(new byte[pad], 0, pad);
            }
            ms.Write(new byte[1024], 0, 1024);
            return ms.ToArray();
        }

        /// <summary>A package of text assets: (project path, content); a null content makes a folder.</summary>
        public static byte[] Package(params (string path, string? content)[] assets)
        {
            var entries = new List<(string, byte[])>();
            foreach (var (path, content) in assets)
            {
                var guid = Guid.NewGuid().ToString("N");
                entries.Add(($"{guid}/pathname", Encoding.UTF8.GetBytes(path + "\n00")));
                if (content != null)
                    entries.Add(($"{guid}/asset", Encoding.UTF8.GetBytes(content)));
                var meta = content == null
                    ? $"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n"
                    : $"fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n";
                entries.Add(($"{guid}/asset.meta", Encoding.UTF8.GetBytes(meta)));
            }
            var tar = Tar(entries);
            using var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
                gz.Write(tar, 0, tar.Length);
            return ms.ToArray();
        }
    }

    public class UnityPackageArchiveTests
    {
        [Test]
        public void ReadsPathsAndTellsFilesFromFolders()
        {
            var tar = TestPackages.Tar(new[]
            {
                ("./aaa/pathname", Encoding.UTF8.GetBytes("Assets/Pack\n00")),
                ("./aaa/asset.meta", Encoding.UTF8.GetBytes("meta")),
                ("bbb/asset", new byte[700]),          // the asset before its pathname, 700 bytes: padding crosses a block
                ("bbb/pathname", Encoding.UTF8.GetBytes("Assets/Pack/Rock.prefab")),
                ("bbb/preview.png", new byte[3]),
            });
            var entries = UnityPackageArchive.ReadEntries(new MemoryStream(tar));
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("Assets/Pack", entries[0].path);
            Assert.IsFalse(entries[0].isFile);
            Assert.AreEqual("Assets/Pack/Rock.prefab", entries[1].path);
            Assert.IsTrue(entries[1].isFile);
            Assert.AreEqual(700, entries[1].sizeBytes);
            Assert.AreEqual("bbb", entries[1].guid);
        }

        [Test]
        public void ReadsAGzippedPackageFile()
        {
            var file = TestPackages.TempPath("rql-assetstore") + ".unitypackage";
            File.WriteAllBytes(file, TestPackages.Package(("Assets/A", null), ("Assets/A/x.txt", "hi")));
            try
            {
                var entries = UnityPackageArchive.ReadEntries(file);
                CollectionAssert.AreEqual(new[] { "Assets/A", "Assets/A/x.txt" }, entries.Select(e => e.path).ToArray());
            }
            finally { File.Delete(file); }
        }
    }

    public class AssetStoreParsingTests
    {
        [Test]
        public void ParsesPurchases()
        {
            using var doc = JsonDocument.Parse("{\"total\":2,\"results\":[" +
                "{\"packageId\":27676,\"displayName\":\"DOTween (HOTween v2)\",\"grantTime\":\"2020-01-02T00:00:00Z\",\"tagging\":[\"Tools\"],\"isHidden\":false}," +
                "{\"packageId\":\"123\",\"displayName\":\"Rocks\",\"isHidden\":true},{\"displayName\":\"no id\"}]}");
            var list = AssetStoreAccount.ParsePurchases(doc.RootElement);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(27676, list[0].productId);
            Assert.AreEqual("DOTween (HOTween v2)", list[0].displayName);
            CollectionAssert.AreEqual(new[] { "Tools" }, list[0].tags);
            Assert.AreEqual(123, list[1].productId);
            Assert.IsTrue(list[1].hidden);
        }

        [Test]
        public void ParsesTheDownloadTargetAndDropsDots()
        {
            using var doc = JsonDocument.Parse("{\"result\":{\"download\":{\"filename_safe_publisher_name\":\"Pub.Inc\"," +
                "\"filename_safe_category_name\":\"3D ModelsProps\",\"filename_safe_package_name\":\"Rocks v1.2\"}}}");
            var target = AssetStoreAccount.ParseDownloadTarget(doc.RootElement)!;
            Assert.AreEqual("PubInc", target.publisher);
            Assert.AreEqual("Rocks v12", target.packageName);
            Assert.AreEqual("C:/cache/PubInc/3D ModelsProps/Rocks v12.unitypackage", AssetStoreCache.DownloadPath("C:/cache", target));
        }

        [Test]
        public void MatchKeyIgnoresPunctuationAndCase()
        {
            Assert.AreEqual(AssetStoreCache.MatchKey("DOTween (HOTween v2)"), AssetStoreCache.MatchKey("DOTween HOTween v2"));
            Assert.AreNotEqual(AssetStoreCache.MatchKey("Rocks"), AssetStoreCache.MatchKey("Rocks 2"));
        }

        [Test]
        public void TellsTheRenderPipelineAPackageIsMadeFor()
        {
            var none = new List<UnityPackageEntry>();
            Assert.AreEqual("URP", AssetStorePackageImporter.PackagePipeline("Free Fire VFX - URP", none));
            Assert.AreEqual("HDRP", AssetStorePackageImporter.PackagePipeline("Trails VFX - HDRP", none));
            Assert.IsNull(AssetStorePackageImporter.PackagePipeline("Chess Pieces Board", none));
            Assert.IsNull(AssetStorePackageImporter.PackagePipeline("Hurpy Tools", none)); // "URP" inside a word is not a pipeline
            var mixed = new List<UnityPackageEntry> { new UnityPackageEntry("a", "Assets/P/URP/a.mat") { isFile = true }, new UnityPackageEntry("b", "Assets/P/HDRP/b.mat") { isFile = true } };
            Assert.IsNull(AssetStorePackageImporter.PackagePipeline("Pack", mixed));
        }

        [Test]
        public void DescribeListsPrefabsAndNotesScriptsAndNestedPackages()
        {
            var files = new[] { "Assets/Pub/Pack/Prefabs/Rock.prefab", "Assets/Pub/Pack/Materials/Rock.mat", "Assets/Pub/Pack/Rock.fbx",
                                "Assets/Pub/Pack/Scripts/Spin.cs", "Assets/Pub/Pack/URP.unitypackage", "Assets/Pub/Pack/Demo.unity" }
                .Select((p, i) => new UnityPackageEntry(i.ToString(), p) { isFile = true }).ToList();
            var result = AssetStorePackageImporter.Describe("Pack", files, 0, "imported");
            CollectionAssert.AreEqual(new[] { "Assets/Pub/Pack/Prefabs/Rock.prefab" }, result.prefabs);
            CollectionAssert.AreEqual(new[] { "Assets/Pub/Pack/Rock.fbx" }, result.models);
            CollectionAssert.AreEqual(new[] { "Assets/Pub/Pack/Demo.unity" }, result.scenes);
            Assert.AreEqual(1, result.scripts);
            CollectionAssert.Contains(result.folders, "Assets/Pub/Pack");
            Assert.IsTrue(result.notes.Any(n => n.Contains("1 scripts")), string.Join("\n", result.notes));
            Assert.IsTrue(result.notes.Any(n => n.Contains("ImportFile")), string.Join("\n", result.notes));
        }
    }

    /// <summary>The assetStore source over a temporary cache (ASSETSTORE_CACHE_PATH), the account switched off.</summary>
    public class AssetStoreSourceTests : AdapterTestBase
    {
        const string ImportFolder = "Assets/RqlAssetStoreTests";
        string _cache = "";
        string? _previousCache;
        Func<bool>? _asks;
        Action<string>? _startImport;
        readonly List<string> _started = new List<string>();

        [SetUp]
        public void SetUpCache()
        {
            AssetStoreEditorApi.AccountDisabled = true;
            // Never let a test start Unity's real import: on Unity 6.6 it shows the modal "Missing signature" window.
            _asks = AssetStorePackageImporter.AsksBeforeImport;
            _startImport = AssetStorePackageImporter.StartImport;
            _started.Clear();
            AssetStorePackageImporter.StartImport = file => _started.Add(file);
            _previousCache = Environment.GetEnvironmentVariable(AssetStoreCache.EnvironmentVariable);
            _cache = TestPackages.TempPath("rql-assetstore-cache");
            Directory.CreateDirectory(Path.Combine(_cache, "Test Publisher", "3D ModelsProps"));
            Environment.SetEnvironmentVariable(AssetStoreCache.EnvironmentVariable, _cache);
            if (AssetDatabase.IsValidFolder(ImportFolder)) AssetDatabase.DeleteAsset(ImportFolder);
        }

        [TearDown]
        public void TearDownCache()
        {
            AssetStoreEditorApi.AccountDisabled = false;
            AssetStorePackageImporter.AsksBeforeImport = _asks!;
            AssetStorePackageImporter.StartImport = _startImport!;
            Environment.SetEnvironmentVariable(AssetStoreCache.EnvironmentVariable, _previousCache);
            if (Directory.Exists(_cache)) Directory.Delete(_cache, true);
            if (AssetDatabase.IsValidFolder(ImportFolder)) AssetDatabase.DeleteAsset(ImportFolder);
        }

        string AddToCache(string name, byte[] package)
        {
            var path = Path.Combine(_cache, "Test Publisher", "3D ModelsProps", name + ".unitypackage");
            File.WriteAllBytes(path, package);
            return path;
        }

        [Test]
        public void TheModuleIsDiscoveredAndTheSchemaListsItsCalls()
        {
            CollectionAssert.Contains(RqlUnity.Engine.Modules.Select(m => m.Module).ToList(), typeof(AssetStoreModule).FullName);
            StringAssert.Contains(AssetStoreModule.SourceName, RqlUnitySchema.Render(RqlUnity.Engine, null));
            var api = RqlUnitySchema.Render(RqlUnity.Engine, AssetStoreModule.SourceName);
            foreach (var member in new[] { "signedIn", "cachePath", "packages", "Refresh(", "OpenStoreSearch(", "ImportFile(", "SignIn(" })
                StringAssert.Contains(member, api);
            var package = RqlUnitySchema.Render(RqlUnity.Engine, nameof(AssetStorePackage));
            foreach (var member in new[] { "productId", "downloaded", "localPath", "Import(", "Contents(", "OpenInPackageManager(" })
                StringAssert.Contains(member, package);
        }

        [Test]
        public void ListsCachedPackagesWhenSignedOut()
        {
            AddToCache("Test Rocks", TestPackages.Package(("Assets/RqlAssetStoreTests", null), ("Assets/RqlAssetStoreTests/a.txt", "a")));
            var r = Q("assetStore | get packages | select name publisher category downloaded owned");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.AreEqual(1, r.Rows!.Count, Table(r));
            Assert.AreEqual("Test Rocks", Cell(r, 0, 0));
            Assert.AreEqual("Test Publisher", Cell(r, 0, 1));
            Assert.AreEqual("3D ModelsProps", Cell(r, 0, 2));
            Assert.AreEqual("true", Cell(r, 0, 3));
            Assert.AreEqual("false", Cell(r, 0, 4));
            Assert.AreEqual("false", Cell(Q("assetStore | select signedIn")));
            StringAssert.Contains("not signed in", Cell(Q("assetStore | select accountError")));
            StringAssert.Contains("SignIn()", Cell(Q("assetStore | select accountError")));
        }

        [Test]
        public void ContentsListsFilesWithoutImporting()
        {
            AddToCache("Test Rocks", TestPackages.Package(("Assets/RqlAssetStoreTests", null), ("Assets/RqlAssetStoreTests/a.txt", "a"), ("Assets/RqlAssetStoreTests/b.json", "{}")));
            var r = M("assetStore | get packages | where name == \"Test Rocks\" | call Contents(\".txt\")");
            Assert.IsTrue(r.Ok, Table(r));
            StringAssert.Contains("Assets/RqlAssetStoreTests/a.txt", Table(r));
            StringAssert.DoesNotContain("b.json", Table(r));
            Assert.IsFalse(AssetDatabase.IsValidFolder(ImportFolder));
        }

        [Test]
        public void ImportOfACachedPackagePassesItsChecks()
        {
            AddToCache("Test Rocks", TestPackages.Package(("Assets/RqlAssetStoreTests", null), ("Assets/RqlAssetStoreTests/a.txt", "hello")));
            var r = M("assetStore | get packages | where name == \"Test Rocks\" | call Import()", dryRun: true);
            Assert.IsTrue(r.Ok, Table(r));
            Assert.IsFalse(AssetDatabase.IsValidFolder(ImportFolder), Table(r));
        }

        [Test]
        public void ImportThatUnityConfirmsFirstAnswersAtOnceAndStartsAfterTheCall()
        {
            AssetStorePackageImporter.AsksBeforeImport = () => true;
            AddToCache("Test Rocks", TestPackages.Package(("Assets/RqlAssetStoreTests", null), ("Assets/RqlAssetStoreTests/a.txt", "hello")));
            var r = M("assetStore | get packages | where name == \"Test Rocks\" | call Import()");
            Assert.IsTrue(r.Ok, Table(r));
            StringAssert.Contains("awaiting-user", Table(r));
            StringAssert.Contains("Missing signature", Table(r));
            StringAssert.Contains("select imported", Table(r));
            Assert.AreEqual(0, _started.Count, "the import starts on the next Editor update, after the call answered");
            Assert.AreEqual("false", Cell(Q("assetStore | get packages | where name == \"Test Rocks\" | select imported")));
        }

        [Test]
        public void ImportWithoutConfirmationStartsRightAway()
        {
            AssetStorePackageImporter.AsksBeforeImport = () => false;
            var file = AddToCache("Test Rocks", TestPackages.Package(("Assets/RqlAssetStoreTests", null), ("Assets/RqlAssetStoreTests/a.txt", "hello")));
            var r = M("assetStore | get packages | where name == \"Test Rocks\" | call Import()");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.AreEqual(1, _started.Count, Table(r));
            Assert.AreEqual(Path.GetFullPath(file).Replace('\\', '/'), Path.GetFullPath(_started[0]).Replace('\\', '/'));
            StringAssert.IsMatch("status:(imported|queued)", Table(r));
        }

        [Test]
        public void ImportRefusesToOverwriteProjectSettingsUnasked()
        {
            AddToCache("Full Project", TestPackages.Package(("ProjectSettings/TagManager.asset", "x")));
            var r = M("assetStore | get packages | where name == \"Full Project\" | call Import()", dryRun: true);
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains("Project Settings", Table(r));
        }

        [Test]
        public void ImportOfAPackageNotDownloadedSaysHowToGetIt()
        {
            var r = M("assetStore | call ImportFile(\"C:/no/such/file.unitypackage\")", dryRun: true);
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains("no file", Table(r));
        }

        [Test]
        public void ImportedIgnoresTheDependencyManifestAPackageCarries()
        {
            // Unity merges a package's Packages/manifest.json into the project's instead of importing it as an asset.
            var file = AddToCache("Test Rocks", TestPackages.Package(("Packages/manifest.json", "{}")));
            Assert.IsFalse(AssetStorePackageImporter.IsImported(file), "no Assets/ files: nothing to have arrived");
            var withAsset = AddToCache("Test Stones", TestPackages.Package(("Packages/manifest.json", "{}"), ("Assets/RqlAssetStoreTests/a.txt", "x")));
            Assert.IsFalse(AssetStorePackageImporter.IsImported(withAsset), "the asset is not in the project yet");
        }

        [Test]
        public void LibraryToolFiltersByEveryWordAndSaysWhatIsImportable()
        {
            AddToCache("Low Poly Trees", TestPackages.Package(("Assets/RqlAssetStoreTests/t.txt", "t")));
            AddToCache("Low Poly Rocks", TestPackages.Package(("Assets/RqlAssetStoreTests/r.txt", "r")));
            var result = new Tool_AssetStore().Library("poly tree");
            Assert.IsFalse(result.SignedIn);
            StringAssert.Contains("SignIn()", result.AccountError);
            Assert.AreEqual(1, result.Total);
            Assert.AreEqual("Low Poly Trees", result.Packages[0].Name);
            Assert.IsTrue(result.Packages[0].Downloaded);
            Assert.IsFalse(result.Packages[0].Imported);
            Assert.AreEqual(2, new Tool_AssetStore().Library("publisher").Total, "the publisher matches too");
        }

        [Test]
        public void AddToolImportsByNameAndPointsBackToTheLibraryTool()
        {
            AssetStorePackageImporter.AsksBeforeImport = () => true;
            AddToCache("Test Rocks", TestPackages.Package(("Assets/RqlAssetStoreTests", null), ("Assets/RqlAssetStoreTests/a.txt", "hello")));
            var result = new Tool_AssetStore().Add("test rocks");
            Assert.AreEqual(AssetStorePackageImporter.AwaitingUser, result.status);
            StringAssert.Contains(Tool_AssetStore.LibraryToolId, result.next);
            StringAssert.DoesNotContain("assetStore |", result.next, "the tool's agent may not know the RQL source");
            StringAssert.Contains("never import the package another way", result.next);
        }

        [Test]
        public void AddToolRefusesAnUnknownPackage()
        {
            var e = Assert.Throws<ArgumentException>(() => new Tool_AssetStore().Add("No Such Pack"));
            StringAssert.Contains(Tool_AssetStore.LibraryToolId, e!.Message);
        }
    }

    /// <summary>
    /// <c>assetStore | call SignIn()</c> with the sign-in UI replaced: tests never open Unity's sign-in or touch the account.
    /// </summary>
    public class AssetStoreSignInTests : AdapterTestBase
    {
        int _shown;
        int _refreshed;
        bool _batchMode;
        Action? _showLogin;
        Func<bool>? _isBatchMode;
        Func<bool>? _viaHub;
        bool _launchedViaHub;
        Action? _refresh;

        [SetUp]
        public void ReplaceTheSignInUi()
        {
            AssetStoreEditorApi.AccountDisabled = true; // signed out, whatever the Editor running the tests is signed in to
            _shown = _refreshed = 0;
            _batchMode = false;
            _launchedViaHub = true;
            _viaHub = AssetStoreSignInFlow.LaunchedViaHub;
            AssetStoreSignInFlow.LaunchedViaHub = () => _launchedViaHub;
            _showLogin = AssetStoreSignInFlow.ShowLogin;
            _isBatchMode = AssetStoreSignInFlow.IsBatchMode;
            _refresh = AssetStoreSignInFlow.RefreshAccessToken;
            AssetStoreSignInFlow.ShowLogin = () => _shown++;
            AssetStoreSignInFlow.IsBatchMode = () => _batchMode;
            AssetStoreSignInFlow.RefreshAccessToken = () => _refreshed++;
        }

        [TearDown]
        public void RestoreTheSignInUi()
        {
            AssetStoreEditorApi.AccountDisabled = false;
            AssetStoreSignInFlow.ShowLogin = _showLogin!;
            AssetStoreSignInFlow.IsBatchMode = _isBatchMode!;
            AssetStoreSignInFlow.LaunchedViaHub = _viaHub!;
            AssetStoreSignInFlow.RefreshAccessToken = _refresh!;
        }

        [Test]
        public void SignInOpensUnitysSignInWhenSignedOut()
        {
            var r = M("assetStore | call SignIn()");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.AreEqual(1, _shown, Table(r));
            StringAssert.Contains("sign-in", Table(r));
            StringAssert.Contains("never", Table(r)); // the agent never types the credentials
            StringAssert.Contains("select signedIn", Table(r));
        }

        [Test]
        public void SignInInAHeadlessEditorSaysToOpenOneWithAWindow()
        {
            _batchMode = true;
            var r = M("assetStore | call SignIn()");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.AreEqual(0, _shown, Table(r));
            StringAssert.Contains("headless", Table(r));
        }

        [Test]
        public void SignInInAnEditorOpenedWithoutTheHubSaysToReopenItThroughTheHub()
        {
            _launchedViaHub = false;
            var r = M("assetStore | call SignIn()");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.AreEqual(0, _shown, Table(r)); // its sign-in would only offer "Install Unity Hub"
            StringAssert.Contains("Install Unity Hub", Table(r));
            StringAssert.Contains("unity-mcp-cli open", Table(r));
        }

        [Test]
        public void SignInWhenSignedInOnlyRefreshesTheToken()
        {
            var answer = AssetStoreSignInFlow.Run(signedIn: true, userName: "someone");
            Assert.AreEqual(0, _shown);
            Assert.AreEqual(1, _refreshed);
            Assert.IsTrue(answer.signedIn);
            Assert.IsFalse(answer.opened);
            StringAssert.Contains("already signed in as 'someone'", answer.next);
        }

        [Test]
        public void ADryRunOpensNothing()
        {
            var r = M("assetStore | call SignIn()", dryRun: true);
            Assert.IsTrue(r.Ok, Table(r));
            Assert.AreEqual(0, _shown, Table(r));
        }

        [Test]
        public void TheSchemaDescribesTheAnswer()
        {
            var answer = RqlUnitySchema.Render(RqlUnity.Engine, nameof(AssetStoreSignIn));
            foreach (var member in new[] { "signedIn", "userName", "opened", "next" })
                StringAssert.Contains(member, answer);
        }
    }
}
