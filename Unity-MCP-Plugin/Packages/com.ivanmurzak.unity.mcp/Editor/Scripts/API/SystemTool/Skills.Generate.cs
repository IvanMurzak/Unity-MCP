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
using System.ComponentModel;
using System.IO;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Skills
    {
        public const string SkillsGenerateToolId = "unity-skill-generate";
        [McpPluginTool
        (
            SkillsGenerateToolId,
            Title = "Skill (Tool) / Generate All",
            DestructiveHint = false,
            Enabled = false,
            ToolType = McpToolType.System
        )]
        [McpPluginSkillDescription("Regenerate every `SKILL.md` from the project's currently-registered MCP tools into " +
            "the configured skills folder (or a project-relative override path). " +
            "Writes the YAML `description:` from `[McpPluginSkillDescription]` and the body from `[McpPluginSkillBody]`.")]
        [McpPluginSkillBody("Generate all skills from the existed Tools in the Unity Project.\n\n" +
            "## Inputs\n\n" +
            "- `path` (optional) — project-relative skills folder (e.g. `.claude/skills`). Absolute paths and `..` " +
            "traversal segments are rejected. When null/empty, the editor's configured `SkillsRootFolderAbsolutePath` " +
            "is used.\n\n" +
            "## Behavior\n\n" +
            "Creates the destination folder if missing, then invokes `McpPluginInstance.GenerateSkillFiles(...)` to " +
            "emit a `SKILL.md` per registered MCP tool. The plugin's `SkillsPath` is temporarily swapped to the target " +
            "folder and restored in `finally` so the on-disk configuration is unchanged after the call returns.")]
        [Description("Generate all skills from the existed Tools in the Unity Project.")]
        public void GenerateAll
        (
            [Description("Path to the skills folder. If null or empty, the default path will be used.")]
            string? path = null
        )
        {
            var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Skills>();

            var userProvidedPath = !string.IsNullOrEmpty(path);
            if (!userProvidedPath)
                path = UnityMcpPluginEditor.SkillsRootFolderAbsolutePath;

            if (userProvidedPath)
            {
                if (Path.IsPathRooted(path))
                    throw new ArgumentException(
                        "Path must be a relative path inside the Unity project (e.g. '.claude/skills'). Absolute paths are not allowed.",
                        nameof(path));

                var normalizedPath = path!.Replace('\\', '/');
                if (normalizedPath.Contains("../") || normalizedPath.EndsWith(".."))
                    throw new ArgumentException(
                        "Path must not contain '..' traversal segments. Use a path relative to the project root.",
                        nameof(path));

                path = Path.Combine(UnityMcpPluginEditor.ProjectRootPath, path);
            }

            if (!Directory.Exists(path))
            {
                logger.LogInformation("Directory does not exist at '{Path}'. Creating it.", path);
                Directory.CreateDirectory(path);
            }

            logger.LogInformation("Generating all skills in folder: {Path}", path);

            var mcpPlugin = UnityMcpPluginEditor.Instance.McpPluginInstance
                ?? throw new InvalidOperationException(
                    "McpPluginInstance is null. The Unity-MCP plugin may not be fully initialized. " +
                    "Ensure the plugin has completed startup before calling unity-skill-generate.");

            var originalSkillsPath = UnityMcpPluginEditor.SkillsPath;
            try
            {
                UnityMcpPluginEditor.SkillsPath = path!;
                mcpPlugin.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
            }
            finally
            {
                UnityMcpPluginEditor.SkillsPath = originalSkillsPath;
            }
        }
    }
}