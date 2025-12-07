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
        public IEnumerator GetRenderingStats_WhenProfilerStopped_ReturnsError()
        {
            // Arrange - ensure profiler is stopped
            _tool.Stop();
            yield return null;

            // Act
            var response = _tool.GetRenderingStats();

            // Assert
            StructuredResponseErrorValidation(response);
        }

        [UnityTest]
        public IEnumerator GetRenderingStats_WhenProfilerRunning_ReturnsValidData()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetRenderingStats();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.RenderingStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
        }

        [UnityTest]
        public IEnumerator GetRenderingStats_ReturnsPositiveFrameTime()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetRenderingStats();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.RenderingStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.FrameTimeMs, 0, "FrameTimeMs should be >= 0.");
        }

        [UnityTest]
        public IEnumerator GetRenderingStats_ReturnsGraphicsInfo()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetRenderingStats();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.RenderingStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.IsNotNull(data!.GraphicsDeviceType, "GraphicsDeviceType should not be null.");
            Assert.IsNotEmpty(data.GraphicsDeviceType, "GraphicsDeviceType should not be empty.");
            Assert.IsNotNull(data.RenderingThreadingMode, "RenderingThreadingMode should not be null.");
        }

        [UnityTest]
        public IEnumerator GetRenderingStats_ReturnsVSyncInfo()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetRenderingStats();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.RenderingStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.VSyncCount, 0, "VSyncCount should be >= 0.");
        }
    }
}

