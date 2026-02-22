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

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public class TomlAiAgentConfig : AiAgentConfig
    {
        private readonly Dictionary<string, (object value, bool required, ValueComparisonMode comparison)> _properties = new();
        private readonly HashSet<string> _propertiesToRemove = new();

        /// <summary>
        /// Wraps a raw TOML value string for types not explicitly supported by the parser
        /// (e.g., floats, dates). The value is written back verbatim during serialization.
        /// </summary>
        private sealed class RawTomlValue
        {
            public string Value { get; }
            public RawTomlValue(string value) => Value = value;
        }

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

        public TomlAiAgentConfig SetProperty(string key, object value, bool requiredForConfiguration = false, ValueComparisonMode comparison = ValueComparisonMode.Exact)
        {
            _properties[key] = (value, requiredForConfiguration, comparison);
            return this;
        }

        public TomlAiAgentConfig SetProperty(string key, object[] values, bool requiredForConfiguration = false, ValueComparisonMode comparison = ValueComparisonMode.Exact)
        {
            _properties[key] = (values, requiredForConfiguration, comparison);
            return this;
        }

        public TomlAiAgentConfig SetPropertyToRemove(string key)
        {
            _propertiesToRemove.Add(key);
            return this;
        }

        public new TomlAiAgentConfig AddIdentityKey(string key)
        {
            base.AddIdentityKey(key);
            return this;
        }

        public override void ApplyHttpAuthorization(bool isRequired, string? token)
        {
            // TOML HTTP config format does not currently support injecting
            // authorization headers. Implement when TOML HTTP auth is needed.
        }

        public override void ApplyStdioAuthorization(bool isRequired, string? token)
        {
            // TOML STDIO config format does not currently support injecting
            // the token argument. Implement when TOML STDIO auth is needed.
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

                // Remove duplicate sections that represent the same server under a different name
                RemoveDuplicateServerSections(lines, sectionName);

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

        public override bool Unconfigure()
        {
            if (string.IsNullOrEmpty(ConfigPath) || !File.Exists(ConfigPath))
                return false;

            try
            {
                var sectionName = $"{BodyPath}.{DefaultMcpServerName}";
                var lines = File.ReadAllLines(ConfigPath).ToList();
                var sectionIndex = FindTomlSection(lines, sectionName);
                if (sectionIndex < 0)
                    return false;

                var sectionEnd = FindSectionEnd(lines, sectionIndex);
                lines.RemoveRange(sectionIndex, sectionEnd - sectionIndex);

                File.WriteAllText(ConfigPath, string.Join(Environment.NewLine, lines));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Consts.Log.Tag} Error unconfiguring TOML MCP client: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        public override bool IsDetected()
        {
            if (string.IsNullOrEmpty(ConfigPath) || !File.Exists(ConfigPath))
                return false;

            try
            {
                var lines = File.ReadAllLines(ConfigPath).ToList();
                var sectionName = $"{BodyPath}.{DefaultMcpServerName}";
                return FindTomlSection(lines, sectionName) >= 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Consts.Log.Tag} Error reading TOML config file: {ex.Message}");
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

                if (!ValuesMatch(prop.Value.comparison, prop.Value.value, existingValue))
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

        private static bool ValuesMatch(ValueComparisonMode comparison, object expected, object actual)
        {
            return (expected, actual) switch
            {
                (string e, string a) => AreStringValuesEquivalent(comparison, e, a),
                (string[] e, string[] a) => e.Length == a.Length && e.Zip(a, (x, y) => AreStringValuesEquivalent(comparison, x, y)).All(match => match),
                (bool e, bool a) => e == a,
                (bool[] e, bool[] a) => e.Length == a.Length && e.Zip(a, (x, y) => x == y).All(match => match),
                (int e, int a) => e == a,
                (int[] e, int[] a) => e.Length == a.Length && e.Zip(a, (x, y) => x == y).All(match => match),
                _ => false
            };
        }

        private static bool AreStringValuesEquivalent(ValueComparisonMode comparison, string expected, string actual)
        {
            if (comparison == ValueComparisonMode.Path)
                return NormalizePath(expected) == NormalizePath(actual);

            if (comparison == ValueComparisonMode.Url)
                return string.Equals(NormalizeUrl(expected), NormalizeUrl(actual), StringComparison.OrdinalIgnoreCase);

            return expected == actual;
        }

        /// <summary>Normalizes a file path by unifying separators.</summary>
        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>Normalizes a URL by lowercasing scheme+host and trimming trailing slashes.</summary>
        private static string NormalizeUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var authority = uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
                var pathPart = uri.AbsolutePath.TrimEnd('/');
                return authority + pathPart + uri.Query;
            }
            return url.TrimEnd('/');
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
                RawTomlValue raw => $"{key} = {raw.Value}",
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
                    // Array value - strip inline comment then parse with type detection
                    var arrayValue = StripArrayInlineComment(rawValue);
                    props[key] = ParseTypedTomlArrayValue(arrayValue);
                }
                else if (rawValue.StartsWith("\""))
                {
                    // Quoted string value
                    var stringValue = ParseTomlStringValue(line);
                    if (stringValue != null)
                        props[key] = stringValue;
                }
                else
                {
                    // Non-string, non-array scalar - strip inline comment first
                    var scalarValue = StripInlineComment(rawValue);

                    if (scalarValue == "true" || scalarValue == "false")
                        props[key] = scalarValue == "true";
                    else if (int.TryParse(scalarValue, out var intValue))
                        props[key] = intValue;
                    else if (scalarValue.Length > 0)
                        props[key] = new RawTomlValue(scalarValue);
                }
            }
            return props;
        }

        private static string EscapeTomlString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string StripInlineComment(string value)
        {
            var idx = value.IndexOf('#');
            return idx >= 0 ? value[..idx].TrimEnd() : value;
        }

        /// <summary>
        /// Strips an inline comment from a TOML array value while respecting quoted strings.
        /// For example, <c>["a", "b"] # comment</c> becomes <c>["a", "b"]</c>.
        /// </summary>
        private static string StripArrayInlineComment(string value)
        {
            var depth = 0;
            var inQuote = false;
            var escaped = false;

            for (int i = 0; i < value.Length; i++)
            {
                var ch = value[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\' && inQuote)
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    inQuote = !inQuote;
                    continue;
                }

                if (inQuote)
                    continue;

                if (ch == '[') depth++;
                else if (ch == ']')
                {
                    depth--;
                    if (depth == 0)
                        return value[..(i + 1)];
                }
            }

            return value;
        }

        private static string? ParseTomlStringValue(string line)
        {
            // Parse: key = "value" or key = "value" # comment
            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                return null;

            var value = parts[1].Trim();
            if (!value.StartsWith("\""))
                return value;

            // Scan for closing quote, handling escape sequences
            for (int i = 1; i < value.Length; i++)
            {
                if (value[i] == '\\')
                {
                    i++; // Skip escaped character
                    continue;
                }
                if (value[i] == '"')
                {
                    // Found closing quote - extract and unescape
                    var inner = value[1..i];
                    return inner.Replace("\\\"", "\"").Replace("\\\\", "\\");
                }
            }

            // No closing quote found - return raw value as fallback
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

            // Detect array element type by looking at the first element.
            // Typed parsers return null when any element is invalid;
            // fall back to RawTomlValue to preserve the original text.
            if (arrayContent.StartsWith("\""))
            {
                // String array
                return ParseTomlStringArrayContent(arrayContent);
            }
            else if (arrayContent.StartsWith("true", StringComparison.Ordinal) ||
                     arrayContent.StartsWith("false", StringComparison.Ordinal))
            {
                // Boolean array
                return ParseTomlBoolArrayContent(arrayContent) ?? (object)new RawTomlValue(rawValue);
            }
            else if (char.IsDigit(arrayContent[0]) || arrayContent[0] == '-')
            {
                // Integer array
                return ParseTomlIntArrayContent(arrayContent) ?? (object)new RawTomlValue(rawValue);
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

        /// <summary>
        /// Parses a comma-separated list of boolean literals.
        /// Returns null if any element is not a valid boolean, so the caller
        /// can fall back to preserving the raw value.
        /// </summary>
        private static bool[]? ParseTomlBoolArrayContent(string arrayContent)
        {
            var elements = arrayContent.Split(',');
            var result = new bool[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                var trimmed = elements[i].Trim().ToLowerInvariant();
                if (trimmed == "true") result[i] = true;
                else if (trimmed == "false") result[i] = false;
                else return null;
            }
            return result;
        }

        /// <summary>
        /// Parses a comma-separated list of integer literals.
        /// Returns null if any element is not a valid integer, so the caller
        /// can fall back to preserving the raw value.
        /// </summary>
        private static int[]? ParseTomlIntArrayContent(string arrayContent)
        {
            var elements = arrayContent.Split(',');
            var result = new int[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                if (!int.TryParse(elements[i].Trim(), out result[i]))
                    return null;
            }
            return result;
        }

        /// <summary>
        /// Removes sibling TOML sections that represent the same server under a different name,
        /// identified by matching identity key property values (e.g. "command", "url", "serverUrl").
        /// </summary>
        private void RemoveDuplicateServerSections(List<string> lines, string ownSectionName)
        {
            // Collect identity values we're about to write
            var ourIdentityValues = _identityKeys
                .Where(key => _properties.TryGetValue(key, out var prop) && prop.value is string)
                .ToDictionary(key => key, key => ((string)_properties[key].value, _properties[key].comparison));

            if (ourIdentityValues.Count == 0)
                return;

            // Find all sibling sections under the same body path
            var bodyPrefix = $"[{BodyPath}.";
            var sectionsToRemove = new List<(int start, int end)>();

            for (int i = 0; i < lines.Count; i++)
            {
                var trimmed = lines[i].Trim();
                if (!trimmed.StartsWith(bodyPrefix) || !trimmed.EndsWith("]"))
                    continue;

                // Extract section name and skip our own section
                var fullSectionName = trimmed[1..^1];
                if (fullSectionName == ownSectionName)
                    continue;

                var sectionEnd = FindSectionEnd(lines, i);
                var props = ParseSectionProperties(lines, i + 1, sectionEnd);

                // Check if any identity property matches
                if (ourIdentityValues.Any(identity =>
                        props.TryGetValue(identity.Key, out var existingValue)
                        && existingValue is string existingStr
                        && AreStringValuesEquivalent(identity.Value.comparison, identity.Value.Item1, existingStr)))
                    sectionsToRemove.Add((i, sectionEnd));
            }

            // Remove from bottom to top to preserve indices
            for (int i = sectionsToRemove.Count - 1; i >= 0; i--)
            {
                var (start, end) = sectionsToRemove[i];
                lines.RemoveRange(start, end - start);
            }
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
