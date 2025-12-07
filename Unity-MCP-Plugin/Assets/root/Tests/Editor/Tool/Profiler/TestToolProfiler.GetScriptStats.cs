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
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler
    {
        [UnityTest]
        public IEnumerator GetScriptStats_WhenProfilerStopped_ReturnsError()
        {
            // Arrange - ensure profiler is stopped
            _tool.Stop();
            yield return null;

            // Act
            var response = _tool.GetScriptStats();

            // Assert
            StructuredResponseErrorValidation(response);
        }

        [UnityTest]
        public IEnumerator GetScriptStats_WhenProfilerRunning_ReturnsValidData()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetScriptStats();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ScriptStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
        }

        [UnityTest]
        public IEnumerator GetScriptStats_ReturnsPositiveFrameTime()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetScriptStats();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ScriptStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.FrameTimeMs, 0, "FrameTimeMs should be >= 0.");
        }

        [UnityTest]
        public IEnumerator GetScriptStats_ReturnsTimeScaleInfo()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetScriptStats();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ScriptStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.TimeScale, 0, "TimeScale should be >= 0.");
        }

        [UnityTest]
        public IEnumerator GetScriptStats_ReturnsFrameCount()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetScriptStats();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ScriptStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.FrameCount, 0, "FrameCount should be >= 0.");
        }

        [UnityTest]
        public IEnumerator GetScriptStats_ReturnsMemoryUsage()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetScriptStats();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ScriptStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.MonoMemoryUsageMB, 0, "MonoMemoryUsageMB should be >= 0.");
            Assert.GreaterOrEqual(data.GCMemoryUsageMB, 0, "GCMemoryUsageMB should be >= 0.");
        }
    }
}

