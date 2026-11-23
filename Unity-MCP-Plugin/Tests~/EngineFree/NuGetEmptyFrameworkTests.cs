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
using System.IO;
using System.IO.Compression;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    public class NuGetEmptyFrameworkTests : IDisposable
    {
        readonly string _folder = Path.Combine(Path.GetTempPath(), "UnityMcp-EmptyFramework-" + Path.GetRandomFileName());

        public NuGetEmptyFrameworkTests() => Directory.CreateDirectory(_folder);
        public void Dispose() => Directory.Delete(_folder, recursive: true);

        string Package(params string[] entries)
        {
            var path = Path.Combine(_folder, Path.GetRandomFileName() + ".nupkg");
            using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
            foreach (var name in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write("fixture");
            }
            return path;
        }

        [Theory]
        [InlineData("lib/netstandard2.0/_._")]
        [InlineData("LIB/NETSTANDARD2.0/_._")]
        public void EmptyStandardGroupDoesNotFallBackToFrameworkDll(string marker)
        {
            // The lib layout of System.ValueTuple 4.6.1.
            var package = Package("lib/net462/System.ValueTuple.dll", "lib/net47/System.ValueTuple.dll",
                "lib/net471/_._", "lib/netcoreapp2.0/_._", marker);
            var planned = NuGetExtractor.PlanDllPaths(package, _folder, out var empty);

            Assert.True(empty);
            Assert.Empty(planned);
            Assert.Empty(NuGetExtractor.ExtractDlls(package, Path.Combine(_folder, "install")));
            Assert.Empty(Directory.GetFiles(Path.Combine(_folder, "install")));
        }

        [Fact]
        public void HigherPriorityDllWinsOverLowerPriorityEmptyGroup()
        {
            var package = Package("lib/netstandard2.1/Game.dll", "lib/netstandard2.0/_._");
            var planned = NuGetExtractor.PlanDllPaths(package, _folder, out var empty);

            Assert.False(empty);
            Assert.Equal("lib/netstandard2.1/Game.dll", Assert.Single(planned).EntryFullName);
        }

        [Fact]
        public void HigherPriorityEmptyGroupWinsOverLowerPriorityDll()
        {
            var package = Package("lib/netstandard2.1/_._", "lib/netstandard2.0/Game.dll");
            Assert.Empty(NuGetExtractor.PlanDllPaths(package, _folder, out var empty));
            Assert.True(empty);
        }

        [Fact]
        public void PlaceholderDoesNotHideDllInItsOwnGroup()
        {
            var package = Package("lib/netstandard2.0/_._", "lib/netstandard2.0/Game.dll");
            var planned = NuGetExtractor.PlanDllPaths(package, _folder, out var empty);

            Assert.False(empty);
            Assert.Equal("Game.dll", Assert.Single(planned).FileName);
        }

        [Theory]
        [InlineData("lib/netstandard2.1/fr/_._")]
        [InlineData("content/_._")]
        [InlineData("lib/netstandard2.1/readme.txt")]
        public void UnrelatedFilesDoNotCreateEmptyGroups(string other)
        {
            var package = Package(other, "lib/netstandard2.0/Game.dll");
            Assert.Equal("Game.dll", Assert.Single(NuGetExtractor.PlanDllPaths(package, _folder)).FileName);
        }

        [Fact]
        public void MissingAssetsAreNotAnExplicitEmptyFramework()
        {
            Assert.Empty(NuGetExtractor.PlanDllPaths(Package("readme.txt"), _folder, out var empty));
            Assert.False(empty);
        }

        [Fact]
        public void EmptyFrameworkStillHasItsDeclaredDependencies()
        {
            var package = Package("lib/netstandard2.0/_._", "lib/net47/Game.dll");
            using (var zip = ZipFile.Open(package, ZipArchiveMode.Update))
            using (var writer = new StreamWriter(zip.CreateEntry("Game.nuspec").Open()))
                writer.Write("<package><metadata><dependencies>" +
                    "<group targetFramework='.NETFramework4.7'><dependency id='Wrong' version='1.0.0'/></group>" +
                    "<group targetFramework='.NETStandard2.0'><dependency id='Right' version='2.0.0'/></group>" +
                    "</dependencies></metadata></package>");

            var dependency = Assert.Single(NuGetExtractor.GetDependencies(package));
            Assert.Equal("Right", dependency.Id);
            Assert.Equal("2.0.0", dependency.Version);
        }
    }
}
