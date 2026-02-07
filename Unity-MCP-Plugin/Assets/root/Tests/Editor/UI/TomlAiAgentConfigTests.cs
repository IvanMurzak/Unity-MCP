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
            .SetProperty("command", Startup.Server.ExecutableFullPath.Replace('\\', '/'), requiredForConfiguration: true)
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
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
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
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
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
    }
}
