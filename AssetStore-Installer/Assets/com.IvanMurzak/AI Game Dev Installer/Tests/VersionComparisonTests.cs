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
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SimpleJSON;

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

        /// <summary>
        /// Uses reflection to access the private ShouldUpdateVersion method for testing
        /// </summary>
        private bool ShouldUpdateVersion(string currentVersion, string installerVersion)
        {
            var method = typeof(Installer).GetMethod("ShouldUpdateVersion", 
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "ShouldUpdateVersion method not found");
            return (bool)method.Invoke(null, new object[] { currentVersion, installerVersion });
        }

        [Test]
        public void ShouldUpdateVersion_InstallerHigher_ReturnsTrue()
        {
            // Arrange
            var currentVersion = "0.17.1";
            var installerVersion = "0.17.2";

            // Act
            var result = ShouldUpdateVersion(currentVersion, installerVersion);

            // Assert
            Assert.IsTrue(result, "Should update when installer version is higher");
        }

        [Test]
        public void ShouldUpdateVersion_InstallerLower_ReturnsFalse()
        {
            // Arrange
            var currentVersion = "0.18.0";
            var installerVersion = "0.17.2";

            // Act
            var result = ShouldUpdateVersion(currentVersion, installerVersion);

            // Assert
            Assert.IsFalse(result, "Should not downgrade when installer version is lower");
        }

        [Test]
        public void ShouldUpdateVersion_SameVersion_ReturnsFalse()
        {
            // Arrange
            var currentVersion = "0.17.2";
            var installerVersion = "0.17.2";

            // Act
            var result = ShouldUpdateVersion(currentVersion, installerVersion);

            // Assert
            Assert.IsFalse(result, "Should not update when versions are the same");
        }

        [Test]
        public void ShouldUpdateVersion_NoCurrentVersion_ReturnsTrue()
        {
            // Arrange
            var currentVersion = "";
            var installerVersion = "0.17.2";

            // Act
            var result = ShouldUpdateVersion(currentVersion, installerVersion);

            // Assert
            Assert.IsTrue(result, "Should install when no current version exists");
        }

        [Test]
        public void ShouldUpdateVersion_NullCurrentVersion_ReturnsTrue()
        {
            // Arrange
            string currentVersion = null;
            var installerVersion = "0.17.2";

            // Act
            var result = ShouldUpdateVersion(currentVersion, installerVersion);

            // Assert
            Assert.IsTrue(result, "Should install when current version is null");
        }

        [Test]
        public void ShouldUpdateVersion_MajorVersionDifference_WorksCorrectly()
        {
            // Test major version upgrade
            Assert.IsTrue(ShouldUpdateVersion("0.17.2", "1.0.0"), 
                "Should upgrade from 0.17.2 to 1.0.0");
            
            // Test major version downgrade prevention
            Assert.IsFalse(ShouldUpdateVersion("1.0.0", "0.17.2"), 
                "Should not downgrade from 1.0.0 to 0.17.2");
        }

        [Test]
        public void AddScopedRegistryIfNeeded_PreventVersionDowngrade_Integration()
        {
            // Arrange - Create manifest with higher version
            var higherVersion = "0.18.0";
            var manifest = new JSONObject
            {
                ["dependencies"] = new JSONObject
                {
                    [PackageId] = higherVersion
                },
                ["scopedRegistries"] = new JSONArray()
            };
            File.WriteAllText(TestManifestPath, manifest.ToString(2));

            // Act - Run installer (should NOT downgrade)
            Installer.AddScopedRegistryIfNeeded(TestManifestPath);

            // Assert - Version should remain unchanged
            var updatedContent = File.ReadAllText(TestManifestPath);
            var updatedManifest = JSONObject.Parse(updatedContent);
            var actualVersion = updatedManifest["dependencies"][PackageId];
            
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
                ["dependencies"] = new JSONObject
                {
                    [PackageId] = lowerVersion
                },
                ["scopedRegistries"] = new JSONArray()
            };
            File.WriteAllText(TestManifestPath, manifest.ToString(2));

            // Act - Run installer (should upgrade)
            Installer.AddScopedRegistryIfNeeded(TestManifestPath);

            // Assert - Version should be upgraded to installer version
            var updatedContent = File.ReadAllText(TestManifestPath);
            var updatedManifest = JSONObject.Parse(updatedContent);
            var actualVersion = updatedManifest["dependencies"][PackageId];
            
            Assert.AreEqual(Installer.Version, actualVersion.ToString().Trim('"'), 
                "Version should be upgraded to installer version");
        }

        [Test]
        public void AddScopedRegistryIfNeeded_NoExistingDependency_InstallsNewVersion()
        {
            // Arrange - Create manifest without the package
            var manifest = new JSONObject
            {
                ["dependencies"] = new JSONObject(),
                ["scopedRegistries"] = new JSONArray()
            };
            File.WriteAllText(TestManifestPath, manifest.ToString(2));

            // Act - Run installer
            Installer.AddScopedRegistryIfNeeded(TestManifestPath);

            // Assert - Package should be added with installer version
            var updatedContent = File.ReadAllText(TestManifestPath);
            var updatedManifest = JSONObject.Parse(updatedContent);
            var actualVersion = updatedManifest["dependencies"][PackageId];
            
            Assert.AreEqual(Installer.Version, actualVersion.ToString().Trim('"'), 
                "New package should be installed with installer version");
        }
    }
}