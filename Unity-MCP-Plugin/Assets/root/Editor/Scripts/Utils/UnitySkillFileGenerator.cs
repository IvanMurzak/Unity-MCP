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
using System.Text;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin.Skills;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Unity-specific skill file generator that emits <c>unity-mcp-cli run-tool</c>
    /// commands instead of <c>curl</c> HTTP API calls in the "How to Call" section.
    /// </summary>
    public class UnitySkillFileGenerator : SkillFileGenerator
    {
        public UnitySkillFileGenerator() : base()
        {
        }
        public UnitySkillFileGenerator(ILogger? logger = null) : base(logger)
        {
        }

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

            // Replace authorization curl block (must be replaced before the basic one
            // because the basic pattern is a substring of the auth pattern)
            var oldAuthCurl = string.Concat(
                $"curl -X POST {trimmedHost}/api/tools/{tool.Name} \\", nl,
                "  -H \"Content-Type: application/json\" \\", nl,
                "  -H \"Authorization: Bearer YOUR_TOKEN\" \\", nl,
                $"  -d '{inputExample}'");
            var newAuthCli = $"unity-mcp-cli run-tool {tool.Name} --url {trimmedHost} --token YOUR_TOKEN --input '{inputExample}'";
            result = result.Replace(oldAuthCurl, newAuthCli);

            // Replace basic curl block
            var oldCurl = string.Concat(
                $"curl -X POST {trimmedHost}/api/tools/{tool.Name} \\", nl,
                "  -H \"Content-Type: application/json\" \\", nl,
                $"  -d '{inputExample}'");
            var newCli = $"unity-mcp-cli run-tool {tool.Name} --url {trimmedHost} --input '{inputExample}'";
            result = result.Replace(oldCurl, newCli);

            return result;
        }

        protected override void BuildInputAuthorizationNotes(StringBuilder sb)
        {
            sb.AppendLine($"> The token is stored in the file: `{UnityMcpPluginEditor.AssetsFilePath}`");
            sb.AppendLine($"> Using the format: `\"token\": \"YOUR_TOKEN\"`");
            sb.AppendLine();
        }
    }
}