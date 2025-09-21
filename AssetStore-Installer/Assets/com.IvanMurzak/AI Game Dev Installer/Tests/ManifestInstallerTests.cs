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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Installer.Tests
{
    public class ManifestInstallerTests
    {
        const string UnityMcpVersionTag = "UNITY_MCP_VERSION";
        const string FilesRoot = "Assets/com.IvanMurzak/AI Game Dev Installer/Tests/Files";
        const string FilesCopyRoot = "Temp/com.IvanMurzak/AI Game Dev Installer/Tests/Files";
        static string CorrectManifestPath => $"{FilesRoot}/Correct/correct_manifest.json";


        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[{nameof(ManifestInstallerTests)}] SetUp");
            Directory.CreateDirectory(FilesCopyRoot);
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log($"[{nameof(ManifestInstallerTests)}] TearDown");

            // var files = Directory.GetFiles(FilesCopyRoot, "*.json", SearchOption.TopDirectoryOnly);
            // foreach (var file in files)
            //     File.Delete(file);
        }

        [Test]
        public void All()
        {
            var files = Directory.GetFiles(FilesRoot, "*.json", SearchOption.TopDirectoryOnly);
            var correctManifest = File.ReadAllText(CorrectManifestPath).Replace(UnityMcpVersionTag, Installer.Version);

            foreach (var file in files)
            {
                Debug.Log($"Found JSON file: {file}");

                // Skip version-specific test files - they are handled by VersionComparisonTests
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith("version_"))
                {
                    Debug.Log($"Skipping version-specific test file: {fileName} (handled by VersionComparisonTests)");
                    continue;
                }

                // Copy the file
                var fileCopy = Path.Combine(FilesCopyRoot, Path.GetFileName(file));
                File.Copy(file, fileCopy, overwrite: true);

                // Arrange
                File.WriteAllText(fileCopy, File.ReadAllText(fileCopy).Replace(UnityMcpVersionTag, Installer.Version));

                // Act
                Installer.AddScopedRegistryIfNeeded(fileCopy);

                // Assert
                var modifiedManifest = File.ReadAllText(fileCopy);
                Assert.AreEqual(correctManifest, modifiedManifest, $"Modified manifest from {file} does not match the correct manifest.");
            }
        }
    }
}
