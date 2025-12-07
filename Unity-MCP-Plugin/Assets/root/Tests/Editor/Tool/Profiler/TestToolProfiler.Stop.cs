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
        public IEnumerator Stop_WhenRunning_StopsProfiler()
        {
            // Arrange - start profiler first
            _tool.Start();
            yield return null;

            // Act
            var result = _tool.Stop();

            // Assert
            ResultValidationExpected(result, "Profiler stopped successfully");
        }

        [UnityTest]
        public IEnumerator Stop_WhenAlreadyStopped_ReturnsAlreadyStopped()
        {
            // Arrange - ensure profiler is stopped
            _tool.Stop();
            yield return null;

            // Act - try to stop again
            var result = _tool.Stop();

            // Assert
            ResultValidationExpected(result, "already stopped");
        }

        [UnityTest]
        public IEnumerator StartStop_Cycle_WorksCorrectly()
        {
            // Arrange - ensure profiler is stopped
            _tool.Stop();
            yield return null;

            // Act & Assert - Start
            var startResult = _tool.Start();
            ResultValidationExpected(startResult, "Profiler started successfully");
            yield return null;

            // Act & Assert - Stop
            var stopResult = _tool.Stop();
            ResultValidationExpected(stopResult, "Profiler stopped successfully");
            yield return null;

            // Act & Assert - Start again
            var startAgainResult = _tool.Start();
            ResultValidationExpected(startAgainResult, "Profiler started successfully");
        }
    }
}

