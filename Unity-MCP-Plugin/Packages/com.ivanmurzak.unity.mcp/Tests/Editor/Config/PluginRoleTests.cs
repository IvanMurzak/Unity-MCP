/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The MCP server tells a game from the Editor by the handshake environment: ending in "(runtime)" is a runtime, which
    /// connects beside the Editor instead of replacing it (GameDev-MCP-Server plugin roles).
    /// </summary>
    public class PluginRoleTests
    {
        private sealed class ProbeRuntime : UnityMcpPluginRuntime
        {
            public string Environment => BuildVersion().Environment;
        }

        [Test]
        public void ARuntimeHandshakeEndsInRuntime()
        {
            var runtime = new ProbeRuntime();
            try
            {
                Assert.AreEqual(Application.unityVersion + " (runtime)", runtime.Environment);
                StringAssert.EndsWith("(runtime)", runtime.Environment);
            }
            finally { runtime.Dispose(); }
        }
    }
}
