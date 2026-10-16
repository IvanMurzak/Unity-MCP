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
using UnityEngine;

namespace YOUR_PACKAGE_ID.Installer.Tests
{
    public class ManifestInstallerTests
    {
        const string PackageIdTag = "PACKAGE_ID";
        const string PackageVersionTag = "PACKAGE_VERSION";
        const string FilesRoot = "Assets/YOUR_PACKAGE_NAME_INSTALLER/Tests/Files";
        const string FilesCopyRoot = "Temp/YOUR_PACKAGE_NAME_INSTALLER/Tests/Files";
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
            var correctManifest = File.ReadAllText(CorrectManifestPath)
                .Replace(PackageVersionTag, Installer.Version)
                .Replace(PackageIdTag, Installer.PackageId);

            foreach (var file in files)
            {
                Debug.Log($"Found JSON file: {file}");

                // Copy the file
                var fileCopy = Path.Combine(FilesCopyRoot, Path.GetFileName(file));
                File.Copy(file, fileCopy, overwrite: true);

                // Arrange
                File.WriteAllText(fileCopy, File.ReadAllText(fileCopy)
                    .Replace(PackageVersionTag, Installer.Version)
                    .Replace(PackageIdTag, Installer.PackageId));

                // Act
                Installer.AddScopedRegistryIfNeeded(fileCopy);

                // Assert
                var modifiedManifest = File.ReadAllText(fileCopy);
                Assert.AreEqual(correctManifest, modifiedManifest, $"Modified manifest from {file} does not match the correct manifest.");
            }
        }
    }
}
