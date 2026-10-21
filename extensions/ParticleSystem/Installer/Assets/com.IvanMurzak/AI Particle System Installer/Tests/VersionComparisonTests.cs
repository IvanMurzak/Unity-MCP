/*
┌────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                       │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-Package-Template) │
│  Copyright (c) 2025 Ivan Murzak                                            │
│  Licensed under the MIT License.                                           │
│  See the LICENSE file in the project root for more information.            │
└────────────────────────────────────────────────────────────────────────────┘
*/
using System.IO;
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.ParticleSystem.Installer.SimpleJSON;

namespace com.IvanMurzak.Unity.MCP.ParticleSystem.Installer.Tests
{
    public class VersionComparisonTests
    {
        const string TestManifestPath = "Temp/com.IvanMurzak.Unity.MCP.ParticleSystem.Installer.Tests/test_manifest.json";
        const string PackageId = "com.ivanmurzak.unity.mcp.particlesystem";

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

            // Assert - version was upgraded and is within valid bounds
            var updatedContent = File.ReadAllText(TestManifestPath);
            var updatedManifest = JSONObject.Parse(updatedContent);
            var actualVersionStr = updatedManifest[Installer.Dependencies][PackageId].ToString().Trim('"');
            var actualVersion = new System.Version(actualVersionStr);

            Assert.Greater(actualVersion, new System.Version(lowerVersion),
                "Version should be upgraded above the initial lower version");
            Assert.LessOrEqual(actualVersion, new System.Version(Installer.Version),
                "Version should not exceed the installer version");
        }

        [Test]
        public void AddScopedRegistryIfNeeded_AllowVersionUpgrade_WritesResolvedVersion()
        {
            // Arrange - Create manifest with lower version (0.0.1 is always lower)
            const string knownVersion = "1.2.3";
            var manifest = new JSONObject
            {
                [Installer.Dependencies] = new JSONObject
                {
                    [PackageId] = "0.0.1"
                },
                [Installer.ScopedRegistries] = new JSONArray()
            };
            File.WriteAllText(TestManifestPath, manifest.ToString(2));

            // Act - inject a known version, bypassing OpenUPM
            Installer.AddScopedRegistryIfNeeded(TestManifestPath, knownVersion);

            // Assert - version was upgraded to exactly the injected version
            var updatedManifest = JSONObject.Parse(File.ReadAllText(TestManifestPath));
            var actualVersion = updatedManifest[Installer.Dependencies][PackageId].ToString().Trim('"');

            Assert.AreEqual(knownVersion, actualVersion,
                "Package should be upgraded to exactly the injected version");
        }

        [Test]
        public void AddScopedRegistryIfNeeded_NoExistingDependency_WritesResolvedVersion()
        {
            // Arrange - Create manifest without the package
            const string knownVersion = "1.2.3";
            var manifest = new JSONObject
            {
                [Installer.Dependencies] = new JSONObject(),
                [Installer.ScopedRegistries] = new JSONArray()
            };
            File.WriteAllText(TestManifestPath, manifest.ToString(2));

            // Act - inject a known version, bypassing OpenUPM
            Installer.AddScopedRegistryIfNeeded(TestManifestPath, knownVersion);

            // Assert - package is present with exactly the injected version
            var updatedManifest = JSONObject.Parse(File.ReadAllText(TestManifestPath));
            var actualVersion = updatedManifest[Installer.Dependencies][PackageId].ToString().Trim('"');

            Assert.AreEqual(knownVersion, actualVersion,
                "Package should be installed with exactly the injected version");
        }

        [Test]
        public void AddScopedRegistryIfNeeded_NoExistingDependency_Integration()
        {
            // Arrange - Create manifest without the package
            var manifest = new JSONObject
            {
                [Installer.Dependencies] = new JSONObject(),
                [Installer.ScopedRegistries] = new JSONArray()
            };
            File.WriteAllText(TestManifestPath, manifest.ToString(2));

            // Act - Run installer (queries OpenUPM)
            Installer.AddScopedRegistryIfNeeded(TestManifestPath);

            // Assert - package is present with a valid semver <= installer version
            var updatedManifest = JSONObject.Parse(File.ReadAllText(TestManifestPath));
            var actualVersionStr = updatedManifest[Installer.Dependencies][PackageId].ToString().Trim('"');
            var actualVersion = new System.Version(actualVersionStr);

            Assert.LessOrEqual(actualVersion, new System.Version(Installer.Version),
                "Installed version should not exceed the installer version");
        }
    }
}