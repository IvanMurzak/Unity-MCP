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
        public IEnumerator CaptureFrame_WhenProfilerStopped_ReturnsError()
        {
            // Arrange - ensure profiler is stopped
            _tool.Stop();
            yield return null;

            // Act
            var response = _tool.CaptureFrame();

            // Assert
            StructuredResponseErrorValidation(response);
        }

        [UnityTest]
        public IEnumerator CaptureFrame_WhenProfilerRunning_ReturnsValidData()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.CaptureFrame();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.FrameCaptureData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
        }

        [UnityTest]
        public IEnumerator CaptureFrame_ReturnsFrameTimeData()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.CaptureFrame();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.FrameCaptureData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.FrameTimeMs, 0, "FrameTimeMs should be >= 0.");
        }

        [UnityTest]
        public IEnumerator CaptureFrame_ReturnsTotalFrameCount()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.CaptureFrame();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.FrameCaptureData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.TotalFrameCount, 0, "TotalFrameCount should be >= 0.");
        }

        [UnityTest]
        public IEnumerator CaptureFrame_ReturnsRealtimeSinceStartup()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.CaptureFrame();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.FrameCaptureData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.RealtimeSinceStartup, 0, "RealtimeSinceStartup should be >= 0.");
        }

    }
}

