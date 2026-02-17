/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler
    {
        private string _testFilePath = null!;

        [SetUp]
        public void SaveLoadSetUp()
        {
            _testFilePath = Path.Combine(Application.temporaryCachePath, "profiler_test_data.json");
        }

        [TearDown]
        public void SaveLoadTearDown()
        {
            // Clean up test file
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }

        [Test]
        public void SaveData_WithEmptyPath_ReturnsError()
        {
            // Act
            var result = _tool.SaveData(string.Empty);

            // Assert
            ErrorValidation(result, "File path is required");
        }

        [Test]
        public void SaveData_WithNullPath_ReturnsError()
        {
            // Act
            var result = _tool.SaveData(null!);

            // Assert
            ErrorValidation(result, "File path is required");
        }

        [UnityTest]
        public IEnumerator SaveData_WithValidPath_SavesDataToFile()
        {
            // Arrange
            yield return null;

            // Act
            var result = _tool.SaveData(_testFilePath);

            // Assert
            ResultValidationExpected(result, "Profiler data saved to");
            Assert.IsTrue(File.Exists(_testFilePath), "File should be created.");
        }

        [UnityTest]
        public IEnumerator SaveData_CreatesValidJsonFile()
        {
            // Arrange
            yield return null;

            // Act
            var result = _tool.SaveData(_testFilePath);

            // Assert
            ResultValidation(result);
            Assert.IsTrue(File.Exists(_testFilePath), "File should be created.");

            var content = File.ReadAllText(_testFilePath);
            Assert.IsNotEmpty(content, "File content should not be empty.");
            Assert.IsTrue(content.Contains("timestamp"), "File should contain timestamp.");
            Assert.IsTrue(content.Contains("memory"), "File should contain memory data.");
            Assert.IsTrue(content.Contains("performance"), "File should contain performance data.");
        }

        [Test]
        public void LoadData_WithEmptyPath_ReturnsError()
        {
            // Act
            var result = _tool.LoadData(string.Empty);

            // Assert
            ErrorValidation(result, "File path is required");
        }

        [Test]
        public void LoadData_WithNullPath_ReturnsError()
        {
            // Act
            var result = _tool.LoadData(null!);

            // Assert
            ErrorValidation(result, "File path is required");
        }

        [Test]
        public void LoadData_WithNonExistentFile_ReturnsError()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Application.temporaryCachePath, "non_existent_file.json");

            // Act
            var result = _tool.LoadData(nonExistentPath);

            // Assert
            ErrorValidation(result, "file not found");
        }

        [UnityTest]
        public IEnumerator LoadData_WithValidFile_LoadsData()
        {
            // Arrange - save data first
            _tool.SaveData(_testFilePath);
            yield return null;

            // Act
            var result = _tool.LoadData(_testFilePath);

            // Assert
            ResultValidationExpected(result, "Profiler data loaded from");
        }

        [UnityTest]
        public IEnumerator SaveLoad_RoundTrip_PreservesData()
        {
            // Arrange - save data
            _tool.SaveData(_testFilePath);
            yield return null;

            // Act - load data
            var result = _tool.LoadData(_testFilePath);

            // Assert
            ResultValidation(result);
            Assert.IsTrue(result!.Contains("timestamp"), "Loaded data should contain timestamp.");
            Assert.IsTrue(result.Contains("memory"), "Loaded data should contain memory.");
        }

        [Test]
        public void ClearData_ReturnsSuccess()
        {
            // Act
            var result = _tool.ClearData();

            // Assert
            ResultValidationExpected(result, "Profiler data cleared successfully");
        }
    }
}

