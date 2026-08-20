/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.  |
+------------------------------------------------------------------+
*/

#nullable enable
using System.IO;
using System.Linq;
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.DependencyResolverTests
{
    /// <summary>
    /// Coverage for the shipped <c>nuget-dependencies.json</c> pin manifest (issue #707: the
    /// package-upgrade restore must use the NEW package's pins, not the previous AppDomain's
    /// compiled-in ones — otherwise the post-update recompile fails against stale DLLs, the
    /// domain reload is blocked, and the MCP server is neither updated nor restarted).
    /// </summary>
    [TestFixture]
    public class NuGetDependencyManifestTests
    {
        string _tempRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(
                Path.GetTempPath(),
                "UnityMcp-DepManifest-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            // Never leave a pin override behind — NuGetConfig is domain-global state and a
            // lingering override would poison every later restore in this editor session.
            NuGetConfig.SetPackagesOverride(null);

            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }

        [Test]
        public void TryParse_ValidManifest_ReturnsPins()
        {
            const string json = @"{
  ""packages"": [
    { ""id"": ""com.IvanMurzak.McpPlugin"", ""version"": ""7.3.0"", ""includeInBuild"": true },
    { ""id"": ""Microsoft.CodeAnalysis.CSharp"", ""version"": ""4.8.0"", ""includeInBuild"": false }
  ]
}";
            var pins = NuGetDependencyManifest.TryParse(json);

            Assert.IsNotNull(pins);
            Assert.AreEqual(2, pins!.Length);
            Assert.AreEqual("com.IvanMurzak.McpPlugin", pins[0].Id);
            Assert.AreEqual("7.3.0", pins[0].Version);
            Assert.IsTrue(pins[0].IncludeInBuild);
            Assert.AreEqual("Microsoft.CodeAnalysis.CSharp", pins[1].Id);
            Assert.AreEqual("4.8.0", pins[1].Version);
            Assert.IsFalse(pins[1].IncludeInBuild);
        }

        [Test]
        public void TryParse_MalformedJson_ReturnsNull()
            => Assert.IsNull(NuGetDependencyManifest.TryParse("{ not json ]"));

        [Test]
        public void TryParse_EmptyPackages_ReturnsNull()
            => Assert.IsNull(NuGetDependencyManifest.TryParse(@"{ ""packages"": [] }"));

        [Test]
        public void TryParse_NoPackagesField_ReturnsNull()
            => Assert.IsNull(NuGetDependencyManifest.TryParse(@"{ ""something"": 1 }"));

        [Test]
        public void TryParse_EntryMissingVersion_ReturnsNull()
            => Assert.IsNull(NuGetDependencyManifest.TryParse(
                @"{ ""packages"": [ { ""id"": ""R3"", ""version"": """" } ] }"));

        [Test]
        public void TryLoad_MissingFile_ReturnsNull()
            => Assert.IsNull(NuGetDependencyManifest.TryLoad(_tempRoot));

        [Test]
        public void TryLoad_NullOrEmptyRoot_ReturnsNull()
        {
            Assert.IsNull(NuGetDependencyManifest.TryLoad(null));
            Assert.IsNull(NuGetDependencyManifest.TryLoad(string.Empty));
        }

        [Test]
        public void TryLoad_ReadsManifestFromPackageLayout()
        {
            var folder = Path.Combine(_tempRoot, "Editor", "DependencyResolver");
            Directory.CreateDirectory(folder);
            File.WriteAllText(
                Path.Combine(folder, NuGetDependencyManifest.FileName),
                @"{ ""packages"": [ { ""id"": ""R3"", ""version"": ""1.3.0"", ""includeInBuild"": true } ] }");

            var pins = NuGetDependencyManifest.TryLoad(_tempRoot);

            Assert.IsNotNull(pins);
            Assert.AreEqual(1, pins!.Length);
            Assert.AreEqual("R3", pins[0].Id);
            Assert.AreEqual("1.3.0", pins[0].Version);
            Assert.IsTrue(pins[0].IncludeInBuild);
        }

        [Test]
        public void PackagesOverride_ReplacesAndRestoresCompiledPins()
        {
            var compiled = NuGetConfig.Packages;
            var replacement = new[] { new NuGetPackage("Test.Package", "1.0.0", includeInBuild: true) };
            try
            {
                NuGetConfig.SetPackagesOverride(replacement);
                Assert.AreSame(replacement, NuGetConfig.Packages);
            }
            finally
            {
                NuGetConfig.SetPackagesOverride(null);
            }
            Assert.AreSame(compiled, NuGetConfig.Packages);
        }

        /// <summary>
        /// THE drift gate: the shipped manifest must stay in lockstep with the compiled pins.
        /// A pin bump that touches <c>NuGetConfig.DefaultPackages</c> but not
        /// <c>nuget-dependencies.json</c> (or vice versa) would silently re-introduce the #707
        /// upgrade wedge — this test turns that mistake into a red CI run.
        /// </summary>
        [Test]
        public void ShippedManifest_MatchesCompiledPins()
        {
            var packageInfo = PackageInfo.FindForAssembly(typeof(NuGetConfig).Assembly);
            Assert.IsNotNull(packageInfo, "Could not resolve the Unity-MCP package for the resolver assembly.");

            var shipped = NuGetDependencyManifest.TryLoad(packageInfo!.resolvedPath);
            Assert.IsNotNull(shipped,
                $"'{NuGetDependencyManifest.FileName}' is missing or unreadable in '{packageInfo.resolvedPath}'.");

            var compiled = NuGetConfig.Packages;
            Assert.AreEqual(compiled.Length, shipped!.Length,
                "Pin count differs between nuget-dependencies.json and NuGetConfig.");

            foreach (var pin in compiled)
            {
                var match = shipped.Where(p => string.Equals(p.Id, pin.Id, System.StringComparison.OrdinalIgnoreCase)).ToArray();
                Assert.AreEqual(1, match.Length, $"Pin '{pin.Id}' missing (or duplicated) in {NuGetDependencyManifest.FileName}.");
                Assert.AreEqual(pin.Version, match[0].Version, $"Version drift for '{pin.Id}'.");
                Assert.AreEqual(pin.IncludeInBuild, match[0].IncludeInBuild, $"includeInBuild drift for '{pin.Id}'.");
            }
        }

        [Test]
        public void IsUnityMcpPackage_MatchesOnlyTheUnityMcpId()
        {
            Assert.IsTrue(NuGetDependencyResolver.IsUnityMcpPackage("com.ivanmurzak.unity.mcp"));
            Assert.IsTrue(NuGetDependencyResolver.IsUnityMcpPackage("Com.IvanMurzak.Unity.MCP"));
            Assert.IsFalse(NuGetDependencyResolver.IsUnityMcpPackage("com.ivanmurzak.unity.mcp.animation"));
            Assert.IsFalse(NuGetDependencyResolver.IsUnityMcpPackage(null));
            Assert.IsFalse(NuGetDependencyResolver.IsUnityMcpPackage(string.Empty));
        }
    }
}
