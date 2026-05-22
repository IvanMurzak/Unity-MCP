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
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler
    {
        [Test]
        public void GetStatus_ReturnsPopulatedSnapshot()
        {
            var status = _tool.GetStatus();
            Assert.IsNotNull(status, "GetStatus() should never return null.");
            Assert.IsNotNull(status.ActiveModules, "ActiveModules should always be initialised (even if empty).");
            Assert.IsTrue(status.MaxUsedMemoryMB >= 0f, "MaxUsedMemoryMB should be a non-negative MB value.");
        }

        [Test]
        public void GetMemoryStats_ReturnsNonNegativeFields()
        {
            var memory = _tool.GetMemoryStats();
            Assert.IsNotNull(memory, "GetMemoryStats() should never return null.");
            Assert.IsTrue(memory.TotalReservedMemoryMB >= 0f, "TotalReservedMemoryMB should be non-negative.");
            Assert.IsTrue(memory.TotalAllocatedMemoryMB >= 0f, "TotalAllocatedMemoryMB should be non-negative.");
            Assert.IsTrue(memory.MonoHeapSizeMB >= 0f, "MonoHeapSizeMB should be non-negative.");
            Assert.IsTrue(memory.MonoUsedSizeMB >= 0f, "MonoUsedSizeMB should be non-negative.");
            Assert.IsTrue(memory.UsedHeapSizeMB >= 0f, "UsedHeapSizeMB should be non-negative.");
            // Allocated should never exceed reserved (Unity invariant).
            Assert.IsTrue(memory.TotalAllocatedMemoryMB <= memory.TotalReservedMemoryMB + 0.001f,
                "Allocated memory should not exceed reserved memory.");
        }

        [Test]
        public void GetRenderingStats_ReturnsRequiredStringFields()
        {
            var rendering = _tool.GetRenderingStats();
            Assert.IsNotNull(rendering, "GetRenderingStats() should never return null.");
            Assert.IsFalse(string.IsNullOrEmpty(rendering.RenderingThreadingMode), "RenderingThreadingMode should be populated.");
            Assert.IsFalse(string.IsNullOrEmpty(rendering.GraphicsDeviceType), "GraphicsDeviceType should be populated.");
            Assert.IsTrue(rendering.Fps >= 0f, "Fps should be non-negative.");
        }

        [Test]
        public void GetScriptStats_ReturnsNonNegativeTiming()
        {
            var script = _tool.GetScriptStats();
            Assert.IsNotNull(script, "GetScriptStats() should never return null.");
            Assert.IsTrue(script.TotalFrameCount >= 0, "TotalFrameCount should be non-negative.");
            Assert.IsTrue(script.RealtimeSinceStartup >= 0f, "RealtimeSinceStartup should be non-negative.");
            Assert.IsTrue(script.GCMemoryUsageMB > 0f, "GCMemoryUsageMB should be > 0 in a running .NET process.");
        }

        [Test]
        public void CaptureFrame_ReturnsCurrentFrameSnapshot()
        {
            var frame = _tool.CaptureFrame();
            Assert.IsNotNull(frame, "CaptureFrame() should never return null.");
            Assert.IsTrue(frame.TotalFrameCount >= 0, "TotalFrameCount should be non-negative.");
            Assert.IsTrue(frame.RealtimeSinceStartup >= 0f, "RealtimeSinceStartup should be non-negative.");
        }
    }
}
