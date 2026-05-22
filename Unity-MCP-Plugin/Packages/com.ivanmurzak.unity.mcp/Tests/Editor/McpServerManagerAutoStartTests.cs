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
    /// Regression tests for issue #695: the local MCP server must NOT be auto-started on
    /// Editor launch (or after a binary update) when the connection mode is Cloud.
    ///
    /// <see cref="McpServerManager.IsAutoStartAllowedForMode"/> is the pure decision used by
    /// every auto-start path. User-driven actions (clicking the Start button in the editor
    /// window) are intentionally not gated by this method.
    /// </summary>
    public class McpServerManagerAutoStartTests
    {
        [Test]
        public void IsAutoStartAllowedForMode_Cloud_ReturnsFalse()
        {
            Assert.IsFalse(
                McpServerManager.IsAutoStartAllowedForMode(ConnectionMode.Cloud),
                "Cloud mode must never auto-start the local MCP server (issue #695).");
        }

        [Test]
        public void IsAutoStartAllowedForMode_Custom_ReturnsTrue()
        {
            Assert.IsTrue(
                McpServerManager.IsAutoStartAllowedForMode(ConnectionMode.Custom),
                "Custom mode targets the local MCP server, so auto-start must be allowed " +
                "(subject to the other gates inside StartServerIfNeeded).");
        }
    }
}
