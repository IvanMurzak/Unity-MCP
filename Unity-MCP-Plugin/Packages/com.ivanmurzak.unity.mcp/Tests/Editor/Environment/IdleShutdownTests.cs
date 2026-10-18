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
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class IdleShutdownTests
    {
        static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Quits_AfterTimeout_WithoutCalls()
        {
            var timer = new IdleShutdownTimer(TimeSpan.FromSeconds(300), T0);
            Assert.IsFalse(timer.ShouldQuit(T0.AddSeconds(299), 0, busy: false));
            Assert.IsTrue(timer.ShouldQuit(T0.AddSeconds(300), 0, busy: false));
        }

        [Test]
        public void ToolCall_RestartsTheTimer()
        {
            var timer = new IdleShutdownTimer(TimeSpan.FromSeconds(300), T0);
            Assert.IsFalse(timer.ShouldQuit(T0.AddSeconds(200), 1, busy: false));
            Assert.IsFalse(timer.ShouldQuit(T0.AddSeconds(450), 1, busy: false));
            Assert.IsTrue(timer.ShouldQuit(T0.AddSeconds(500), 1, busy: false));
        }

        [Test]
        public void CounterReset_CountsAsActivity()
        {
            var timer = new IdleShutdownTimer(TimeSpan.FromSeconds(300), T0);
            Assert.IsFalse(timer.ShouldQuit(T0.AddSeconds(100), 5, busy: false));
            Assert.IsFalse(timer.ShouldQuit(T0.AddSeconds(350), 0, busy: false));
            Assert.IsTrue(timer.ShouldQuit(T0.AddSeconds(650), 0, busy: false));
        }

        [Test]
        public void BusyEditor_NeverQuits()
        {
            var timer = new IdleShutdownTimer(TimeSpan.FromSeconds(300), T0);
            Assert.IsFalse(timer.ShouldQuit(T0.AddSeconds(1000), 0, busy: true));
            Assert.IsFalse(timer.ShouldQuit(T0.AddSeconds(1299), 0, busy: false));
        }

        [Test]
        public void ReadIdleTimeout_ArgsBeatEnv()
        {
            var args = new Dictionary<string, string> { [Startup.EnvIdleTimeout] = "60" };
            Assert.AreEqual(60, Startup.ReadIdleTimeoutSeconds(args, _ => "300"));
            Assert.AreEqual(300, Startup.ReadIdleTimeoutSeconds(new Dictionary<string, string>(), _ => "\"300\""));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("0")]
        [TestCase("-5")]
        [TestCase("abc")]
        public void ReadIdleTimeout_OffForMissingOrInvalid(string? value)
        {
            Assert.IsNull(Startup.ReadIdleTimeoutSeconds(new Dictionary<string, string>(), _ => value));
        }
    }
}
