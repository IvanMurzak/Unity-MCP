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
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler
    {
        [UnityTest]
        public IEnumerator Start_WhenNotRunning_StartsProfiler()
        {
            // Arrange - ensure profiler is stopped
            _tool.Stop();
            yield return null;

            // Act
            var result = _tool.Start();

            // Assert
            ResultValidationExpected(result, "Profiler started successfully");
        }

        [UnityTest]
        public IEnumerator Start_WhenAlreadyRunning_ReturnsAlreadyRunning()
        {
            // Arrange - start profiler first
            _tool.Start();
            yield return null;

            // Act - try to start again
            var result = _tool.Start();

            // Assert
            ResultValidationExpected(result, "already running");
        }

        [Test]
        public void Start_ReturnsJsonWithExpectedFields()
        {
            // Arrange - ensure profiler is stopped
            _tool.Stop();

            // Act
            var result = _tool.Start();

            // Assert
            ResultValidation(result);
            Assert.IsTrue(result!.Contains("enabled"), "Should contain 'enabled' field");
            Assert.IsTrue(result.Contains("targetFrameRate"), "Should contain 'targetFrameRate' field");
            Assert.IsTrue(result.Contains("note"), "Should contain 'note' field");
        }
    }
}

