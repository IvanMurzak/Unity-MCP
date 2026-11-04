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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Xunit;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    public class McpDisableTests
    {
        [Fact]
        public void AllShippedAssembliesHonorCompilationExclusion()
        {
            var root = Path.GetDirectoryName(RepoFile.Require("extensions/manifest.json"))!;
            var repo = Directory.GetParent(root)!.FullName;
            var core = Path.Combine(repo, "Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp");
            var paths = Directory.GetFiles(core, "*.asmdef", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(root, "*.asmdef", SearchOption.AllDirectories)
                    .Where(p => p.Replace('\\', '/').Contains("/Packages/"))).ToArray();
            Assert.True(paths.Length >= 52, "Core and all ten modules must be present");
            foreach (var path in paths)
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                Assert.Contains(doc.RootElement.GetProperty("defineConstraints").EnumerateArray(),
                    v => v.GetString() == "!UNITY_MCP_EXCLUDE_FROM_COMPILATION");
            }
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData("unexpected", false)]
        [InlineData("1", true)]
        [InlineData("true", true)]
        [InlineData(" TRUE ", true)]
        [InlineData(" 1 ", true)]
        public void OnlyExplicitOptOutDisablesMcp(string? value, bool expected)
            => Assert.Equal(expected, McpDisable.IsEnabledValue(value));
    }
}
