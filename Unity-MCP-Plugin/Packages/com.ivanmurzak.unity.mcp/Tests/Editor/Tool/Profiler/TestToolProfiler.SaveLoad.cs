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
using System.IO;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler
    {
        string _tempPath = null!;

        [SetUp]
        public void TestSetUp_SaveLoad()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), $"unity-mcp-profiler-test-{System.Guid.NewGuid():N}.json");
        }

        [TearDown]
        public void TestTearDown_SaveLoad()
        {
            if (!string.IsNullOrEmpty(_tempPath) && File.Exists(_tempPath))
                File.Delete(_tempPath);
        }

        [Test]
        public void SaveData_WritesValidJsonFile()
        {
            var result = _tool.SaveData(_tempPath);
            Assert.IsTrue(result.Contains("[Success]"), $"SaveData should succeed.\n{result}");
            Assert.IsTrue(File.Exists(_tempPath), "Output file should exist on disk after SaveData.");

            var content = File.ReadAllText(_tempPath);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content), "Snapshot file should not be empty.");
            // Lightweight JSON shape check — avoids dragging in a JSON dep at test time.
            Assert.IsTrue(content.TrimStart().StartsWith("{"), "Snapshot should begin with '{'.");
            Assert.IsTrue(content.Contains("\"status\""), "Snapshot should contain a 'status' field.");
            Assert.IsTrue(content.Contains("\"memory\""), "Snapshot should contain a 'memory' field.");
        }

        [Test]
        public void SaveData_RejectsEmptyFilePath()
        {
            var result = _tool.SaveData("");
            Assert.IsTrue(result.Contains("[Error]"), $"Empty path should return error.\n{result}");
            Assert.IsTrue(result.Contains("required"), "Error should mention required.");
        }

        [Test]
        public void LoadData_ReturnsContentForExistingFile()
        {
            // Round-trip — save then load.
            _tool.SaveData(_tempPath);
            var loaded = _tool.LoadData(_tempPath);
            Assert.IsFalse(loaded.Contains("[Error]"), $"LoadData should not return error for an existing file.\n{loaded}");
            Assert.IsTrue(loaded.Contains("\"status\""), "Loaded content should contain the 'status' field written by SaveData.");
        }

        [Test]
        public void LoadData_ReportsFileNotFound()
        {
            var result = _tool.LoadData(Path.Combine(Path.GetTempPath(), $"unity-mcp-nonexistent-{System.Guid.NewGuid():N}.json"));
            Assert.IsTrue(result.Contains("[Error]"), $"Missing file should return error.\n{result}");
            Assert.IsTrue(result.Contains("not found"), "Error should mention 'not found'.");
        }

        [Test]
        public void LoadData_RejectsEmptyFilePath()
        {
            var result = _tool.LoadData("");
            Assert.IsTrue(result.Contains("[Error]"), $"Empty path should return error.\n{result}");
            Assert.IsTrue(result.Contains("required"), "Error should mention required.");
        }
    }
}
