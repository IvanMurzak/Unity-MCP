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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Skills;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Unity-specific skill file generator that emits <c>unity-mcp-cli run-tool</c>
    /// commands instead of <c>curl</c> HTTP API calls in the "How to Call" section.
    /// Authorization is handled automatically by the CLI (reads config file),
    /// so the authorization example block is omitted.
    /// </summary>
    public class UnitySkillFileGenerator : SkillFileGenerator
    {
        private readonly ILogger? _log;

        public UnitySkillFileGenerator() : base()
        {
        }
        public UnitySkillFileGenerator(ILogger? logger = null) : base(logger)
        {
            _log = logger;
        }

        /// <summary>
        /// Authorization is handled automatically by the CLI from the project config file.
        /// No need to show a separate authorization example in SKILL.md.
        /// </summary>
        public override bool IncludeAuthorizationExample => false;

        protected override string BuildMarkdown(IRunTool tool, string skillName, string host)
        {
            var result = base.BuildMarkdown(tool, skillName, host);
            var trimmedHost = host.TrimEnd('/');
            var inputExample = BuildInputExample(tool.InputSchema);
            var nl = Environment.NewLine;

            // Replace section heading and intro text
            result = result.Replace(
                "### HTTP API (Direct Tool Execution)",
                "### CLI (Direct Tool Execution)");
            result = result.Replace(
                "Execute this tool directly via the MCP Plugin HTTP API:",
                "Execute this tool directly via the Unity-MCP CLI:");

            // Replace basic curl block with CLI command
            var oldCurl = string.Concat(
                $"curl -X POST {trimmedHost}/api/tools/{tool.Name} \\", nl,
                "  -H \"Content-Type: application/json\" \\", nl,
                $"  -d '{inputExample}'");
            var newCli = $"unity-mcp-cli run-tool {tool.Name} --url {trimmedHost} --input '{inputExample}'";
            result = result.Replace(oldCurl, newCli);

            if (result.Contains($"curl -X POST {trimmedHost}/api/tools/{tool.Name}"))
                _log?.LogWarning("UnitySkillFileGenerator: failed to replace curl block for tool '{ToolName}' — base format may have changed.", tool.Name);

            return result;
        }
    }
}