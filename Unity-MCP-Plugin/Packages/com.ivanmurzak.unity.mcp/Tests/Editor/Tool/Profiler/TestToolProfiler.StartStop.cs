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
using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler
    {
        [Test]
        public void Start_EnablesUnityProfiler()
        {
            var result = _tool.Start();
            Assert.IsTrue(result, "Start() should return the profiler's enabled state, which should be true.");
            Assert.IsTrue(UnityProfiler.enabled, "UnityEngine.Profiling.Profiler.enabled should be true after Start().");
        }

        [Test]
        public void Stop_DisablesUnityProfiler()
        {
            _tool.Start();
            var result = _tool.Stop();
            Assert.IsFalse(result, "Stop() should return the profiler's enabled state, which should be false.");
            Assert.IsFalse(UnityProfiler.enabled, "UnityEngine.Profiling.Profiler.enabled should be false after Stop().");
        }

        [Test]
        public void Start_IsIdempotent()
        {
            _tool.Start();
            var second = _tool.Start();
            Assert.IsTrue(second, "Calling Start() twice should still leave the profiler enabled.");
            Assert.IsTrue(UnityProfiler.enabled, "Profiler should remain enabled after a second Start().");
        }

        [Test]
        public void Stop_IsIdempotent()
        {
            _tool.Stop();
            var second = _tool.Stop();
            Assert.IsFalse(second, "Calling Stop() twice should still leave the profiler disabled.");
            Assert.IsFalse(UnityProfiler.enabled, "Profiler should remain disabled after a second Stop().");
        }
    }
}
