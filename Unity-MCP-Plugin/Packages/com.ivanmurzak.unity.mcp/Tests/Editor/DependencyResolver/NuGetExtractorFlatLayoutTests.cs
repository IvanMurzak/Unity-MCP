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
    /// Coverage for <see cref="NuGetExtractor"/>'s flat-layout extraction.
    /// Synthetic .nupkg fixtures exercise the extractor directly without
    /// touching nuget.org. DLLs are written FLAT under the install directory
    /// using the original <c>{stem}.dll</c> filename from the .nupkg lib
    /// folder — the package version is NOT embedded in the on-disk filename
    /// (it is tracked in <see cref="NuGetInstallManifest"/> instead, so
    /// asmdef <c>precompiledReferences</c> stay stable across version bumps).
    /// </summary>
    [TestFixture]
    public class NuGetExtractorFlatLayoutTests
    {
        string _tempDir = string.Empty;
        string _installDir = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "UnityMcp-Extractor-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _installDir = Path.Combine(_tempDir, "install");
            Directory.CreateDirectory(_installDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, recursive: true); }
                catch { /* best-effort */ }
            }
        }

        [Test]
        public void ExtractDlls_WritesUnversionedFlatFilename()
        {
            // Output filename is the original {stem}.dll from the .nupkg's
            // lib/<tfm>/ folder; version is tracked in the manifest, not the
            // filename. The DLL sits directly under the install path (no
            // per-package subfolder, no version tail).
            var nupkg = BuildNupkg("System.Text.Json", "8.0.5", "lib/netstandard2.0/System.Text.Json.dll");

            var extracted = NuGetExtractor.ExtractDlls(nupkg, _installDir);

            CollectionAssert.AreEqual(new[] { "System.Text.Json.dll" }, extracted);
            Assert.IsTrue(File.Exists(Path.Combine(_installDir, "System.Text.Json.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installDir, "System.Text.Json.8.0.5.dll")),
                "Versioned-filename layout is no longer produced.");
        }

        [Test]
        public void ExtractDlls_WritesAllShippedDlls_ForMultiDllPackage()
        {
            // Multi-DLL packages (e.g. Microsoft.Bcl.Memory) write every DLL
            // under its original stem; the per-package version (10.0.3 here)
            // lives in the manifest entry.
            var nupkg = BuildNupkg("Microsoft.Bcl.Memory", "10.0.3",
                "lib/netstandard2.0/System.Memory.dll",
                "lib/netstandard2.0/System.Buffers.dll",
                "lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll");

            var extracted = NuGetExtractor.ExtractDlls(nupkg, _installDir);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "System.Memory.dll",
                    "System.Buffers.dll",
                    "System.Runtime.CompilerServices.Unsafe.dll",
                },
                extracted);
        }

        [Test]
        public void ExtractDlls_OverwritesExistingFile_ForVersionBumpInPlace()
        {
            // Steady-state version-bump: the same {stem}.dll filename is
            // reused across versions, so re-extraction overwrites bytes in
            // place. This is the primary win of the unversioned layout —
            // asmdef precompiledReferences never need to be edited when the
            // configured package version bumps.
            var nupkg = BuildNupkg("Foo.Bar", "1.0.0", "lib/netstandard2.0/Foo.Bar.dll");
            var targetPath = Path.Combine(_installDir, "Foo.Bar.dll");

            File.WriteAllText(targetPath, "old-version-bytes");
            NuGetExtractor.ExtractDlls(nupkg, _installDir);

            Assert.AreEqual("dummy-dll-content", File.ReadAllText(targetPath),
                "Extraction must overwrite the existing file with the new version's bytes.");
        }

        [Test]
        public void ExtractDlls_ReturnsEmpty_WhenPackageHasNoCompatibleFramework()
        {
            // Build a nupkg with NO lib/ entries.
            var emptyNupkg = Path.Combine(_tempDir, "Empty.1.0.0.nupkg");
            using (var zip = ZipFile.Open(emptyNupkg, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("Empty.nuspec");
                using var w = new StreamWriter(entry.Open());
                w.Write("<?xml version=\"1.0\"?><package><metadata><id>Empty</id><version>1.0.0</version><authors>t</authors><description>t</description></metadata></package>");
            }

            var extracted = NuGetExtractor.ExtractDlls(emptyNupkg, _installDir);

            Assert.AreEqual(0, extracted.Count);
            Assert.AreEqual(0, Directory.GetFiles(_installDir).Length,
                "Empty package must not write anything to disk.");
        }

        /// <summary>
        /// Builds a minimal synthetic .nupkg zip with a .nuspec at the root and
        /// the given lib/{tfm}/*.dll entries. Sufficient input for the
        /// extractor under test.
        /// </summary>
        string BuildNupkg(string id, string version, params string[] entryPaths)
        {
            var nupkgPath = Path.Combine(_tempDir, $"{id}.{version}.nupkg");
            using var zip = ZipFile.Open(nupkgPath, ZipArchiveMode.Create);

            // Minimal .nuspec at root.
            var nuspec = zip.CreateEntry($"{id}.nuspec");
            using (var w = new StreamWriter(nuspec.Open()))
            {
                w.Write(
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<package xmlns=\"http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd\">" +
                    "<metadata>" +
                    $"<id>{id}</id>" +
                    $"<version>{version}</version>" +
                    "<authors>test</authors>" +
                    "<description>fixture</description>" +
                    "</metadata>" +
                    "</package>");
            }

            foreach (var path in entryPaths)
            {
                var entry = zip.CreateEntry(path);
                using var w = new StreamWriter(entry.Open());
                w.Write("dummy-dll-content");
            }

            return nupkgPath;
        }
    }
}
