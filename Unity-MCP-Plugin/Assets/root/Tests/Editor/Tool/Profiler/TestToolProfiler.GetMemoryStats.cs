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
        public IEnumerator GetMemoryStats_ReturnsValidMemoryData()
        {
            // Act
            var response = _tool.GetMemoryStats();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.MemoryStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
        }

        [UnityTest]
        public IEnumerator GetMemoryStats_ReturnsPositiveReservedMemory()
        {
            // Act
            var response = _tool.GetMemoryStats();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.MemoryStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.TotalReservedMemoryMB, 0, "TotalReservedMemoryMB should be >= 0.");
        }

        [UnityTest]
        public IEnumerator GetMemoryStats_ReturnsPositiveAllocatedMemory()
        {
            // Act
            var response = _tool.GetMemoryStats();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.MemoryStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.TotalAllocatedMemoryMB, 0, "TotalAllocatedMemoryMB should be >= 0.");
        }

        [UnityTest]
        public IEnumerator GetMemoryStats_ReturnsMonoHeapData()
        {
            // Act
            var response = _tool.GetMemoryStats();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.MemoryStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.MonoHeapSizeMB, 0, "MonoHeapSizeMB should be >= 0.");
            Assert.GreaterOrEqual(data.MonoUsedSizeMB, 0, "MonoUsedSizeMB should be >= 0.");
        }

        [UnityTest]
        public IEnumerator GetMemoryStats_AllocatedLessThanOrEqualToReserved()
        {
            // Act
            var response = _tool.GetMemoryStats();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.MemoryStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.LessOrEqual(data!.TotalAllocatedMemoryMB, data.TotalReservedMemoryMB,
                "TotalAllocatedMemoryMB should be <= TotalReservedMemoryMB.");
        }

        [UnityTest]
        public IEnumerator GetMemoryStats_MonoUsedLessThanOrEqualToHeap()
        {
            // Act
            var response = _tool.GetMemoryStats();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.MemoryStatsData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.LessOrEqual(data!.MonoUsedSizeMB, data.MonoHeapSizeMB,
                "MonoUsedSizeMB should be <= MonoHeapSizeMB.");
        }
    }
}

