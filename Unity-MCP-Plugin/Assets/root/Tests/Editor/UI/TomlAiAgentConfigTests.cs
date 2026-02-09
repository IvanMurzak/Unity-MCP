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
using System.Collections;
using System.IO;
using System.Linq;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    using Consts = McpPlugin.Common.Consts;
    using TransportMethod = Consts.MCP.Server.TransportMethod;

    public class TomlAiAgentConfigTests : BaseTest
    {
        private string tempConfigPath = null!;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();
            tempConfigPath = Path.GetTempFileName();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);

            yield return base.TearDown();
        }

        private TomlAiAgentConfig CreateStdioConfig(string configPath, string bodyPath = "mcp_servers")
        {
            return new TomlAiAgentConfig(
                name: "Test",
                configPath: configPath,
                bodyPath: bodyPath)
            .SetProperty("command", McpServerManager.ExecutableFullPath.Replace('\\', '/'), requiredForConfiguration: true)
            .SetProperty("args", new[] {
                $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
                $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
                $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
            }, requiredForConfiguration: true)
            .SetPropertyToRemove("url");
        }

        private TomlAiAgentConfig CreateHttpConfig(string configPath, string bodyPath = "mcp_servers")
        {
            return new TomlAiAgentConfig(
                name: "Test",
                configPath: configPath,
                bodyPath: bodyPath)
            .SetProperty("url", UnityMcpPlugin.Host, requiredForConfiguration: true)
            .SetPropertyToRemove("command")
            .SetPropertyToRemove("args");
        }

        #region Configure - New File

        [UnityTest]
        public IEnumerator Configure_NewFile_CreatesCorrectStructure()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");
            Assert.IsTrue(File.Exists(tempConfigPath), "Config file should be created");

            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Should contain correct section header");
            Assert.IsTrue(content.Contains("command = "), "Should contain command property");
            Assert.IsTrue(content.Contains("args = ["), "Should contain args property");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_NewFile_ContainsAllArguments()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains($"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"), "Should contain port argument");
            Assert.IsTrue(content.Contains($"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}"), "Should contain timeout argument");
            Assert.IsTrue(content.Contains($"{Consts.MCP.Server.Args.ClientTransportMethod}=stdio"), "Should contain transport argument");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_HttpConfig_NewFile_CreatesCorrectStructure()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = CreateHttpConfig(tempConfigPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Should contain correct section header");
            Assert.IsTrue(content.Contains($"url = \"{UnityMcpPlugin.Host}\""), "Should contain url property");
            Assert.IsFalse(content.Contains("command = "), "Should not contain command property");
            Assert.IsFalse(content.Contains("args = ["), "Should not contain args property");

            yield return null;
        }

        #endregion

        #region Configure - Existing File

        [UnityTest]
        public IEnumerator Configure_ExistingFile_PreservesOtherSections()
        {
            // Arrange
            var existingToml = "[other_section]\nkey = \"value\"\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("[other_section]"), "Other sections should be preserved");
            Assert.IsTrue(content.Contains("key = \"value\""), "Other section properties should be preserved");
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Should contain server section");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_MergesProperties()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\ncustom_prop = \"should-stay\"\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("custom_prop = \"should-stay\""), "Custom properties should be preserved");
            Assert.IsFalse(content.Contains("old-command"), "Old command should be overwritten");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_RemovesSpecifiedProperties()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\nurl = \"http://old-url\"\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var content = File.ReadAllText(tempConfigPath);
            Assert.IsFalse(content.Contains("url = "), "url property should be removed by SetPropertyToRemove");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_MultipleCalls_DoesNotDuplicate()
        {
            // Arrange
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            var sectionHeader = $"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]";
            var firstIndex = content.IndexOf(sectionHeader);
            var secondIndex = content.IndexOf(sectionHeader, firstIndex + 1);
            Assert.AreEqual(-1, secondIndex, "Should have only one server section after multiple configures");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_EmptyConfigPath_ReturnsFalse()
        {
            // Arrange
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: "",
                bodyPath: "mcp_servers")
            .SetProperty("command", "some-command", requiredForConfiguration: true);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsFalse(result, "Configure should return false for empty config path");

            yield return null;
        }

        #endregion

        #region IsConfigured

        [UnityTest]
        public IEnumerator IsConfigured_AfterConfigure_ReturnsTrue()
        {
            // Arrange
            var config = CreateStdioConfig(tempConfigPath);
            config.Configure();

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should detect that client is configured");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_HttpAfterConfigure_ReturnsTrue()
        {
            // Arrange
            var config = CreateHttpConfig(tempConfigPath);
            config.Configure();

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should detect that HTTP client is configured");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "non_existent_config.toml");
            var config = CreateStdioConfig(nonExistentPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false for non-existent file");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_EmptyFile_ReturnsFalse()
        {
            // Arrange
            File.WriteAllText(tempConfigPath, "");
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false for empty file");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_WrongCommand_ReturnsFalse()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var wrongCommandToml = $"[{sectionName}]\ncommand = \"wrong-command\"\nargs = [\"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}\",\"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}\",\"{Consts.MCP.Server.Args.ClientTransportMethod}=stdio\"]\n";
            File.WriteAllText(tempConfigPath, wrongCommandToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when command doesn't match");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_MissingArgs_ReturnsFalse()
        {
            // Arrange
            var executable = McpServerManager.ExecutableFullPath.Replace('\\', '/');
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var missingArgsToml = $"[{sectionName}]\ncommand = \"{executable}\"\n";
            File.WriteAllText(tempConfigPath, missingArgsToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when args are missing");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_HasPropertyToRemove_ReturnsFalse()
        {
            // Arrange - stdio config has SetPropertyToRemove("url"), so if url exists it should fail
            var executable = McpServerManager.ExecutableFullPath.Replace('\\', '/');
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var argsStr = $"\"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}\",\"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}\",\"{Consts.MCP.Server.Args.ClientTransportMethod}=stdio\"";
            var tomlWithUrl = $"[{sectionName}]\ncommand = \"{executable}\"\nargs = [{argsStr}]\nurl = \"http://some-url\"\n";
            File.WriteAllText(tempConfigPath, tomlWithUrl);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when a property marked for removal is present");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_DifferentBodyPath_ReturnsFalse()
        {
            // Arrange - configure at "mcp_servers" but check at a different path
            var configInstance = CreateStdioConfig(tempConfigPath, "mcp_servers");
            configInstance.Configure();

            var checkInstance = CreateStdioConfig(tempConfigPath, "other_path");

            // Act
            var isConfigured = checkInstance.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false for different body path");

            yield return null;
        }

        #endregion

        #region CodexConfigurator-like setup with boolean and integer properties

        /// <summary>
        /// Helper method that replicates the exact CodexConfigurator setup
        /// </summary>
        private TomlAiAgentConfig CreateCodexLikeConfig(string configPath, string bodyPath = "mcp_servers")
        {
            return new TomlAiAgentConfig(
                name: "Codex",
                configPath: configPath,
                bodyPath: bodyPath)
            .SetProperty("enabled", true, requiredForConfiguration: true) // Codex requires an "enabled" property
            .SetProperty("command", McpServerManager.ExecutableFullPath.Replace('\\', '/'), requiredForConfiguration: true)
            .SetProperty("args", new[] {
                $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
                $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
                $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
            }, requiredForConfiguration: true)
            .SetProperty("tool_timeout_sec", 300, requiredForConfiguration: false) // Optional: Set a longer tool timeout for Codex
            .SetPropertyToRemove("url")
            .SetPropertyToRemove("type");
        }

        [UnityTest]
        public IEnumerator Configure_CodexLikeConfig_IsConfiguredReturnsTrue()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = CreateCodexLikeConfig(tempConfigPath);

            // Act
            var configureResult = config.Configure();
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(configureResult, "Configure should return true");
            Assert.IsTrue(isConfigured, "IsConfigured should return true after Configure with boolean and integer properties");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_WithBooleanProperty_IsConfiguredReturnsTrue()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("enabled", true, requiredForConfiguration: true);

            // Act
            var configureResult = config.Configure();
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(configureResult, "Configure should return true");
            Assert.IsTrue(isConfigured, "IsConfigured should return true for boolean property");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_WithIntegerProperty_IsConfiguredReturnsTrue()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("timeout", 300, requiredForConfiguration: true);

            // Act
            var configureResult = config.Configure();
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(configureResult, "Configure should return true");
            Assert.IsTrue(isConfigured, "IsConfigured should return true for integer property");

            yield return null;
        }

        #endregion

        #region ExpectedFileContent

        [UnityTest]
        public IEnumerator ExpectedFileContent_ContainsCorrectSection()
        {
            // Arrange
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            var content = config.ExpectedFileContent;

            // Assert
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Should contain correct section header");
            Assert.IsTrue(content.Contains("command = "), "Should contain command");
            Assert.IsTrue(content.Contains("args = ["), "Should contain args");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpectedFileContent_HttpConfig_ContainsUrl()
        {
            // Arrange
            var config = CreateHttpConfig(tempConfigPath);

            // Act
            var content = config.ExpectedFileContent;

            // Assert
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Should contain correct section header");
            Assert.IsTrue(content.Contains($"url = \"{UnityMcpPlugin.Host}\""), "Should contain url");
            Assert.IsFalse(content.Contains("command = "), "Should not contain command");
            Assert.IsFalse(content.Contains("args = ["), "Should not contain args");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpectedFileContent_CustomBodyPath_UsesCorrectSection()
        {
            // Arrange
            var config = CreateStdioConfig(tempConfigPath, "custom_path");

            // Act
            var content = config.ExpectedFileContent;

            // Assert
            Assert.IsTrue(content.Contains($"[custom_path.{AiAgentConfig.DefaultMcpServerName}]"), "Should use custom body path in section name");

            yield return null;
        }

        #endregion

        #region Typed Array Parsing

        [UnityTest]
        public IEnumerator Configure_WithIntArray_IsConfiguredReturnsTrue()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("ports", new[] { 8080, 8081, 8082 }, requiredForConfiguration: true);

            // Act
            var configureResult = config.Configure();
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(configureResult, "Configure should return true");
            Assert.IsTrue(isConfigured, "IsConfigured should return true for int[] property");

            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("ports = [8080,8081,8082]"), "Should contain correct int array format");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_WithBoolArray_IsConfiguredReturnsTrue()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("flags", new[] { true, false, true }, requiredForConfiguration: true);

            // Act
            var configureResult = config.Configure();
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(configureResult, "Configure should return true");
            Assert.IsTrue(isConfigured, "IsConfigured should return true for bool[] property");

            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("flags = [true,false,true]"), "Should contain correct bool array format");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_WithStringArray_IsConfiguredReturnsTrue()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("names", new[] { "alpha", "beta", "gamma" }, requiredForConfiguration: true);

            // Act
            var configureResult = config.Configure();
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(configureResult, "Configure should return true");
            Assert.IsTrue(isConfigured, "IsConfigured should return true for string[] property");

            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("names = [\"alpha\",\"beta\",\"gamma\"]"), "Should contain correct string array format");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_ExistingIntArray_MatchesCorrectly()
        {
            // Arrange - manually write a TOML file with int array
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var toml = $"[{sectionName}]\nports = [8080, 8081, 8082]\n";
            File.WriteAllText(tempConfigPath, toml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("ports", new[] { 8080, 8081, 8082 }, requiredForConfiguration: true);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should correctly parse and match int[] from existing TOML");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_ExistingBoolArray_MatchesCorrectly()
        {
            // Arrange - manually write a TOML file with bool array
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var toml = $"[{sectionName}]\nflags = [true, false, true]\n";
            File.WriteAllText(tempConfigPath, toml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("flags", new[] { true, false, true }, requiredForConfiguration: true);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should correctly parse and match bool[] from existing TOML");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_ExistingStringArray_MatchesCorrectly()
        {
            // Arrange - manually write a TOML file with string array
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var toml = $"[{sectionName}]\nnames = [\"alpha\", \"beta\", \"gamma\"]\n";
            File.WriteAllText(tempConfigPath, toml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("names", new[] { "alpha", "beta", "gamma" }, requiredForConfiguration: true);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should correctly parse and match string[] from existing TOML");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_MismatchedIntArray_ReturnsFalse()
        {
            // Arrange - manually write a TOML file with different int array values
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var toml = $"[{sectionName}]\nports = [9000, 9001]\n";
            File.WriteAllText(tempConfigPath, toml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("ports", new[] { 8080, 8081, 8082 }, requiredForConfiguration: true);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when int[] values don't match");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_MismatchedBoolArray_ReturnsFalse()
        {
            // Arrange - manually write a TOML file with different bool array values
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var toml = $"[{sectionName}]\nflags = [false, false]\n";
            File.WriteAllText(tempConfigPath, toml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("flags", new[] { true, false, true }, requiredForConfiguration: true);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when bool[] values don't match");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingFileWithIntArray_MergesCorrectly()
        {
            // Arrange - existing file with an int array property
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\nports = [1, 2, 3]\ncustom_prop = \"keep\"\n";
            File.WriteAllText(tempConfigPath, existingToml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("ports", new[] { 8080, 8081 }, requiredForConfiguration: true);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("ports = [8080,8081]"), "Should overwrite int array");
            Assert.IsTrue(content.Contains("custom_prop = \"keep\""), "Should preserve other properties");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_EmptyArray_HandledCorrectly()
        {
            // Arrange - existing file with empty array
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\nempty = []\n";
            File.WriteAllText(tempConfigPath, existingToml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("value", 42, requiredForConfiguration: true);

            // Act
            config.Configure();
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should handle empty arrays in existing file");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_NegativeIntArray_HandledCorrectly()
        {
            // Arrange
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("offsets", new[] { -10, 0, 10 }, requiredForConfiguration: true);

            // Act
            var configureResult = config.Configure();
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(configureResult, "Configure should return true");
            Assert.IsTrue(isConfigured, "IsConfigured should return true for int[] with negative values");

            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("offsets = [-10,0,10]"), "Should contain correct int array with negative values");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_ExistingNegativeIntArray_MatchesCorrectly()
        {
            // Arrange - manually write a TOML file with negative int array
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var toml = $"[{sectionName}]\noffsets = [-10, 0, 10]\n";
            File.WriteAllText(tempConfigPath, toml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("offsets", new[] { -10, 0, 10 }, requiredForConfiguration: true);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should correctly parse and match int[] with negative values from existing TOML");

            yield return null;
        }

        #endregion

        #region Deterministic Property Order

        [UnityTest]
        public IEnumerator ExpectedFileContent_PropertiesInAlphabeticalOrder()
        {
            // Arrange - add properties in reverse alphabetical order
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("zebra", "last")
            .SetProperty("alpha", "first")
            .SetProperty("middle", "mid");

            // Act
            var content = config.ExpectedFileContent;
            var lines = content.Split('\n');

            // Assert - find property lines (skip section header, skip empty)
            var propLines = lines
                .Select(line => line.Trim())
                .Where(t => !string.IsNullOrEmpty(t) && !t.StartsWith("[") && t.Contains(" = "))
                .ToList();

            Assert.AreEqual(3, propLines.Count, "Should have 3 properties");
            Assert.IsTrue(propLines[0].StartsWith("alpha"), "First property should be 'alpha'");
            Assert.IsTrue(propLines[1].StartsWith("middle"), "Second property should be 'middle'");
            Assert.IsTrue(propLines[2].StartsWith("zebra"), "Third property should be 'zebra'");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_NewFile_PropertiesInAlphabeticalOrder()
        {
            // Arrange - add properties in reverse alphabetical order
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("zebra", "last")
            .SetProperty("alpha", "first")
            .SetProperty("middle", "mid");

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            var alphaIdx = content.IndexOf("alpha = ");
            var middleIdx = content.IndexOf("middle = ");
            var zebraIdx = content.IndexOf("zebra = ");

            Assert.IsTrue(alphaIdx >= 0, "Should contain alpha");
            Assert.IsTrue(middleIdx >= 0, "Should contain middle");
            Assert.IsTrue(zebraIdx >= 0, "Should contain zebra");
            Assert.IsTrue(alphaIdx < middleIdx, "alpha should appear before middle");
            Assert.IsTrue(middleIdx < zebraIdx, "middle should appear before zebra");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_MergedPropertiesInAlphabeticalOrder()
        {
            // Arrange - existing file with a section, then configure with new properties in reverse order
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\nexisting = \"value\"\n";
            File.WriteAllText(tempConfigPath, existingToml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("zebra", "last")
            .SetProperty("alpha", "first");

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            var alphaIdx = content.IndexOf("alpha = ");
            var existingIdx = content.IndexOf("existing = ");
            var zebraIdx = content.IndexOf("zebra = ");

            Assert.IsTrue(alphaIdx >= 0, "Should contain alpha");
            Assert.IsTrue(existingIdx >= 0, "Should contain existing");
            Assert.IsTrue(zebraIdx >= 0, "Should contain zebra");
            Assert.IsTrue(alphaIdx < existingIdx, "alpha should appear before existing");
            Assert.IsTrue(existingIdx < zebraIdx, "existing should appear before zebra");

            yield return null;
        }

        #endregion

        #region Duplicate Server Section Removal

        [UnityTest]
        public IEnumerator Configure_Stdio_RemovesDuplicateByCommand()
        {
            // Arrange - existing file with the same server under a custom name
            var executable = McpServerManager.ExecutableFullPath.Replace('\\', '/');
            var existingToml = $"[mcp_servers.my-custom-name]\ncommand = \"{executable}\"\nargs = [\"--old-arg\"]\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsFalse(content.Contains("[mcp_servers.my-custom-name]"), "Duplicate section with same command should be removed");
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Default section should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_Http_RemovesDuplicateByUrl()
        {
            // Arrange - existing file with the same server under a custom name
            var existingToml = $"[mcp_servers.my-custom-name]\nurl = \"{UnityMcpPlugin.Host}\"\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateHttpConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsFalse(content.Contains("[mcp_servers.my-custom-name]"), "Duplicate section with same url should be removed");
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Default section should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_Http_RemovesDuplicateByServerUrl()
        {
            // Arrange - existing file with the same server under a custom name using serverUrl
            var existingToml = $"[mcp_servers.my-custom-name]\nserverUrl = \"{UnityMcpPlugin.Host}\"\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .AddIdentityKey("serverUrl")
            .SetProperty("serverUrl", UnityMcpPlugin.Host, requiredForConfiguration: true)
            .SetPropertyToRemove("command")
            .SetPropertyToRemove("args")
            .SetPropertyToRemove("url");

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsFalse(content.Contains("[mcp_servers.my-custom-name]"), "Duplicate section with same serverUrl should be removed");
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Default section should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_Http_DefaultIdentityKeys_DoNotRemoveByServerUrl()
        {
            // Arrange - existing file with a server using serverUrl but config does NOT add serverUrl identity key
            var existingToml = $"[mcp_servers.my-custom-name]\nserverUrl = \"{UnityMcpPlugin.Host}\"\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateHttpConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert - without adding serverUrl as identity key, the entry should be preserved
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("[mcp_servers.my-custom-name]"), "Entry with serverUrl should be preserved when serverUrl is not an identity key");
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Default section should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_PreservesUnrelatedServers()
        {
            // Arrange - existing file with a different server (different command)
            var existingToml = "[mcp_servers.other-server]\ncommand = \"completely-different-command\"\nargs = [\"--some-arg\"]\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("[mcp_servers.other-server]"), "Unrelated server should be preserved");
            Assert.IsTrue(content.Contains($"[mcp_servers.{AiAgentConfig.DefaultMcpServerName}]"), "Default section should exist");

            yield return null;
        }

        #endregion

        #region Inline Comments and Unknown Types

        [UnityTest]
        public IEnumerator Configure_ExistingSection_PreservesFloatValue()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\ntimeout = 1.5\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("timeout = 1.5"), "Float value should be preserved verbatim");
            Assert.IsFalse(content.Contains("timeout = \"1.5\""), "Float value should not become a quoted string");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_PreservesDateValue()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\ncreated = 2024-01-01\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("created = 2024-01-01"), "Date value should be preserved verbatim");
            Assert.IsFalse(content.Contains("created = \"2024-01-01\""), "Date value should not become a quoted string");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_InlineCommentOnInt()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\nport = 8080 # default port\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("port = 8080"), "Int value should be preserved after stripping inline comment");
            Assert.IsFalse(content.Contains("# default port"), "Inline comment should be stripped");
            Assert.IsFalse(content.Contains("port = \""), "Int should not become a quoted string");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_InlineCommentOnBool()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\nenabled = true # some flag\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("enabled = true"), "Bool value should be preserved after stripping inline comment");
            Assert.IsFalse(content.Contains("# some flag"), "Inline comment should be stripped");
            Assert.IsFalse(content.Contains("enabled = \""), "Bool should not become a quoted string");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_InlineCommentOnQuotedString()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\nname = \"hello\" # some note\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("name = \"hello\""), "Quoted string should be preserved after stripping inline comment");
            Assert.IsFalse(content.Contains("# some note"), "Inline comment should be stripped");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_InlineCommentOnStringArray()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\nnames = [\"alpha\", \"beta\"] # some list\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("names = [\"alpha\",\"beta\"]"), "String array should be preserved after stripping inline comment");
            Assert.IsFalse(content.Contains("# some list"), "Inline comment should be stripped");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_InlineCommentOnIntArray()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\nports = [8080, 8081] # default ports\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("ports = [8080,8081]"), "Int array should be preserved after stripping inline comment");
            Assert.IsFalse(content.Contains("# default ports"), "Inline comment should be stripped");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingSection_InlineCommentOnBoolArray()
        {
            // Arrange
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var existingToml = $"[{sectionName}]\ncommand = \"old-command\"\nflags = [true, false] # feature flags\n";
            File.WriteAllText(tempConfigPath, existingToml);
            var config = CreateStdioConfig(tempConfigPath);

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            Assert.IsTrue(content.Contains("flags = [true,false]"), "Bool array should be preserved after stripping inline comment");
            Assert.IsFalse(content.Contains("# feature flags"), "Inline comment should be stripped");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_StringArrayWithInlineComment_MatchesCorrectly()
        {
            // Arrange - manually write TOML with inline comment on a required string array
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var toml = $"[{sectionName}]\nnames = [\"alpha\", \"beta\"] # a comment\n";
            File.WriteAllText(tempConfigPath, toml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("names", new[] { "alpha", "beta" }, requiredForConfiguration: true);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "IsConfigured should return true for string array with inline comment");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_StringArrayWithHashInsideQuotes_MatchesCorrectly()
        {
            // Arrange - string array where a value contains a # character
            var sectionName = $"mcp_servers.{AiAgentConfig.DefaultMcpServerName}";
            var toml = $"[{sectionName}]\ntags = [\"C#\", \"F#\"] # languages\n";
            File.WriteAllText(tempConfigPath, toml);

            var config = new TomlAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcp_servers")
            .SetProperty("tags", new[] { "C#", "F#" }, requiredForConfiguration: true);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "IsConfigured should handle # inside quoted strings correctly");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_WithInlineComments_ReturnsTrue()
        {
            // Arrange
            var config = CreateStdioConfig(tempConfigPath);

            // First configure normally
            config.Configure();

            // Now manually add an inline comment to the command line
            var lines = File.ReadAllLines(tempConfigPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("command ="))
                {
                    lines[i] += " # path to executable";
                    break;
                }
            }
            File.WriteAllLines(tempConfigPath, lines);

            // Act & Assert
            Assert.IsTrue(config.IsConfigured(), "IsConfigured should return true even with inline comments on required properties");

            yield return null;
        }

        #endregion

        #region ValueComparisonMode - Path and URL Normalization

        [UnityTest]
        public IEnumerator IsConfigured_PathComparison_BackslashEqualsForwardSlash()
        {
            // Arrange - write config with backslashes, register with forward slashes
            var bodyPath = "mcp_servers";
            var sectionName = $"{bodyPath}.{AiAgentConfig.DefaultMcpServerName}";

            File.WriteAllText(tempConfigPath, $"[{sectionName}]\ncommand = \"C:\\\\Users\\\\test\\\\app.exe\"\n");

            var config = new TomlAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("command", "C:/Users/test/app.exe", requiredForConfiguration: true, comparison: ValueComparisonMode.Path);

            // Act & Assert
            Assert.IsTrue(config.IsConfigured(), "IsConfigured should return true when paths differ only in separator style");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_PathComparison_TrailingSlashIgnored()
        {
            // Arrange
            var bodyPath = "mcp_servers";
            var sectionName = $"{bodyPath}.{AiAgentConfig.DefaultMcpServerName}";

            File.WriteAllText(tempConfigPath, $"[{sectionName}]\ncommand = \"C:/Users/test/app/\"\n");

            var config = new TomlAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("command", "C:/Users/test/app", requiredForConfiguration: true, comparison: ValueComparisonMode.Path);

            // Act & Assert
            Assert.IsTrue(config.IsConfigured(), "IsConfigured should return true when paths differ only by trailing slash");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_UrlComparison_TrailingSlashIgnored()
        {
            // Arrange
            var bodyPath = "mcp_servers";
            var sectionName = $"{bodyPath}.{AiAgentConfig.DefaultMcpServerName}";

            File.WriteAllText(tempConfigPath, $"[{sectionName}]\nurl = \"http://localhost:5000/mcp/\"\n");

            var config = new TomlAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("url", "http://localhost:5000/mcp", requiredForConfiguration: true, comparison: ValueComparisonMode.Url);

            // Act & Assert
            Assert.IsTrue(config.IsConfigured(), "IsConfigured should return true when URLs differ only by trailing slash");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_UrlComparison_SchemeCaseInsensitive()
        {
            // Arrange
            var bodyPath = "mcp_servers";
            var sectionName = $"{bodyPath}.{AiAgentConfig.DefaultMcpServerName}";

            File.WriteAllText(tempConfigPath, $"[{sectionName}]\nurl = \"HTTP://LOCALHOST:5000/mcp\"\n");

            var config = new TomlAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("url", "http://localhost:5000/mcp", requiredForConfiguration: true, comparison: ValueComparisonMode.Url);

            // Act & Assert
            Assert.IsTrue(config.IsConfigured(), "IsConfigured should return true when URLs differ only in scheme/host casing");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_ExactComparison_RejectsDifferentPaths()
        {
            // Arrange - without ValueComparisonMode.Path, backslash vs forward slash should NOT match
            var bodyPath = "mcp_servers";
            var sectionName = $"{bodyPath}.{AiAgentConfig.DefaultMcpServerName}";

            File.WriteAllText(tempConfigPath, $"[{sectionName}]\ncommand = \"C:\\\\Users\\\\test\\\\app.exe\"\n");

            var config = new TomlAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("command", "C:/Users/test/app.exe", requiredForConfiguration: true);

            // Act & Assert
            Assert.IsFalse(config.IsConfigured(), "IsConfigured should return false with Exact comparison when paths have different separators");

            yield return null;
        }

        #endregion
    }
}
