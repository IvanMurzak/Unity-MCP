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
    public partial class AssetsShaderTests : BaseTest
    {
        // ── background thread tests ───────────────────────────────────────

        [UnityTest]
        public IEnumerator ShaderListAll_FromBackgroundThread_ReturnsShaders()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
            {
                var tool = new Tool_Assets_Shader();
                var result = tool.ListAll();
                Assert.IsNotNull(result, "ListAll() should return non-null from background thread");
                Assert.Greater(result.Count, 0, "Should return at least one shader from background thread");
            });
        }

        [UnityTest]
        public IEnumerator ShaderListAll_ViaRunTool_FromBackgroundThread_Succeeds()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Assets_Shader.AssetsShaderListAllToolId, "{}"));
        }
    }
}
