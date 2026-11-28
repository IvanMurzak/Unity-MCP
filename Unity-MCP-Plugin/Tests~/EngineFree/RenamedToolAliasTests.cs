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
using System.IO;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    /// <summary>A renamed tool and its old name, registered the way csharp-exec and its script-execute alias are (DEV-314).</summary>
    public static class RenamedProbeTools
    {
        public const string NewName = "renamed-probe-new";
        public const string OldName = "renamed-probe-old";

        [AiTool(NewName)]
        public static string Run(string text) => "ran:" + text;

        [Obsolete("Old name, as Tool_Script.ScriptExecuteToolId.")]
        [AiTool(OldName, Visible = false)]
        public static string RunOldName(string text) => Run(text);
    }

    /// <summary>
    /// The hidden alias of a renamed tool (DEV-314): McpPlugin keeps a Visible=false tool out of what the server lists
    /// (<c>ResponseListTool.Visible</c>) but still runs it by name, and UnitySkillFileGenerator writes no SKILL.md for it and
    /// deletes the one an older plugin wrote under the old name.
    /// </summary>
    public class RenamedToolAliasTests
    {
        [Fact]
        public async System.Threading.Tasks.Task Alias_IsNotListedAsVisible_StillRuns_AndHasNoSkill()
        {
            var root = Path.Combine(Path.GetTempPath(), "UnityRenamedTool_" + Guid.NewGuid().ToString("N"));
            try
            {
                // The SKILL.md an older plugin generated under the old name.
                var stale = Path.Combine(root, RenamedProbeTools.OldName);
                Directory.CreateDirectory(stale);
                File.WriteAllText(Path.Combine(stale, "SKILL.md"), "stale");

                var builder = new McpPluginBuilder(new com.IvanMurzak.McpPlugin.Common.Version());
                builder.WithConfig(config =>
                {
                    config.SkillsPath = root;
                    config.KeepConnected = false;
                    UnitySkillFileGenerator.ApplySkillMetadata(config);
                });
                builder.WithSkillFileGenerator(new UnitySkillFileGenerator { HiddenAliases = new[] { RenamedProbeTools.OldName } });
                builder.WithTools(typeof(RenamedProbeTools));
                using var plugin = (com.IvanMurzak.McpPlugin.McpPlugin)builder.Build(new Reflector());
                var tools = plugin.McpManager.ToolManager!;

                var listed = (await tools.RunListTool(new RequestListTool())).Value!;
                Assert.True(listed.Single(t => t.Name == RenamedProbeTools.NewName).Visible);
                Assert.DoesNotContain(listed, t => t.Name == RenamedProbeTools.OldName && t.Visible);
                Assert.False(tools.IsToolVisible(RenamedProbeTools.OldName));

                foreach (var name in new[] { RenamedProbeTools.NewName, RenamedProbeTools.OldName })
                {
                    var response = await tools.RunCallTool(new RequestCallTool("r-" + name, name,
                        new Dictionary<string, JsonElement> { ["text"] = JsonDocument.Parse("\"x\"").RootElement }));
                    Assert.True(response.Status == ResponseStatus.Success, response.Message);
                    Assert.Contains("ran:x", response.Value!.Content!.First().Text);
                }

                Assert.True(File.Exists(Path.Combine(root, RenamedProbeTools.NewName, "SKILL.md")));
                Assert.False(Directory.Exists(stale));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
