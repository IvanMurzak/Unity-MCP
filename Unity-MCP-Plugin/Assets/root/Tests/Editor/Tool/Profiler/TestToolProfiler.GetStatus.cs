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
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler
    {
        [UnityTest]
        public IEnumerator GetStatus_WhenStopped_ReturnsDisabledStatus()
        {
            // Arrange - ensure profiler is stopped
            _tool.Stop();
            yield return null;

            // Act
            var response = _tool.GetStatus();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ProfilerStatusData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.IsFalse(data!.Enabled, "Profiler should be disabled.");
        }

        [UnityTest]
        public IEnumerator GetStatus_WhenRunning_ReturnsEnabledStatus()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetStatus();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ProfilerStatusData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.IsTrue(data!.Enabled, "Profiler should be enabled.");
        }

        [UnityTest]
        public IEnumerator GetStatus_ReturnsActiveModules()
        {
            // Arrange - start profiler
            _tool.Start();
            yield return null;

            // Act
            var response = _tool.GetStatus();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ProfilerStatusData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.IsNotNull(data!.ActiveModules, "ActiveModules should not be null.");
            Assert.Greater(data.ActiveModules!.Count, 0, "Should have at least one active module.");
        }

        [UnityTest]
        public IEnumerator GetStatus_ReturnsMemoryInfo()
        {
            // Act
            var response = _tool.GetStatus();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ProfilerStatusData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.MaxUsedMemoryMB, 0, "MaxUsedMemoryMB should be >= 0.");
        }

        [Test]
        public void GetStatus_ReturnsSupported()
        {
            // Act
            var response = _tool.GetStatus();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ProfilerStatusData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            // Profiler should be supported in the Unity Editor
            Assert.IsTrue(data!.Supported, "Profiler should be supported in Editor.");
        }
    }
}

