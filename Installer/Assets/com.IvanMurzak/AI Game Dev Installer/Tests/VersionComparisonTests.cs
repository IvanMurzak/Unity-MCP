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
        public void ShouldUpdateVersion_InstallerHigher_ReturnsTrue()
        {
            // Assert
            Assert.IsTrue(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "0.17.1",
                    installerVersion: "0.17.2"
                ),
                message: "Should update when installer version is higher"
            );

            Assert.IsTrue(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "0.9.0",
                    installerVersion: "0.17.0"
                ),
                message: "Should update when installer version is higher"
            );

            Assert.IsTrue(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "0.17.1",
                    installerVersion: "1.0.0"
                ),
                message: "Should update when installer version is higher"
            );
        }

        [Test]
        public void ShouldUpdateVersion_InstallerLower_ReturnsFalse()
        {
            // Act
            var result = Installer.ShouldUpdateVersion(
                currentVersion: "0.18.0",
                installerVersion: "0.17.2"
            );

            // Assert
            Assert.IsFalse(result, "Should not downgrade when installer version is lower");
        }

        [Test]
        public void ShouldUpdateVersion_SameVersion_ReturnsFalse()
        {
            // Act
            var result = Installer.ShouldUpdateVersion(
                currentVersion: "0.17.2",
                installerVersion: "0.17.2"
            );

            // Assert
            Assert.IsFalse(result, "Should not update when versions are the same");
        }

        [Test]
        public void ShouldUpdateVersion_NoCurrentVersion_ReturnsTrue()
        {
            // Act
            var result = Installer.ShouldUpdateVersion(
                currentVersion: "",
                installerVersion: "0.17.2"
            );

            // Assert
            Assert.IsTrue(result, "Should install when no current version exists");
        }

        [Test]
        public void ShouldUpdateVersion_NullCurrentVersion_ReturnsTrue()
        {
            // Act
            var result = Installer.ShouldUpdateVersion(
                currentVersion: null,
                installerVersion: "0.17.2"
            );

            // Assert
            Assert.IsTrue(result, "Should install when current version is null");
        }

        [Test]
        public void ShouldUpdateVersion_MajorVersionDifference_WorksCorrectly()
        {
            // Test major version upgrade
            Assert.IsTrue(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "0.17.2",
                    installerVersion: "1.0.0"
                ),
                message: "Should upgrade from 0.17.2 to 1.0.0"
            );

            // Test major version downgrade prevention
            Assert.IsFalse(
                condition: Installer.ShouldUpdateVersion(
                    currentVersion: "1.0.0",
                    installerVersion: "0.17.2"
                ),
                message: "Should not downgrade from 1.0.0 to 0.17.2"
            );
        }

        [Test]
        public void AddScopedRegistryIfNeeded_PreventVersionDowngrade_Integration()
        {
            // Arrange - Create manifest with higher version
            var higherVersion = "0.18.0";
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
            // Arrange - Create manifest with lower version
            var lowerVersion = "0.16.0";
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