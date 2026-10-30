/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2026 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.  |
+------------------------------------------------------------------+
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.DependencyResolverTests
{
    /// <summary>
    /// The resolver with a fake feed: a pin that cannot be fetched (404) must not churn the DLL set or refresh the
    /// AssetDatabase (offline, that was an endless domain-reload loop), and the version every Id resolves to is the
    /// highest any chain asks for, whatever order the chains come in (a first-come order removed and re-extracted
    /// the same DLLs on every pass).
    /// </summary>
    [TestFixture]
    public class NuGetResolverPartialGraphTests
    {
        string _tempDir = string.Empty;
        string _installDir = string.Empty;
        Func<NuGetPackage, string> _realDownload = null!;
        readonly Dictionary<string, string> _feed = new(StringComparer.OrdinalIgnoreCase);
        readonly List<string> _requests = new();

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "UnityMcp-NuGetPartial-" + Path.GetRandomFileName());
            _installDir = Path.Combine(_tempDir, "install");
            Directory.CreateDirectory(_installDir);
            _feed.Clear();
            _requests.Clear();
            _realDownload = NuGetPackageInstaller.DownloadPackage;
            NuGetPackageInstaller.DownloadPackage = package =>
            {
                _requests.Add(package.ToString());
                if (_feed.TryGetValue(package.ToString(), out var path))
                    return path;
                throw new InvalidOperationException("Response status code does not indicate success: 404 (Not Found).");
            };
            NuGetPackageInstaller.ResetSession();
        }

        [TearDown]
        public void TearDown()
        {
            NuGetPackageInstaller.DownloadPackage = _realDownload;
            NuGetPackageInstaller.ResetSession();
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
        }

        static NuGetPackage P(string id, string version) => new NuGetPackage(id, version);

        // One NuGetPackageRestorer.Restore() pass against the temp install directory.
        (bool changed, IReadOnlyList<string> failures) Pass(params NuGetPackage[] roots)
        {
            var changed = NuGetPackageRestorer.Restore(_installDir, roots);
            return (changed, NuGetPackageRestorer.LastFailures);
        }

        string InstalledVersion(string id) =>
            NuGetInstallManifest.Load(_installDir).Packages.TryGetValue(id, out var e) ? e.Version : "<none>";

        [Test]
        public void UnfetchablePin_ChangesNothing_AndIsReportedOnce()
        {
            Publish("Fake.Lib", "2.0.0");
            Publish("Fake.Lib", "1.0.0");
            Publish("Fake.App", "1.0.0", ("Fake.Missing", "1.0.0"), ("Fake.Lib", "1.0.0"));
            Assert.IsFalse(Pass(P("Fake.Lib", "2.0.0")).failures.Any());
            foreach (var dll in Directory.GetFiles(_installDir, "*.dll"))
                File.SetLastWriteTimeUtc(dll, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var before = Snapshot();

            var (changed, failures) = Pass(P("Fake.App", "1.0.0"), P("Fake.Lib", "2.0.0"));

            Assert.AreEqual(1, failures.Count, string.Join("\n", failures));
            StringAssert.Contains("Fake.Missing 1.0.0", failures[0]);
            StringAssert.Contains("404", failures[0]);
            Assert.IsFalse(changed);
            Assert.IsFalse(NuGetPackageRestorer.ShouldRefresh(changed, failures), "a failed fetch never refreshes (no reload loop)");
            CollectionAssert.AreEquivalent(before, Snapshot(), "no DLL is added, removed or rewritten from a partial graph");
            Assert.AreEqual("2.0.0", InstalledVersion("Fake.Lib"));

            var message = NuGetPackageRestorer.FailureMessage(failures);
            StringAssert.Contains("Fake.Missing 1.0.0", message);
            StringAssert.Contains("Force Resolve NuGet DLLs", message);
        }

        [Test]
        public void LowerVersionRequestedFirst_IsNeverExtracted_AndASecondPassChangesNothing()
        {
            Publish("Fake.Lib", "1.0.0");
            Publish("Fake.Lib", "2.0.0");
            Publish("Fake.App", "1.0.0", ("Fake.Lib", "1.0.0"));
            var roots = new[] { P("Fake.App", "1.0.0"), P("Fake.Lib", "2.0.0") };

            var first = Pass(roots);
            Assert.IsTrue(first.changed);
            Assert.IsTrue(NuGetPackageRestorer.ShouldRefresh(first.changed, first.failures), "a successful change refreshes once");
            Assert.AreEqual("2.0.0", InstalledVersion("Fake.Lib"));
            // A sentinel timestamp: any delete + re-extract of the same version shows (zip entry times would not).
            foreach (var dll in Directory.GetFiles(_installDir, "*.dll"))
                File.SetLastWriteTimeUtc(dll, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var after = Snapshot();

            var second = Pass(roots);
            Assert.IsFalse(second.changed, "the steady state must not flip Fake.Lib 2.0.0 -> 1.0.0 -> 2.0.0");
            Assert.IsFalse(NuGetPackageRestorer.ShouldRefresh(second.changed, second.failures));
            CollectionAssert.AreEquivalent(after, Snapshot());
            Assert.AreEqual("2.0.0", InstalledVersion("Fake.Lib"));
        }

        [Test]
        public void ALosingVersion_ContributesNoDependencies_AndIsNotEvenFetched()
        {
            // As in NuGet: Fake.Tool resolves to 1.1.0, so only 1.1.0's dependency counts (Fake.Lib 1.0.0); the 2.0.0
            // that losing Fake.Tool 1.0.0 asks for is not installed, and 1.0.0 is not downloaded at all (offline it
            // would otherwise fail a restore that does not need it).
            Publish("Fake.Lib", "1.0.0");
            Publish("Fake.Lib", "2.0.0");
            Publish("Fake.Tool", "1.1.0", ("Fake.Lib", "1.0.0"));

            var (changed, failures) = Pass(P("Fake.Tool", "1.0.0"), P("Fake.Tool", "1.1.0"));

            Assert.IsEmpty(failures, string.Join("; ", failures));
            Assert.IsTrue(changed);
            Assert.AreEqual("1.1.0", InstalledVersion("Fake.Tool"));
            Assert.AreEqual("1.0.0", InstalledVersion("Fake.Lib"));
            CollectionAssert.DoesNotContain(_requests, "Fake.Tool 1.0.0");
            CollectionAssert.DoesNotContain(_requests, "Fake.Lib 2.0.0");
        }

        [Test]
        public void ADependencyOfTheSelectedVersion_RaisesTheVersionOverALowerRequest()
        {
            // Fake.App asks for Fake.Lib 1.0.0, the selected Fake.Plugin 2.0.0 asks for Fake.Lib 2.0.0: 2.0.0 wins.
            Publish("Fake.Lib", "1.0.0");
            Publish("Fake.Lib", "2.0.0");
            Publish("Fake.App", "1.0.0", ("Fake.Lib", "1.0.0"), ("Fake.Plugin", "2.0.0"));
            Publish("Fake.Plugin", "2.0.0", ("Fake.Lib", "2.0.0"));

            var (changed, failures) = Pass(P("Fake.App", "1.0.0"));

            Assert.IsEmpty(failures);
            Assert.IsTrue(changed);
            Assert.AreEqual("2.0.0", InstalledVersion("Fake.Lib"));
            Assert.AreEqual("2.0.0", InstalledVersion("Fake.Plugin"));
        }

        [TestCase("1.10.0", "1.9.0", 1)]
        [TestCase("8.14.0", "8.14.0-dev.c0d3157", 1)]
        [TestCase("8.14.0-dev.a", "8.14.0-dev.b", -1)]
        [TestCase("10.0.0-preview.1", "9.0.0", 1)]
        [TestCase("1.0.0+build.7", "1.0.0", 0)]
        [TestCase("8.0.3", "8.0.3", 0)]
        public void CompareVersions_NumericCoreThenReleaseOverPrerelease(string a, string b, int expected)
        {
            Assert.AreEqual(expected, NuGetPackageInstaller.CompareVersions(a, b));
            Assert.AreEqual(-expected, NuGetPackageInstaller.CompareVersions(b, a));
        }

        [Test]
        public void ShouldRefresh_OnlyForAChangeWithoutFailures()
        {
            Assert.IsTrue(NuGetPackageRestorer.ShouldRefresh(true, Array.Empty<string>()));
            Assert.IsFalse(NuGetPackageRestorer.ShouldRefresh(false, Array.Empty<string>()));
            Assert.IsFalse(NuGetPackageRestorer.ShouldRefresh(true, new[] { "X 1.0.0 (404)" }));
        }

        // Every DLL in the install directory with its content and timestamp: any churn shows.
        List<string> Snapshot() => Directory.GetFiles(_installDir, "*.dll")
            .Select(f => Path.GetFileName(f) + "|" + File.ReadAllText(f) + "|" + File.GetLastWriteTimeUtc(f).Ticks).ToList();

        void Publish(string id, string version, params (string id, string version)[] deps)
        {
            var path = Path.Combine(_tempDir, $"{id}.{version}.nupkg");
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var nuspec = zip.CreateEntry($"{id}.nuspec");
                using (var w = new StreamWriter(nuspec.Open()))
                {
                    w.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                        "<package xmlns=\"http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd\"><metadata>" +
                        $"<id>{id}</id><version>{version}</version><authors>test</authors><description>fixture</description>" +
                        "<dependencies><group targetFramework=\".NETStandard2.0\">" +
                        string.Concat(deps.Select(d => $"<dependency id=\"{d.id}\" version=\"[{d.version}, )\" />")) +
                        "</group></dependencies></metadata></package>");
                }
                var dll = zip.CreateEntry($"lib/netstandard2.0/{id}.dll");
                using (var w = new StreamWriter(dll.Open()))
                    w.Write($"{id} {version}");
            }
            _feed[$"{id} {version}"] = path;
        }
    }
}
