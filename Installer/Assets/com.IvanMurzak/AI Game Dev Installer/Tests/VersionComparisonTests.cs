/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.IO;
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Installer.SimpleJSON;

namespace com.IvanMurzak.Unity.MCP.Installer.Tests
{
    public class VersionComparisonTests
    {
        const string TestManifestPath = "Temp/com.IvanMurzak/Unity.MCP.Installer.Tests/test_manifest.json";
        const string PackageId = "com.ivanmurzak.unity.mcp";

        [SetUp]
        public void SetUp()
        {
            var dir = Path.GetDirectoryName(TestManifestPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(TestManifestPath))
                File.Delete(TestManifestPath);
        }

        [Test]
        public void ShouldUpdateVersion_PatchVersionHigher_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "1.5.1",
                    installerVersion: "1.5.2"
                ),
                message: "Should update when patch version is higher"
            );
        }

        [Test]
        public void ShouldUpdateVersion_PatchVersionLower_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "1.5.2",
                    installerVersion: "1.5.1"
                ),
                message: "Should not downgrade when patch version is lower"
            );
        }

        [Test]
        public void ShouldUpdateVersion_MinorVersionHigher_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "1.5.0",
                    installerVersion: "1.6.0"
                ),
                message: "Should update when minor version is higher"
            );
        }

        [Test]
        public void ShouldUpdateVersion_MinorVersionLower_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "1.6.0",
                    installerVersion: "1.5.0"
                ),
                message: "Should not downgrade when minor version is lower"
            );
        }

        [Test]
        public void ShouldUpdateVersion_MajorVersionHigher_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "1.5.0",
                    installerVersion: "2.0.0"
                ),
                message: "Should update when major version is higher"
            );
        }

        [Test]
        public void ShouldUpdateVersion_MajorVersionLower_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "2.0.0",
                    installerVersion: "1.5.0"
                ),
                message: "Should not downgrade when major version is lower"
            );
        }

        [Test]
        public void ShouldUpdateVersion_SameVersion_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "1.5.2",
                    installerVersion: "1.5.2"
                ),
                message: "Should not update when versions are the same"
            );
        }

        [Test]
        public void ShouldUpdateVersion_EmptyCurrentVersion_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "",
                    installerVersion: "1.5.2"
                ),
                message: "Should install when no current version exists"
            );
        }

        [Test]
        public void ShouldUpdateVersion_NullCurrentVersion_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: null,
                    installerVersion: "1.5.2"
                ),
                message: "Should install when current version is null"
            );
        }

        [Test]
        public void AddScopedRegistryIfNeeded_PreventVersionDowngrade_Integration()
        {
            // Arrange - Create manifest with higher version
            var versionParts = Installer.Version.Split('.');
            var majorVersion = int.Parse(versionParts[0]);
            var higherVersion = $"{majorVersion + 10}.0.0";
            var manifest = new JSONObject
            {
                [Installer.Dependencies] = new JSONObject
                {
                    [PackageId] = higherVersion
                },
                [Installer.ScopedRegistries] = new JSONArray()
            };
            File.WriteAllText(TestManifestPath, manifest.ToString(2));

            // Act - Run installer (should NOT downgrade)
            Installer.AddScopedRegistryIfNeeded(TestManifestPath);

            // Assert - Version should remain unchanged
            var updatedContent = File.ReadAllText(TestManifestPath);
            var updatedManifest = JSONObject.Parse(updatedContent);
            var actualVersion = updatedManifest[Installer.Dependencies][PackageId];

            Assert.AreEqual(higherVersion, actualVersion.ToString().Trim('"'),
                "Version should not be downgraded from higher version");
        }

        [Test]
        public void AddScopedRegistryIfNeeded_AllowVersionUpgrade_Integration()
        {
            // Arrange - Create manifest with lower version (0.0.1 is always lower)
            var lowerVersion = "0.0.1";
            var manifest = new JSONObject
            {
                [Installer.Dependencies] = new JSONObject
                {
                    [PackageId] = lowerVersion
                },
                [Installer.ScopedRegistries] = new JSONArray()
            };
            File.WriteAllText(TestManifestPath, manifest.ToString(2));

            // Act - Run installer (should upgrade)
            Installer.AddScopedRegistryIfNeeded(TestManifestPath);

            // Assert - Version should be upgraded to installer version
            var updatedContent = File.ReadAllText(TestManifestPath);
            var updatedManifest = JSONObject.Parse(updatedContent);
            var actualVersion = updatedManifest[Installer.Dependencies][PackageId];

            Assert.AreEqual(Installer.Version, actualVersion.ToString().Trim('"'),
                "Version should be upgraded to installer version");
        }

        [Test]
        public void AddScopedRegistryIfNeeded_NoExistingDependency_InstallsNewVersion()
        {
            // Arrange - Create manifest without the package
            var manifest = new JSONObject
            {
                [Installer.Dependencies] = new JSONObject(),
                [Installer.ScopedRegistries] = new JSONArray()
            };
            File.WriteAllText(TestManifestPath, manifest.ToString(2));

            // Act - Run installer
            Installer.AddScopedRegistryIfNeeded(TestManifestPath);

            // Assert - Package should be added with installer version
            var updatedContent = File.ReadAllText(TestManifestPath);
            var updatedManifest = JSONObject.Parse(updatedContent);
            var actualVersion = updatedManifest[Installer.Dependencies][PackageId];

            Assert.AreEqual(Installer.Version, actualVersion.ToString().Trim('"'),
                "New package should be installed with installer version");
        }
    }
}