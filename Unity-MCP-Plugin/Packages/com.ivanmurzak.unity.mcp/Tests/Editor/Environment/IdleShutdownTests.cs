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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class IdleShutdownTests
    {
        static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Editor update loop: checks every 0.1 s from <paramref name="now"/> up to <paramref name="until"/>.</summary>
        static bool RunUntil(IdleShutdownTimer timer, ref DateTime now, double until, ulong calls = 0, bool busy = false)
        {
            var end = T0.AddSeconds(until);
            var quit = false;
            while (now < end && !quit)
            {
                now = now.AddSeconds(0.1);
                quit = timer.ShouldQuit(now, calls, busy);
            }
            return quit;
        }

        [Test]
        public void Quits_AfterTimeout_WithoutCalls()
        {
            var timer = new IdleShutdownTimer(TimeSpan.FromSeconds(300), T0);
            var now = T0;
            Assert.IsFalse(RunUntil(timer, ref now, 299));
            Assert.IsTrue(RunUntil(timer, ref now, 301));
        }

        [Test]
        public void ToolCall_RestartsTheTimer()
        {
            var timer = new IdleShutdownTimer(TimeSpan.FromSeconds(300), T0);
            var now = T0;
            Assert.IsFalse(RunUntil(timer, ref now, 200));
            Assert.IsFalse(RunUntil(timer, ref now, 450, calls: 1));
            Assert.IsTrue(RunUntil(timer, ref now, 501, calls: 1));
        }

        [Test]
        public void CounterReset_CountsAsActivity()
        {
            var timer = new IdleShutdownTimer(TimeSpan.FromSeconds(300), T0);
            var now = T0;
            Assert.IsFalse(RunUntil(timer, ref now, 100, calls: 5));
            Assert.IsFalse(RunUntil(timer, ref now, 350, calls: 0)); // reset at 100 s
            Assert.IsTrue(RunUntil(timer, ref now, 401, calls: 0));
        }

        [Test]
        public void BusyEditor_NeverQuits()
        {
            var timer = new IdleShutdownTimer(TimeSpan.FromSeconds(300), T0);
            var now = T0;
            Assert.IsFalse(RunUntil(timer, ref now, 1000, busy: true));
            Assert.IsFalse(RunUntil(timer, ref now, 1299));
            Assert.IsTrue(RunUntil(timer, ref now, 1301));
        }

        [Test]
        public void StalledUpdateLoop_CountsAsActivity()
        {
            // A long blocking tool call freezes the update loop; when it returns, the editor must not quit at once.
            var timer = new IdleShutdownTimer(TimeSpan.FromSeconds(300), T0);
            Assert.IsFalse(timer.ShouldQuit(T0.AddSeconds(600), 0, busy: false));
            var now = T0.AddSeconds(600);
            Assert.IsFalse(RunUntil(timer, ref now, 899));
            Assert.IsTrue(RunUntil(timer, ref now, 901));
        }

        [Test]
        public void ReadIdleTimeout_ArgsBeatEnv()
        {
            var args = new Dictionary<string, string> { [EnvironmentUtils.EnvIdleTimeout] = "60" };
            Assert.AreEqual(60, EnvironmentUtils.ReadIdleTimeoutSeconds(args, _ => "300"));
            Assert.AreEqual(300, EnvironmentUtils.ReadIdleTimeoutSeconds(new Dictionary<string, string>(), _ => "\"300\""));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("0")]
        [TestCase("-5")]
        [TestCase("abc")]
        public void ReadIdleTimeout_OffForMissingOrInvalid(string? value)
        {
            Assert.IsNull(EnvironmentUtils.ReadIdleTimeoutSeconds(new Dictionary<string, string>(), _ => value));
        }
    }
}
