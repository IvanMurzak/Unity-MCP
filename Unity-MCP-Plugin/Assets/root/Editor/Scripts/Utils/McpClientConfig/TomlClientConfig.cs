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

        public override bool Configure() => ConfigureTomlMcpClient(ConfigPath, Consts.MCP.Server.DefaultServerName, BodyPath);
        public override bool IsConfigured() => IsMcpClientConfigured(ConfigPath, Consts.MCP.Server.DefaultServerName, BodyPath);

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

                return IsMcpClientConfigured(configPath, serverName, bodyPath);
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
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return false;

            try
            {
                var sectionName = $"{bodyPath}.{serverName}";
                var lines = File.ReadAllLines(configPath);

                // Find the section
                var sectionIndex = FindTomlSection(lines.ToList(), sectionName);
                if (sectionIndex < 0)
                    return false;

                // Parse the section to extract command and args
                var sectionEndIndex = FindSectionEnd(lines.ToList(), sectionIndex);
                string? command = null;
                List<string> args = new();

                for (int i = sectionIndex + 1; i < sectionEndIndex; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    // Parse command line
                    if (line.StartsWith("command"))
                    {
                        command = ParseTomlStringValue(line);
                    }
                    // Parse args array
                    else if (line.StartsWith("args"))
                    {
                        args = ParseTomlArrayValue(line);
                    }
                }

                // Validate command matches
                if (command == null || !IsCommandMatch(command))
                    return false;

                // Validate arguments match
                return DoArgumentsMatch(args);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Consts.Log.Tag} Error reading TOML config file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        private static string? ParseTomlStringValue(string line)
        {
            // Parse: command = "value"
            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                return null;

            var value = parts[1].Trim();
            // Remove quotes
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value[1..^1];
                // Unescape
                value = value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            }

            return value;
        }

        private static List<string> ParseTomlArrayValue(string line)
        {
            // Parse: args = ["value1","value2","value3"]
            var result = new List<string>();
            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                return result;

            var arrayContent = parts[1].Trim();
            // Remove array brackets
            if (arrayContent.StartsWith("[") && arrayContent.EndsWith("]"))
            {
                arrayContent = arrayContent[1..^1];

                // Simple parsing - split by comma and extract quoted strings
                var inQuote = false;
                var escaped = false;
                var currentValue = new StringBuilder();

                foreach (var ch in arrayContent)
                {
                    if (escaped)
                    {
                        currentValue.Append(ch);
                        escaped = false;
                        continue;
                    }

                    if (ch == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (ch == '"')
                    {
                        if (inQuote)
                        {
                            // End of quoted string
                            var value = currentValue.ToString();
                            value = value.Replace("\\\"", "\"").Replace("\\\\", "\\");
                            result.Add(value);
                            currentValue.Clear();
                        }
                        inQuote = !inQuote;
                    }
                    else if (inQuote)
                    {
                        currentValue.Append(ch);
                    }
                }
            }

            return result;
        }

        private static bool IsCommandMatch(string command)
        {
            // Normalize both paths for comparison
            try
            {
                var normalizedCommand = Path.GetFullPath(command.Replace('/', Path.DirectorySeparatorChar));
                var normalizedTarget = Path.GetFullPath(Startup.Server.ExecutableFullPath.Replace('/', Path.DirectorySeparatorChar));
                return string.Equals(normalizedCommand, normalizedTarget, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // If normalization fails, fallback to string comparison
                return string.Equals(command, Startup.Server.ExecutableFullPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool DoArgumentsMatch(List<string> args)
        {
            if (args == null || args.Count == 0)
                return false;

            var targetPort = UnityMcpPlugin.Port.ToString();
            var targetTimeout = UnityMcpPlugin.TimeoutMs.ToString();

            var foundPort = false;
            var foundTimeout = false;

            // Check for both positional and named argument formats
            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                if (string.IsNullOrEmpty(arg))
                    continue;

                // Check positional format
                if (i == 0 && arg == targetPort)
                    foundPort = true;
                else if (i == 1 && arg == targetTimeout)
                    foundTimeout = true;

                // Check named format
                else if (arg.StartsWith($"{Consts.MCP.Server.Args.Port}=") && arg[(Consts.MCP.Server.Args.Port.Length + 1)..] == targetPort)
                    foundPort = true;
                else if (arg.StartsWith($"{Consts.MCP.Server.Args.PluginTimeout}=") && arg[(Consts.MCP.Server.Args.PluginTimeout.Length + 1)..] == targetTimeout)
                    foundTimeout = true;
            }

            return foundPort && foundTimeout;
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