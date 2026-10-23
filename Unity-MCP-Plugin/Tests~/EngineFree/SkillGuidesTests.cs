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
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
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
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GuideContainsOnlyTheTargetShellAndExecutableExamples(bool windows)
        {
            var guide = Skill_Cli.BuildMarkdown(windows);
            Assert.Contains(windows ? "```powershell" : "```bash", guide);
            Assert.DoesNotContain(windows ? "```bash" : "```powershell", guide);
            Assert.Contains(windows ? "npm.cmd install -g unity-mcp-cli" : "npm install -g unity-mcp-cli", guide);
            Assert.Contains("run-tool rql-schema --input '{}'", guide);
            Assert.DoesNotContain("<tool-name>", guide);
            Assert.DoesNotMatch("@[A-Z_]+@", guide);
            Assert.DoesNotContain("<<'EOF'", guide);
        }

        [Fact]
        public async Task GeneratedCommandRoundTripsQuotesUnicodeAndNewlinesThroughTheHostShell()
        {
            var windows = Skill_Cli.IsWindows;
            const string json = "{\"text\":\"Bob's hero \\\"quoted\\\" C:\\\\Games\\\\Hero $HOME `tick`\\n\\u041f\\u0440\\u0438\\u0432\\u0435\\u0442\"}";
            var block = Skill_Cli.ToolCommand("run-tool", "example-tool", json, windows);
            var command = block.Substring(block.IndexOf('\n') + 1);
            command = command.Substring(0, command.LastIndexOf("```", StringComparison.Ordinal));
            var script = windows
                ? "$env:PATH = (Get-Location).Path + ';' + $env:PATH\n" + command
                : Skill_Cli.JsonFile(json, false) + "\nunity-mcp-cli() { printf '%s' \"$4\"; }\n" + command;
            var root = Path.Combine(Path.GetTempPath(), "Skill shell path " + Guid.NewGuid().ToString("N"));
            var dir = Path.Combine(root, "project with spaces");
            Directory.CreateDirectory(dir);
            if (windows) script = "Set-Location -LiteralPath '" + dir.Replace("'", "''") + "'\n" + script;
            try
            {
                if (windows)
                    File.WriteAllText(Path.Combine(dir, "unity-mcp-cli.cmd"),
                        "@echo off\r\nif not \"%~1\"==\"run-tool\" exit /b 2\r\nif not \"%~3\"==\"--input-file\" exit /b 3\r\ntype \"%~4\"\r\n");
                var start = new ProcessStartInfo(windows ? "powershell.exe" : "/bin/bash")
                {
                    WorkingDirectory = windows ? root : dir, UseShellExecute = false,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                };
                if (windows)
                {
                    start.ArgumentList.Add("-NoProfile");
                    start.ArgumentList.Add("-NonInteractive");
                    start.ArgumentList.Add("-EncodedCommand");
                    start.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
                }
                else { start.ArgumentList.Add("-c"); start.ArgumentList.Add(script); }
                using var process = Process.Start(start)!;
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(20000)) { process.Kill(entireProcessTree: true); throw new TimeoutException("Skill shell command hung"); }
                Assert.Equal(0, process.ExitCode);
                Assert.Equal(json, (await output).Trim());
                var bytes = File.ReadAllBytes(Path.Combine(dir, "args.json"));
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf);
                Assert.Equal(json, File.ReadAllText(Path.Combine(dir, "args.json")).TrimEnd('\n', '\r'));
                Assert.True(string.IsNullOrWhiteSpace(await error));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

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
                Assert.Contains(Skill_Cli.IsWindows ? "npm.cmd install -g unity-mcp-cli" : "npm install -g unity-mcp-cli", File.ReadAllText(Path.Combine(root, "unity-cli", "SKILL.md")));
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
