/*
┌────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                       │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-Package-Template) │
│  Copyright (c) 2025 Ivan Murzak                                            │
│  Licensed under the MIT License.                                           │
│  See the LICENSE file in the project root for more information.            │
└────────────────────────────────────────────────────────────────────────────┘
*/
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.ParticleSystem.Installer.Tests
{
    public class ManifestInstallerTests
    {
        const string PackageIdTag = "PACKAGE_ID";
        const string PackageVersionTag = "PACKAGE_VERSION";
        const string FilesRoot = "Assets/com.IvanMurzak/AI Particle System Installer/Tests/Files";
        const string FilesCopyRoot = "Temp/AI Particle System Installer/Tests/Files";
        static string CorrectManifestPath => $"{FilesRoot}/Correct/correct_manifest.json";

        // Fixture names are enumerated once at test-discovery time so each appears as
        // its own test case in the runner — a failure points at a specific fixture
        // instead of hiding inside a single combined "All" test.
        static IEnumerable<string> FixtureNames()
        {
            foreach (var path in Directory.GetFiles(FilesRoot, "*.json", SearchOption.TopDirectoryOnly))
                yield return Path.GetFileName(path);
        }

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(FilesCopyRoot);

        [TearDown]
        public void TearDown()
        {
            if (!Directory.Exists(FilesCopyRoot))
                return;
            foreach (var file in Directory.GetFiles(FilesCopyRoot, "*.json", SearchOption.TopDirectoryOnly))
                File.Delete(file);
        }

        [TestCaseSource(nameof(FixtureNames))]
        public void NormalizesManifestTo_CorrectManifest(string fixtureName)
        {
            // Arrange — copy fixture to temp, substitute tags, keep Installer.Version
            // deterministic so tests don't depend on OpenUPM's live response.
            var src = Path.Combine(FilesRoot, fixtureName);
            var dst = Path.Combine(FilesCopyRoot, fixtureName);
            File.WriteAllText(dst, File.ReadAllText(src)
                .Replace(PackageVersionTag, Installer.Version)
                .Replace(PackageIdTag, Installer.PackageId));

            var expected = File.ReadAllText(CorrectManifestPath)
                .Replace(PackageVersionTag, Installer.Version)
                .Replace(PackageIdTag, Installer.PackageId);

            // Act — use the version-aware overload so the test is offline-safe.
            Installer.AddScopedRegistryIfNeeded(dst, Installer.Version);

            // Assert
            Assert.AreEqual(expected, File.ReadAllText(dst),
                $"Normalized manifest from '{fixtureName}' does not match the correct manifest.");
        }
    }
}
