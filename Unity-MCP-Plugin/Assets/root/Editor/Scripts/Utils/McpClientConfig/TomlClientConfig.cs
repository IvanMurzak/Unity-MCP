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
using com.IvanMurzak.Unity.MCP.Common;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public class TomlClientConfig : ClientConfig
    {
        public TomlClientConfig(string name, string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
            : base(name, configPath, bodyPath)
        {
        }

        public override bool Configure() => ConfigureTomlMcpClient(ConfigPath, Name, BodyPath);
        public override bool IsConfigured() => IsMcpClientConfigured(ConfigPath, Name, BodyPath);

        public static bool ConfigureTomlMcpClient(string configPath, string serverName = Consts.MCP.Server.DefaultServerName, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
        {
            if (string.IsNullOrEmpty(configPath))
                return false;

            Debug.Log($"{Consts.Log.Tag} Configuring MCP client TOML with path: {configPath} and bodyPath: {bodyPath}");

            try
            {
                var sectionName = $"{bodyPath}.{serverName}";

                var commandPath = Startup.Server.ExecutableFullPath.Replace('\\', '/');
                var args = new[]
                {
                    $"--port={UnityMcpPlugin.Port}",
                    $"--plugin-timeout={UnityMcpPlugin.TimeoutMs}",
                    $"--client-transport=stdio"
                };

                if (!File.Exists(configPath))
                {
                    // Create all necessary directories
                    var directory = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    // Create new TOML file with Unity-MCP configuration
                    var tomlContent = GenerateTomlSection(sectionName, commandPath, args);
                    File.WriteAllText(configPath, tomlContent);
                    Debug.Log($"{Consts.Log.Tag} Created new TOML config file");
                    return true;
                }

                // Read existing TOML file
                var lines = File.ReadAllLines(configPath).ToList();

                // Find or update the Unity-MCP section
                var sectionIndex = FindTomlSection(lines, sectionName);

                if (sectionIndex >= 0)
                {
                    // Section exists - update it
                    var sectionEndIndex = FindSectionEnd(lines, sectionIndex);

                    // Remove old section
                    lines.RemoveRange(sectionIndex, sectionEndIndex - sectionIndex);

                    // Insert updated section at the same position
                    var newSection = GenerateTomlSection(sectionName, commandPath, args);
                    lines.Insert(sectionIndex, newSection.TrimEnd());

                    Debug.Log($"{Consts.Log.Tag} Updated existing TOML section [{sectionName}]");
                }
                else
                {
                    // Section doesn't exist - add it
                    // Add blank line if file is not empty and doesn't end with a blank line
                    if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                        lines.Add("");

                    lines.Add(GenerateTomlSection(sectionName, commandPath, args).TrimEnd());

                    Debug.Log($"{Consts.Log.Tag} Added new TOML section [{sectionName}]");
                }

                // Write back to file
                File.WriteAllText(configPath, string.Join(Environment.NewLine, lines));

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Consts.Log.Tag} Error configuring TOML file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        public static bool IsMcpClientConfigured(string configPath, string serverName = Consts.MCP.Server.DefaultServerName, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
        {
            return false;
        }
        private static string GenerateTomlSection(string sectionName, string command, string[] args)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{sectionName}]");
            sb.AppendLine($"command = \"{EscapeTomlString(command)}\"");

            // Format args as TOML array
            sb.Append("args = [");
            for (int i = 0; i < args.Length; i++)
            {
                sb.Append($"\"{EscapeTomlString(args[i])}\"");
                if (i < args.Length - 1)
                    sb.Append(",");
            }
            sb.AppendLine("]");

            return sb.ToString();
        }

        private static string EscapeTomlString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static int FindTomlSection(List<string> lines, string sectionName)
        {
            var sectionHeader = $"[{sectionName}]";
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim() == sectionHeader)
                    return i;
            }
            return -1;
        }

        private static int FindSectionEnd(List<string> lines, int sectionStartIndex)
        {
            // Find the next section or end of file
            for (int i = sectionStartIndex + 1; i < lines.Count; i++)
            {
                var trimmed = lines[i].Trim();
                // New section starts with [
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    return i;
            }
            return lines.Count;
        }
    }
}