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
using System.IO.Compression;
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.DependencyResolverTests
{
    /// <summary>
    /// Regression coverage for issue #670: DependencyResolver was logging
    /// "No compatible framework found" / "No DLLs extracted" warnings for
    /// Roslyn analyzer packages (e.g. Microsoft.CodeAnalysis.Analyzers), which
    /// are marked <c>&lt;developmentDependency&gt;true&lt;/developmentDependency&gt;</c>
    /// in their .nuspec and legitimately have no runtime DLLs under <c>lib/&lt;tfm&gt;/</c>.
    /// </summary>
    [TestFixture]
    public class NuGetExtractorDevelopmentDependencyTests
    {
        string _tempDir = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "UnityMcp-NuGetExtractor-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
            }
        }

        [Test]
        public void IsDevelopmentDependency_ReturnsTrue_WhenNuspecDeclaresDevDependencyTrue()
        {
            var nupkg = BuildNupkg(
                id: "Example.Analyzer",
                version: "1.0.0",
                developmentDependencyElement: "<developmentDependency>true</developmentDependency>");

            Assert.IsTrue(NuGetExtractor.IsDevelopmentDependency(nupkg));
        }

        [Test]
        public void IsDevelopmentDependency_ReturnsTrue_WhenValueHasWhitespace()
        {
            // Some package authors pretty-print their nuspec; we must not choke on whitespace.
            var nupkg = BuildNupkg(
                id: "Example.Analyzer",
                version: "1.0.0",
                developmentDependencyElement: "<developmentDependency>  true  </developmentDependency>");

            Assert.IsTrue(NuGetExtractor.IsDevelopmentDependency(nupkg));
        }

        [Test]
        public void IsDevelopmentDependency_ReturnsFalse_WhenNuspecDeclaresDevDependencyFalse()
        {
            var nupkg = BuildNupkg(
                id: "Example.Library",
                version: "1.0.0",
                developmentDependencyElement: "<developmentDependency>false</developmentDependency>");

            Assert.IsFalse(NuGetExtractor.IsDevelopmentDependency(nupkg));
        }

        [Test]
        public void IsDevelopmentDependency_ReturnsFalse_WhenDevDependencyElementIsMissing()
        {
            // The overwhelmingly common case: normal runtime libraries simply omit the element.
            var nupkg = BuildNupkg(
                id: "Example.Library",
                version: "1.0.0",
                developmentDependencyElement: null);

            Assert.IsFalse(NuGetExtractor.IsDevelopmentDependency(nupkg));
        }

        [Test]
        public void IsDevelopmentDependency_ReturnsFalse_WhenNupkgIsCorrupt()
        {
            // A zero-byte file is not a valid zip archive; the helper must swallow the
            // exception and return false so the caller falls back to normal extraction.
            var bogus = Path.Combine(_tempDir, "corrupt.nupkg");
            File.WriteAllBytes(bogus, new byte[] { 0, 0, 0, 0 });

            Assert.IsFalse(NuGetExtractor.IsDevelopmentDependency(bogus));
        }

        [Test]
        public void IsDevelopmentDependency_ReturnsFalse_WhenNupkgHasNoNuspec()
        {
            var nupkg = Path.Combine(_tempDir, "no-nuspec.nupkg");
            using (var zip = ZipFile.Open(nupkg, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("readme.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("no nuspec here");
            }

            Assert.IsFalse(NuGetExtractor.IsDevelopmentDependency(nupkg));
        }

        /// <summary>
        /// Builds a minimal synthetic .nupkg containing a single .nuspec entry at the root.
        /// Mirrors enough of the real NuGet v3 layout to exercise the parser without downloading
        /// anything over the network.
        /// </summary>
        string BuildNupkg(string id, string version, string? developmentDependencyElement)
        {
            var nupkg = Path.Combine(_tempDir, $"{id}.{version}.nupkg");
            var nuspec =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<package xmlns=\"http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd\">" +
                "  <metadata>" +
                $"    <id>{id}</id>" +
                $"    <version>{version}</version>" +
                "    <authors>test</authors>" +
                "    <description>test fixture package</description>" +
                (developmentDependencyElement ?? string.Empty) +
                "  </metadata>" +
                "</package>";

            using (var zip = ZipFile.Open(nupkg, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry($"{id}.nuspec");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(nuspec);
            }

            return nupkg;
        }
    }
}
