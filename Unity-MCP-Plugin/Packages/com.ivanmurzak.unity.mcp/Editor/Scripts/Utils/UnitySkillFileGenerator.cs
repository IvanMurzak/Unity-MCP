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
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Skills;
using com.IvanMurzak.Unity.MCP.Editor.API;
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
        readonly ILogger? logger;

        public UnitySkillFileGenerator() : base()
        {
        }
        public UnitySkillFileGenerator(ILogger? logger = null) : base(logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Hidden aliases of renamed tools (<c>UnityMcpPlugin.RenamedTools</c>): no SKILL.md, so an agent never sees the tool
        /// twice, and the one an older plugin wrote under the old name is deleted.
        /// </summary>
        public IEnumerable<string> HiddenAliases { get; set; } = Array.Empty<string>();

        /// <inheritdoc/>
        protected override bool GenerateFor(IRunTool tool, string skillsDir, string host, string skillName)
        {
            if (!HiddenAliases.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))
                return base.GenerateFor(tool, skillsDir, host, skillName);
            var skillDir = Path.Combine(skillsDir, SanitizeSkillName(skillName));
            try
            {
                if (Directory.Exists(skillDir))
                    Directory.Delete(skillDir, recursive: true);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to delete the skill of the renamed tool '{tool}' at '{path}'.", tool.Name, skillDir);
                return false;
            }
        }

        /// <summary>
        /// Authorization is handled automatically by the CLI from the project config file.
        /// No need to show a separate authorization example in SKILL.md.
        /// </summary>
        public override bool IncludeAuthorizationExample => false;

        /// <summary>
        /// Description is already in the YAML front-matter — skip the duplicate paragraph
        /// after the title to save tokens.
        /// </summary>
        public override bool IncludeDescriptionBody => false;

        /// <summary>
        /// Descriptions are already shown in the parameter table — strip them from the
        /// Input JSON Schema to save tokens.
        /// </summary>
        public override bool IncludeInputSchemaPropertyDescriptions => false;

        /// <inheritdoc/>
        protected override void BuildHowToCallHeading(StringBuilder sb)
        {
            // sb.AppendLine("## Use command line");
            // sb.AppendLine();
        }

        /// <inheritdoc/>
        protected virtual bool UsePowerShell => Skill_Cli.IsWindows;

        protected override void BuildToolCommand(StringBuilder sb, IRunTool tool, string host, string inputExample)
        {
            var command = tool.ToolType == McpToolType.System ? "run-system-tool" : "run-tool";
            sb.AppendLine(Skill_Cli.ToolCommand(command, tool.Name, inputExample, UsePowerShell));
            AppendInputFileHint(sb, tool, host, inputExample);
            AppendSkillReferences(sb);
        }

        protected override string BuildSkillContentMarkdown(ISkillContent skill, string skillName)
        {
            var markdown = base.BuildSkillContentMarkdown(skill, skillName);
            if (skill.Name == Skill_Cli.SkillId || skill.Name == Skill_App.SkillId)
                return markdown;
            var sb = new StringBuilder(markdown).AppendLine().AppendLine();
            AppendSkillReferences(sb);
            return sb.ToString();
        }

        private static void AppendSkillReferences(StringBuilder sb)
        {
            sb.AppendLine($"> CLI installation and usage: see the `{Skill_Cli.SkillId}` skill.");
            sb.AppendLine($"> If this task needs unavailable asset-generation or Blender tools, consult `{Skill_App.SkillId}` for an optional integrated workflow.");
            sb.AppendLine();
        }

        /// <inheritdoc/>
        protected override void AppendInputFileHint(StringBuilder sb, IRunTool tool, string host, string inputExample)
        {
            if (UsePowerShell || inputExample == "{}") return;
            var command = tool.ToolType == McpToolType.System ? "run-system-tool" : "run-tool";
            sb.AppendLine("For complex input, create a JSON file instead:");
            sb.AppendLine("```bash");
            sb.AppendLine(Skill_Cli.JsonFile(inputExample, false));
            sb.AppendLine($"unity-mcp-cli {command} {tool.Name} --input-file args.json");
            sb.AppendLine("```");
            sb.AppendLine();
        }

    }
}
