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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// A player / runtime connection declares <see cref="ClientRole.Runtime"/> (McpPlugin sends it as <c>client_role</c>), so
    /// the MCP server keeps it beside the Editor instead of letting it replace the Editor (GameDev-MCP-Server plugin roles).
    /// </summary>
    public class PluginRoleTests
    {
        private sealed class ProbeRuntime : UnityMcpPluginRuntime
        {
            public string Environment => BuildVersion().Environment;
            public static void Declare(IMcpPluginBuilder builder) => DeclareRuntimeRole(builder);
        }

        [Test]
        public void TheRuntimeHandshakeEnvironment_IsTheUnityVersion_WithoutASuffix()
        {
            var runtime = new ProbeRuntime();
            try
            {
                Assert.AreEqual(Application.unityVersion, runtime.Environment);
            }
            finally { runtime.Dispose(); }
        }

        [Test]
        public void TheRuntimeBuilder_DeclaresRuntime_EvenAfterTheDeveloperSetsEditor()
        {
            var config = new ConnectionConfig();
            var builder = new McpPluginBuilder(new McpPlugin.Common.Version()).SetConfig(config);
            builder.WithConfig(c => c.ClientRole = ClientRole.Editor);

            ProbeRuntime.Declare(builder);

            Assert.AreEqual(ClientRole.Runtime, config.ClientRole);
        }

        [Test]
        public void TheEditorConfig_StaysAnEditor()
        {
            Assert.AreEqual(ClientRole.Editor, new UnityMcpPlugin.UnityConnectionConfig().ClientRole);
        }
    }
}
