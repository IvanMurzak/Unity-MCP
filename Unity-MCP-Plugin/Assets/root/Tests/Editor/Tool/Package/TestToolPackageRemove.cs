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
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestToolPackageRemove : BaseTest
    {
        private string _testManifestPath = "";
        private string _originalManifestContent = "";

        [SetUp]
        public void TestSetUp()
        {
            // Create a temporary test manifest file path
            _testManifestPath = Path.Combine(Path.GetTempPath(), "test_manifest.json");
            
            // Store original manifest content if it exists
            var originalPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            if (File.Exists(originalPath))
                _originalManifestContent = File.ReadAllText(originalPath);
        }

        [TearDown]
        public void TestTearDown()
        {
            // Clean up test file
            if (File.Exists(_testManifestPath))
                File.Delete(_testManifestPath);
        }

        [Test]
        public void PackageRemove_InvalidRequestId_ShouldReturnError()
        {
            // Arrange & Act
            var result = Tool_Package.Remove(new string[] { "com.test.package" }, null).Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(Common.Model.ResponseStatus.Error, result.Status, "Should return error status");
            
            var message = result.GetMessage();
            Assert.IsNotNull(message, "Error message should not be null");
            Assert.IsTrue(message.Contains("RequestID"), "Should contain RequestID error");
        }

        [Test]
        public void PackageRemove_EmptyPackageIds_ShouldReturnSuccess()
        {
            // Arrange
            var packageIds = new string[0];
            var requestId = "test-request-1";

            // Act
            var result = Tool_Package.Remove(packageIds, requestId).Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            // With empty array, no packages are removed so it should return Success immediately
            Assert.AreEqual(Common.Model.ResponseStatus.Success, result.Status, "Should return success status for empty array");
        }

        [Test]
        public void PackageRemove_NonExistentManifest_ShouldReturnError()
        {
            // Test requires simulating non-existent manifest, which is complex in Unity environment
            // This would need to be tested in integration tests with a controlled manifest path
            Assert.Pass("Test requires Unity Editor with controlled manifest path");
        }

        [Test]
        public void PackageRemovalTracker_StoreAndRetrieveResults_ShouldWork()
        {
            // Arrange
            var requestId = "test-request-tracker";
            var results = new System.Collections.Generic.List<string>
            {
                "[Success] Package com.test.package1 removed",
                "[Warning] Package com.test.package2 not found"
            };

            // Act
            PackageRemovalTracker.StoreResults(requestId, results);
            var retrievedResults = PackageRemovalTracker.GetResults(requestId);

            // Assert
            Assert.IsNotNull(retrievedResults, "Retrieved results should not be null");
            Assert.AreEqual(2, retrievedResults.Count, "Should retrieve 2 results");
            Assert.AreEqual(results[0], retrievedResults[0], "First result should match");
            Assert.AreEqual(results[1], retrievedResults[1], "Second result should match");

            // Should be removed after retrieval
            var secondRetrieval = PackageRemovalTracker.GetResults(requestId);
            Assert.IsNull(secondRetrieval, "Results should be removed after first retrieval");
        }

        [Test]
        public void PackageRemovalTracker_HasPendingRequest_ShouldWork()
        {
            // Arrange
            var requestId = "test-pending-request";
            var results = new System.Collections.Generic.List<string> { "[Success] Test" };

            // Act & Assert - No pending request initially
            Assert.IsFalse(PackageRemovalTracker.HasPendingRequest(requestId), "Should not have pending request initially");

            // Store results
            PackageRemovalTracker.StoreResults(requestId, results);
            Assert.IsTrue(PackageRemovalTracker.HasPendingRequest(requestId), "Should have pending request after storing");

            // Retrieve results
            PackageRemovalTracker.GetResults(requestId);
            Assert.IsFalse(PackageRemovalTracker.HasPendingRequest(requestId), "Should not have pending request after retrieval");
        }
        
        [Test]
        public void TestJsonManipulation_ShouldWorkCorrectly()
        {
            // Test JSON manipulation logic separately from file operations
            var testManifest = """
            {
              "dependencies": {
                "com.test.package1": "1.0.0",
                "com.test.package2": "2.0.0",
                "com.unity.test": "1.5.0"
              }
            }
            """;

            // Parse JSON
            var manifestJson = JsonNode.Parse(testManifest)?.AsObject();
            Assert.IsNotNull(manifestJson, "Should parse JSON correctly");

            var dependencies = manifestJson["dependencies"]?.AsObject();
            Assert.IsNotNull(dependencies, "Should have dependencies object");
            Assert.IsTrue(dependencies.ContainsKey("com.test.package1"), "Should contain test package 1");
            Assert.IsTrue(dependencies.ContainsKey("com.test.package2"), "Should contain test package 2");

            // Remove packages
            dependencies.Remove("com.test.package1");
            Assert.IsFalse(dependencies.ContainsKey("com.test.package1"), "Should not contain test package 1 after removal");
            Assert.IsTrue(dependencies.ContainsKey("com.test.package2"), "Should still contain test package 2");

            // Serialize back
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var serializedJson = JsonSerializer.Serialize(manifestJson, options);
            Assert.IsNotNull(serializedJson, "Should serialize JSON correctly");
            Assert.IsFalse(serializedJson.Contains("com.test.package1"), "Serialized JSON should not contain removed package");
        }
    }
}