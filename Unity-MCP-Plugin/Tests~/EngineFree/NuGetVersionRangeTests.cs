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
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    public class NuGetVersionRangeTests
    {
        [Theory]
        [InlineData("(1.0.0,2.0.0]", "1.0.0", false)]
        [InlineData("(1.0.0,2.0.0]", "2.0.0", true)]
        [InlineData("(,2.0.0)", "2.0.0", false)]
        [InlineData("(,2.0.0)", "1.9.0", true)]
        [InlineData("[1.0.0,2.0.0)", "2.0.0", false)]
        [InlineData("[1.0.0,2.0.0)", "1.0.0", true)]
        [InlineData("[1.0.0]", "1.0", true)]
        [InlineData("[1.0.0]", "1.0.1", false)]
        [InlineData("1.0.0", "2.0.0", true)]
        [InlineData("[1.0.0,)", "1.0.0+metadata", true)]
        [InlineData("(1.0.0,)", "1.0.0", false)]
        [InlineData("(,2.0.0]", "2.0.0", true)]
        public void BoundsKeepTheirNuGetMeaning(string text, string version, bool expected)
            => Assert.Equal(expected, NuGetVersionRange.Parse(text).Contains(version));

        [Theory]
        [InlineData("[1.0.0,2.0.0")]
        [InlineData("(1.0.0)")]
        [InlineData("[2.0.0,1.0.0]")]
        [InlineData("(1.0.0,1.0.0]")]
        [InlineData("*")]
        public void UnsupportedOrInvalidRangesFailExplicitly(string text)
            => Assert.Throws<FormatException>(() => NuGetVersionRange.Parse(text));

        [Theory]
        [InlineData("1.0", "1.0.0", 0)]
        [InlineData("1.0.0.0", "1.0.0", 0)]
        [InlineData("1.0.0-rc.10", "1.0.0-rc.2", 1)]
        [InlineData("1.0.0-2", "1.0.0-alpha", -1)]
        [InlineData("1.0.0", "1.0.0-rc.10", 1)]
        public void VersionOrderingMatchesBounds(string left, string right, int expected)
            => Assert.Equal(expected, Math.Sign(NuGetVersionRange.CompareVersions(left, right)));

        [Theory]
        [InlineData("[1.0]", "1.0.0")]
        [InlineData("[1.0.0.0]", "1.0.0")]
        [InlineData("1.0.0+build", "1.0.0")]
        public void DownloadCandidatesUseNormalizedVersions(string range, string expected)
            => Assert.Equal(expected, NuGetPackage.Dependency("Shared", range).Version);

        static NuGetPackage P(string id, string version) => new NuGetPackage(id, version);
        static NuGetPackage D(string id, string range) => NuGetPackage.Dependency(id, range);

        [Theory]
        [InlineData("[1.0.0,2.0.0)", "2.0.0")]
        [InlineData("[1.0.0]", "1.1.0")]
        public void ConflictingCandidateIsReportedWithItsParent(string range, string version)
        {
            var errors = NuGetPackagePlanner.Resolve(new[] { P("Parent", "1.0.0"), P("Shared", version) },
                package => package.Id == "Parent" ? new List<NuGetPackage> { D("Shared", range) } : new List<NuGetPackage>(),
                _ => false, out _);

            var error = Assert.Single(errors);
            Assert.Contains("Shared " + version, error);
            Assert.Contains(range, error);
            Assert.Contains("Parent 1.0.0", error);
        }

        [Theory]
        [InlineData("(1.0.0,2.0.0]", "1.0.0")]
        [InlineData("(,2.0.0)", "2.0.0")]
        public void ExcludedBoundIsNeverFetched(string range, string forbidden)
        {
            var fetched = new List<string>();
            var errors = NuGetPackagePlanner.Resolve(new[] { P("Parent", "1.0.0") }, package =>
            {
                fetched.Add(package.ToString());
                return package.Id == "Parent" ? new List<NuGetPackage> { D("Shared", range) } : new List<NuGetPackage>();
            }, _ => false, out _);

            Assert.NotEmpty(errors);
            Assert.DoesNotContain("Shared " + forbidden, fetched);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExplicitCandidateCanSatisfyAnExclusiveBound(bool reverse)
        {
            var roots = new[] { P("Parent", "1.0.0"), P("Shared", "1.5.0") };
            if (reverse) Array.Reverse(roots);
            var errors = NuGetPackagePlanner.Resolve(roots,
                package => package.Id == "Parent" ? new List<NuGetPackage> { D("Shared", "(1.0.0,2.0.0)") } : new List<NuGetPackage>(),
                _ => false, out var selection);

            Assert.Empty(errors);
            Assert.Equal("1.5.0", selection["Shared"]);
        }

        [Fact]
        public void ExplicitCandidateCanSatisfyAnExclusiveUpperBound()
        {
            var errors = NuGetPackagePlanner.Resolve(new[] { P("Parent", "1.0.0"), P("Shared", "1.5.0") },
                package => package.Id == "Parent" ? new List<NuGetPackage> { D("Shared", "(,2.0.0)") } : new List<NuGetPackage>(),
                _ => false, out var selection);
            Assert.Empty(errors);
            Assert.Equal("1.5.0", selection["Shared"]);
        }

        [Fact]
        public void LosingPackageVersionContributesNoConstraints()
        {
            var errors = NuGetPackagePlanner.Resolve(new[] { P("Parent", "1.0.0"), P("Parent", "2.0.0"), P("Shared", "2.0.0") },
                package => package.Id == "Parent"
                    ? new List<NuGetPackage> { D("Shared", package.Version == "1.0.0" ? "[1.0.0]" : "[2.0.0]") }
                    : new List<NuGetPackage>(), _ => false, out var selection);
            Assert.Empty(errors);
            Assert.Equal("2.0.0", selection["Shared"]);
        }

        [Fact]
        public void HighestCompatibleMinimumWinsAcrossSelectedParents()
        {
            var errors = NuGetPackagePlanner.Resolve(new[] { P("App", "1.0.0"), P("Plugin", "2.0.0") },
                package => package.Id == "App" ? new List<NuGetPackage> { D("Shared", "[1.0.0,3.0.0)") }
                    : package.Id == "Plugin" ? new List<NuGetPackage> { D("Shared", "[2.0.0,3.0.0)") }
                    : new List<NuGetPackage>(), _ => false, out var selection);
            Assert.Empty(errors);
            Assert.Equal("2.0.0", selection["Shared"]);
        }

        [Fact]
        public void EveryParentConstraintIsChecked()
        {
            var errors = NuGetPackagePlanner.Resolve(new[] { P("App", "1.0.0"), P("Plugin", "1.0.0") },
                package => package.Id == "App" ? new List<NuGetPackage> { D("Shared", "1.5.0") }
                    : package.Id == "Plugin" ? new List<NuGetPackage> { D("Shared", "[1.0.0,1.4.0]") }
                    : new List<NuGetPackage>(), _ => false, out _);
            Assert.Contains(errors, error => error.Contains("Plugin 1.0.0") && error.Contains("[1.0.0,1.4.0]"));
        }

        [Fact]
        public void UnstableGraphFailsInsteadOfInstallingAPartialSelection()
        {
            var errors = NuGetPackagePlanner.Resolve(new[] { P("A", "1.0.0") }, package =>
                package.Id == "A" ? new List<NuGetPackage> { D("B", package.Version == "1.0.0" ? "1.0.0" : "2.0.0") }
                    : new List<NuGetPackage> { D("A", package.Version == "1.0.0" ? "2.0.0" : "1.0.0") },
                _ => false, out _);
            Assert.Contains(errors, error => error.Contains("did not converge"));
        }

        [Fact]
        public void ExtractorRetainsOriginalRangeInNuspec()
        {
            var path = Path.GetTempFileName();
            try
            {
                using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
                using (var writer = new StreamWriter(zip.CreateEntry("Parent.nuspec").Open()))
                    writer.Write("<package><metadata><dependencies><dependency id='Shared' version='(1.0.0,2.0.0]'/></dependencies></metadata></package>");
                var dependency = Assert.Single(NuGetExtractor.GetDependencies(path));
                Assert.Equal("(1.0.0,2.0.0]", dependency.VersionRange!.Value.Text);
                Assert.False(dependency.VersionRange.Value.Contains("1.0.0"));
            }
            finally { File.Delete(path); }
        }
    }
}
