/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using System;
using System.IO;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    public class SkillGuidesTests
    {
        [Fact]
        public void RegisteredGuidesAreStandalone_AndGeneratedWithGlobalMetadata()
        {
            var root = Path.Combine(Path.GetTempPath(), "UnitySkillGuides_" + Guid.NewGuid().ToString("N"));
            try
            {
                var builder = new McpPluginBuilder(new com.IvanMurzak.McpPlugin.Common.Version());
                builder.WithConfig(config =>
                {
                    config.SkillsPath = root;
                    config.KeepConnected = false;
                    config.SkillMetadata["generated-by"] = "unity-mcp";
                    config.SkillMetadata["ai-game-dev"] = "exclude";
                });
                builder.WithSkillFileGenerator(new UnitySkillFileGenerator());
                builder.WithTools(typeof(Tool_Ping));
                builder.WithSkills(typeof(Skill_InitialSetup), typeof(Skill_Cli), typeof(Skill_App));
                using var plugin = (com.IvanMurzak.McpPlugin.McpPlugin)builder.Build(new Reflector());

                var files = Directory.GetFiles(root, "SKILL.md", SearchOption.AllDirectories);
                Assert.Equal(4, files.Length);
                foreach (var file in files)
                    Assert.Contains("metadata:\n  generated-by: unity-mcp\n  ai-game-dev: exclude\n",
                        File.ReadAllText(file).Replace("\r\n", "\n"));

                var tool = File.ReadAllText(Path.Combine(root, "ping", "SKILL.md"));
                Assert.Contains("unity-cli", tool);
                Assert.Contains("ai-game-dev-app", tool);
                Assert.DoesNotContain("npm install", tool);
                Assert.Contains("npm install -g unity-mcp-cli", File.ReadAllText(Path.Combine(root, "unity-cli", "SKILL.md")));
                Assert.Contains("Blender", File.ReadAllText(Path.Combine(root, "ai-game-dev-app", "SKILL.md")));
                Assert.Empty(plugin.McpManager.ToolManager!.GetAllTools()); // Only the existing SYSTEM ping was registered.
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
