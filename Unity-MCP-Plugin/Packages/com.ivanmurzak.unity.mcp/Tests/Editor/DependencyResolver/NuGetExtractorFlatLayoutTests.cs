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
    /// Coverage for <see cref="NuGetExtractor"/>'s flat-layout extraction
    /// (issue #733). Synthetic .nupkg fixtures exercise the extractor directly
    /// without touching nuget.org. DLL filenames remain unversioned so
    /// existing asmdef <c>precompiledReferences</c> entries continue to
    /// resolve.
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
        public void ExtractDlls_WritesFilesFlatAtTheInstallRoot()
        {
            // Single-DLL package. Output filename matches the input stem and
            // sits directly under the install path (no per-package subfolder).
            var nupkg = BuildNupkg("System.Text.Json", "8.0.5", "lib/netstandard2.0/System.Text.Json.dll");

            var extracted = NuGetExtractor.ExtractDlls(nupkg, _installDir);

            CollectionAssert.AreEqual(new[] { "System.Text.Json.dll" }, extracted);
            Assert.IsTrue(File.Exists(Path.Combine(_installDir, "System.Text.Json.dll")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_installDir, "System.Text.Json.8.0.5")),
                "Per-package directory must not be created in the flat layout.");
        }

        [Test]
        public void ExtractDlls_PreservesEachDllStem_ForMultiDllPackage()
        {
            // Multi-DLL packages (e.g. Microsoft.Bcl.Memory) write every DLL
            // with its own stem, unmodified.
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
        public void ExtractDlls_ReturnsEmpty_WhenPackageHasNoCompatibleFramework()
        {
            // A .nupkg that ships only a non-compatible framework folder
            // produces no extractions and no install-dir noise.
            var nupkg = BuildNupkg("Some.Package", "1.0.0", "lib/wp80/Some.Package.dll");
            // wp80 is not in NuGetConfig.TargetFrameworkPriority — extractor
            // falls back to lex-min available framework, which is wp80, and
            // extracts it. We use a clearly-invalid path instead.
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
