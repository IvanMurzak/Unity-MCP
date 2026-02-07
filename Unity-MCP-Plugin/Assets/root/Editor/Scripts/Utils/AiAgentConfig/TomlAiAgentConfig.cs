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
using com.IvanMurzak.McpPlugin.Common;
using UnityEngine;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public class TomlAiAgentConfig : AiAgentConfig
    {
        private readonly Dictionary<string, (object value, bool required)> _properties = new();
        private readonly HashSet<string> _propertiesToRemove = new();

        public override string ExpectedFileContent
        {
            get
            {
                var sectionName = $"{BodyPath}.{DefaultMcpServerName}";
                var sb = new StringBuilder();
                sb.AppendLine($"[{sectionName}]");
                foreach (var key in _properties.Keys.OrderBy(k => k, StringComparer.Ordinal))
                {
                    sb.AppendLine(FormatTomlProperty(key, _properties[key].value));
                }
                return sb.ToString();
            }
        }

        public TomlAiAgentConfig(
            string name,
            string configPath,
            string bodyPath = Consts.MCP.Server.DefaultBodyPath)
            : base(
                name: name,
                configPath: configPath,
                bodyPath: bodyPath)
        {
            // empty
        }

        public TomlAiAgentConfig SetProperty(string key, object value, bool requiredForConfiguration = false)
        {
            _properties[key] = (value, requiredForConfiguration);
            return this;
        }

        public TomlAiAgentConfig SetProperty(string key, object[] values, bool requiredForConfiguration = false)
        {
            _properties[key] = (values, requiredForConfiguration);
            return this;
        }

        public TomlAiAgentConfig SetPropertyToRemove(string key)
        {
            _propertiesToRemove.Add(key);
            return this;
        }

        public override bool Configure()
        {
            if (string.IsNullOrEmpty(ConfigPath))
                return false;

            Debug.Log($"{Consts.Log.Tag} Configuring MCP client TOML with path: {ConfigPath} and bodyPath: {BodyPath}");

            try
            {
                var sectionName = $"{BodyPath}.{DefaultMcpServerName}";

                if (!File.Exists(ConfigPath))
                {
                    // Create all necessary directories
                    var directory = Path.GetDirectoryName(ConfigPath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    File.WriteAllText(ConfigPath, ExpectedFileContent);
                    return true;
                }

                // Read existing TOML file
                var lines = File.ReadAllLines(ConfigPath).ToList();

                // Remove deprecated sections
                foreach (var deprecatedName in DeprecatedMcpServerNames)
                {
                    var deprecatedSection = $"{BodyPath}.{deprecatedName}";
                    var deprecatedIndex = FindTomlSection(lines, deprecatedSection);
                    if (deprecatedIndex >= 0)
                    {
                        var deprecatedEnd = FindSectionEnd(lines, deprecatedIndex);
                        lines.RemoveRange(deprecatedIndex, deprecatedEnd - deprecatedIndex);
                    }
                }

                var sectionIndex = FindTomlSection(lines, sectionName);
                if (sectionIndex >= 0)
                {
                    // Section exists - merge properties
                    var sectionEndIndex = FindSectionEnd(lines, sectionIndex);
                    var existingProps = ParseSectionProperties(lines, sectionIndex + 1, sectionEndIndex);

                    // Remove specified properties
                    foreach (var key in _propertiesToRemove)
                        existingProps.Remove(key);

                    // Set/overwrite properties from _properties
                    foreach (var prop in _properties)
                        existingProps[prop.Key] = prop.Value.value;

                    // Remove old section lines
                    lines.RemoveRange(sectionIndex, sectionEndIndex - sectionIndex);

                    // Generate new section from merged properties
                    var newSection = GenerateTomlSectionFromDict(sectionName, existingProps);
                    lines.Insert(sectionIndex, newSection.TrimEnd());
                }
                else
                {
                    // Section doesn't exist - append
                    if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                        lines.Add("");

                    var propsDict = _properties.ToDictionary(p => p.Key, p => p.Value.value);
                    lines.Add(GenerateTomlSectionFromDict(sectionName, propsDict).TrimEnd());
                }

                // Write back to file
                File.WriteAllText(ConfigPath, string.Join(Environment.NewLine, lines));

                return IsConfigured();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Consts.Log.Tag} Error configuring TOML file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        public override bool IsConfigured()
        {
            if (string.IsNullOrEmpty(ConfigPath) || !File.Exists(ConfigPath))
                return false;

            try
            {
                var lines = File.ReadAllLines(ConfigPath).ToList();
                var sectionName = $"{BodyPath}.{DefaultMcpServerName}";
                var sectionIndex = FindTomlSection(lines, sectionName);
                if (sectionIndex < 0)
                    return false;

                var sectionEndIndex = FindSectionEnd(lines, sectionIndex);
                var existingProps = ParseSectionProperties(lines, sectionIndex + 1, sectionEndIndex);

                return AreRequiredPropertiesMatching(existingProps) && !HasPropertiesToRemove(existingProps);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Consts.Log.Tag} Error reading TOML config file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        private bool AreRequiredPropertiesMatching(Dictionary<string, object> existingProps)
        {
            foreach (var prop in _properties)
            {
                if (!prop.Value.required)
                    continue;

                if (!existingProps.TryGetValue(prop.Key, out var existingValue))
                    return false;

                if (!ValuesMatch(prop.Value.value, existingValue))
                    return false;
            }

            return true;
        }

        private bool HasPropertiesToRemove(Dictionary<string, object> existingProps)
        {
            if (_propertiesToRemove.Count == 0)
                return false;

            return _propertiesToRemove.Any(key => existingProps.ContainsKey(key));
        }

        private static bool ValuesMatch(object expected, object actual)
        {
            return (expected, actual) switch
            {
                (string e, string a) => e == a,
                (string[] e, string[] a) => e.Length == a.Length && e.Zip(a, (x, y) => x == y).All(match => match),
                (bool e, bool a) => e == a,
                (bool[] e, bool[] a) => e.Length == a.Length && e.Zip(a, (x, y) => x == y).All(match => match),
                (int e, int a) => e == a,
                (int[] e, int[] a) => e.Length == a.Length && e.Zip(a, (x, y) => x == y).All(match => match),
                _ => false
            };
        }

        private static string GenerateTomlSectionFromDict(string sectionName, Dictionary<string, object> properties)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{sectionName}]");
            foreach (var key in properties.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                sb.AppendLine(FormatTomlProperty(key, properties[key]));
            }
            return sb.ToString();
        }

        private static string FormatTomlProperty(string key, object value)
        {
            return value switch
            {
                string s => $"{key} = \"{EscapeTomlString(s)}\"",
                string[] arr => $"{key} = [{string.Join(",", arr.Select(v => $"\"{EscapeTomlString(v)}\""))}]",
                int i => $"{key} = {i}",
                int[] arr => $"{key} = [{string.Join(",", arr)}]",
                bool b => $"{key} = {b.ToString().ToLower()}",
                bool[] arr => $"{key} = [{string.Join(",", arr.Select(v => v.ToString().ToLower()))}]",
                _ => throw new InvalidOperationException($"Unsupported TOML value type: {value.GetType()}")
            };
        }

        private static Dictionary<string, object> ParseSectionProperties(List<string> lines, int startIndex, int endIndex)
        {
            var props = new Dictionary<string, object>();
            for (int i = startIndex; i < endIndex; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var rawValue = parts[1].Trim();

                if (rawValue.StartsWith("["))
                {
                    // Array value - parse with type detection
                    props[key] = ParseTypedTomlArrayValue(rawValue);
                }
                else if (rawValue == "true" || rawValue == "false")
                {
                    // Boolean value
                    props[key] = rawValue == "true";
                }
                else if (int.TryParse(rawValue, out var intValue))
                {
                    // Integer value
                    props[key] = intValue;
                }
                else
                {
                    // String value
                    var stringValue = ParseTomlStringValue(line);
                    if (stringValue != null)
                        props[key] = stringValue;
                }
            }
            return props;
        }

        private static string EscapeTomlString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string? ParseTomlStringValue(string line)
        {
            // Parse: key = "value"
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

        private static object ParseTypedTomlArrayValue(string rawValue)
        {
            // Remove array brackets
            if (!rawValue.StartsWith("[") || !rawValue.EndsWith("]"))
                return Array.Empty<string>();

            var arrayContent = rawValue[1..^1].Trim();
            if (string.IsNullOrEmpty(arrayContent))
                return Array.Empty<string>();

            // Detect array element type by looking at the first element
            if (arrayContent.StartsWith("\""))
            {
                // String array
                return ParseTomlStringArrayContent(arrayContent);
            }
            else if (arrayContent.StartsWith("true", StringComparison.Ordinal) ||
                     arrayContent.StartsWith("false", StringComparison.Ordinal))
            {
                // Boolean array
                return ParseTomlBoolArrayContent(arrayContent);
            }
            else if (char.IsDigit(arrayContent[0]) || arrayContent[0] == '-')
            {
                // Integer array
                return ParseTomlIntArrayContent(arrayContent);
            }

            // Fallback to string array
            return ParseTomlStringArrayContent(arrayContent);
        }

        private static string[] ParseTomlStringArrayContent(string arrayContent)
        {
            var result = new List<string>();
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
                        var parsedValue = currentValue.ToString();
                        parsedValue = parsedValue.Replace("\\\"", "\"").Replace("\\\\", "\\");
                        result.Add(parsedValue);
                        currentValue.Clear();
                    }
                    inQuote = !inQuote;
                }
                else if (inQuote)
                {
                    currentValue.Append(ch);
                }
            }

            return result.ToArray();
        }

        private static bool[] ParseTomlBoolArrayContent(string arrayContent)
        {
            var elements = arrayContent.Split(',');
            var result = new List<bool>();

            foreach (var element in elements)
            {
                var trimmed = element.Trim();
                if (trimmed == "true")
                    result.Add(true);
                else if (trimmed == "false")
                    result.Add(false);
            }

            return result.ToArray();
        }

        private static int[] ParseTomlIntArrayContent(string arrayContent)
        {
            var elements = arrayContent.Split(',');
            var result = new List<int>();

            foreach (var element in elements)
            {
                var trimmed = element.Trim();
                if (int.TryParse(trimmed, out var intValue))
                    result.Add(intValue);
            }

            return result.ToArray();
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
