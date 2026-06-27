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
    /// <summary>
    /// Regression tests: the EditorPrefs key that stores the running server PID must be qualified by
    /// the server port. EditorPrefs are shared across every project of the same Editor version on a
    /// machine, so an unqualified key let two Editors (different projects, different ports) clobber a
    /// single global slot — one project's <c>CheckExistingProcess</c> would adopt the other's server
    /// PID and terminate it on stop, making the two Editors mutually exclusive despite distinct ports.
    ///
    /// <see cref="McpServerManager.ProcessIdKeyForPort"/> is the pure helper behind the key.
    /// </summary>
    public class McpServerManagerProcessIdKeyTests
    {
        [Test]
        public void ProcessIdKeyForPort_DifferentPorts_ProduceDifferentKeys()
        {
            Assert.AreNotEqual(
                McpServerManager.ProcessIdKeyForPort(26645),
                McpServerManager.ProcessIdKeyForPort(26646),
                "Two projects on different ports must not share the same PID key, " +
                "or they tear down each other's servers.");
        }

        [Test]
        public void ProcessIdKeyForPort_SamePort_IsStable()
        {
            Assert.AreEqual(
                McpServerManager.ProcessIdKeyForPort(26646),
                McpServerManager.ProcessIdKeyForPort(26646),
                "The key for a given port must be deterministic across calls so the stored PID " +
                "can be read back and deleted.");
        }

        [Test]
        public void ProcessIdKeyForPort_IncludesPort()
        {
            StringAssert.Contains("26646", McpServerManager.ProcessIdKeyForPort(26646));
        }
    }
}
