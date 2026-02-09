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
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    using Consts = McpPlugin.Common.Consts;
    using TransportMethod = Consts.MCP.Server.TransportMethod;

    public class JsonAiAgentConfigCommandArrayTests : BaseTest
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

        private JsonAiAgentConfig CreateCommandArrayConfig(string configPath, string bodyPath = "mcpServers")
        {
            return new JsonAiAgentConfig(
                name: "Test",
                configPath: configPath,
                bodyPath: bodyPath)
            .SetProperty("type", JsonValue.Create("local"), requiredForConfiguration: true)
            .SetProperty("enabled", JsonValue.Create(true), requiredForConfiguration: true)
            .SetProperty("command", new JsonArray {
                McpServerManager.ExecutableFullPath.Replace('\\', '/'),
                $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
                $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
                $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
            }, requiredForConfiguration: true)
            .SetPropertyToRemove("url")
            .SetPropertyToRemove("args");
        }

        #region Configure - Command Array Format

        [UnityTest]
        public IEnumerator Configure_SimpleBodyPath_CreatesCommandArrayFormat()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");
            Assert.IsTrue(File.Exists(tempConfigPath), "Config file should be created");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.IsNotNull(rootObj!["mcpServers"], "mcpServers should exist");

            var mcpServers = rootObj["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should be an object");
            Assert.Greater(mcpServers!.Count, 0, "mcpServers should contain at least one server entry");

            // Verify command array format
            var serverEntry = mcpServers[AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry, "Server entry should exist");
            Assert.AreEqual("local", serverEntry!["type"]?.GetValue<string>(), "Type should be 'local'");
            Assert.AreEqual(true, serverEntry["enabled"]?.GetValue<bool>(), "Enabled should be true");

            var commandArray = serverEntry["command"]?.AsArray();
            Assert.IsNotNull(commandArray, "Command array should exist");
            Assert.Greater(commandArray!.Count, 0, "Command array should have elements");

            // First element should be executable path
            var executable = commandArray[0]?.GetValue<string>();
            Assert.IsNotNull(executable, "Executable should not be null");
            Assert.IsTrue(executable!.Contains("unity-mcp-server"), "First element should be the executable path");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_CommandArrayContainsAllRequiredArguments()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var serverEntry = rootObj!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();
            var commandArray = serverEntry!["command"]?.AsArray();

            Assert.IsNotNull(commandArray, "Command array should exist");

            // Check for required arguments in command array (starting from index 1, skip executable)
            var hasPortArg = false;
            var hasTimeoutArg = false;
            var hasTransportArg = false;

            for (int i = 1; i < commandArray!.Count; i++)
            {
                var arg = commandArray[i]?.GetValue<string>();
                if (arg?.StartsWith($"{Consts.MCP.Server.Args.Port}=") == true)
                    hasPortArg = true;
                if (arg?.StartsWith($"{Consts.MCP.Server.Args.PluginTimeout}=") == true)
                    hasTimeoutArg = true;
                if (arg?.Contains($"{Consts.MCP.Server.Args.ClientTransportMethod}=stdio") == true)
                    hasTransportArg = true;
            }

            Assert.IsTrue(hasPortArg, "Command array should contain port argument");
            Assert.IsTrue(hasTimeoutArg, "Command array should contain timeout argument");
            Assert.IsTrue(hasTransportArg, "Command array should contain transport argument");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_NestedBodyPath_CreatesCorrectStructure()
        {
            // Arrange
            var bodyPath = $"projects{Consts.MCP.Server.BodyPathDelimiter}myProject{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");
            Assert.IsTrue(File.Exists(tempConfigPath), "Config file should be created");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.IsNotNull(rootObj!["projects"], "projects should exist");

            var projects = rootObj["projects"]?.AsObject();
            Assert.IsNotNull(projects, "projects should be an object");
            Assert.IsNotNull(projects!["myProject"], "myProject should exist");

            var myProject = projects["myProject"]?.AsObject();
            Assert.IsNotNull(myProject, "myProject should be an object");
            Assert.IsNotNull(myProject!["mcpServers"], "mcpServers should exist in myProject");

            var mcpServers = myProject["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should be an object");

            // Verify command array format in nested structure
            var serverEntry = mcpServers![AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry, "Server entry should exist");
            Assert.IsNotNull(serverEntry!["command"]?.AsArray(), "Command array should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_DeepNestedBodyPath_CreatesFullStructure()
        {
            // Arrange
            var bodyPath = $"level1{Consts.MCP.Server.BodyPathDelimiter}level2{Consts.MCP.Server.BodyPathDelimiter}level3{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj!["level1"], "level1 should exist");

            var level1 = rootObj["level1"]?.AsObject();
            Assert.IsNotNull(level1!["level2"], "level2 should exist");

            var level2 = level1["level2"]?.AsObject();
            Assert.IsNotNull(level2!["level3"], "level3 should exist");

            var level3 = level2["level3"]?.AsObject();
            Assert.IsNotNull(level3!["mcpServers"], "mcpServers should exist");

            var mcpServers = level3["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers![AiAgentConfig.DefaultMcpServerName]?["command"], "Command should exist in deep nested structure");

            yield return null;
        }

        #endregion

        #region Configure - Existing File Handling

        [UnityTest]
        public IEnumerator Configure_ExistingFileSimpleStructure_PreservesContent()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var existingJson = @"{
                ""otherProperty"": ""shouldBePreserved"",
                ""mcpServers"": {
                    ""existingServer"": {
                        ""command"": [""other-command"", ""--arg1""],
                        ""type"": ""local""
                    }
                }
            }";
            File.WriteAllText(tempConfigPath, existingJson);
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.AreEqual("shouldBePreserved", rootObj!["otherProperty"]?.GetValue<string>(), "Other properties should be preserved");

            var mcpServers = rootObj["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should exist");
            Assert.IsNotNull(mcpServers!["existingServer"], "Existing server should be preserved");
            Assert.Greater(mcpServers.Count, 1, "Should have both existing and new server entries");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingFileNestedStructure_PreservesContent()
        {
            // Arrange
            var bodyPath = $"projects{Consts.MCP.Server.BodyPathDelimiter}myProject{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            var existingJson = @"{
                ""globalProperty"": ""globalValue"",
                ""projects"": {
                    ""otherProject"": {
                        ""mcpServers"": {
                            ""otherProjectServer"": {
                                ""command"": [""other-project-command""]
                            }
                        }
                    },
                    ""myProject"": {
                        ""projectProperty"": ""projectValue"",
                        ""mcpServers"": {
                            ""existingServer"": {
                                ""command"": [""existing-command""]
                            }
                        }
                    }
                }
            }";
            File.WriteAllText(tempConfigPath, existingJson);
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.AreEqual("globalValue", rootObj!["globalProperty"]?.GetValue<string>(), "Global properties should be preserved");

            var projects = rootObj["projects"]?.AsObject();
            Assert.IsNotNull(projects, "projects should exist");
            Assert.IsNotNull(projects!["otherProject"], "Other project should be preserved");

            var myProject = projects["myProject"]?.AsObject();
            Assert.IsNotNull(myProject, "myProject should exist");
            Assert.AreEqual("projectValue", myProject!["projectProperty"]?.GetValue<string>(), "Project properties should be preserved");

            var mcpServers = myProject["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should exist");
            Assert.IsNotNull(mcpServers!["existingServer"], "Existing server should be preserved");
            Assert.Greater(mcpServers.Count, 1, "Should have both existing and new server entries");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingFileWithDuplicateCommand_ReplacesEntry()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var duplicateCommand = McpServerManager.ExecutableFullPath.Replace('\\', '/');
            var existingJson = $@"{{
                ""mcpServers"": {{
                    ""Unity-MCP-Duplicate"": {{
                        ""command"": [""{duplicateCommand}"", ""--old-port=9999""],
                        ""type"": ""local"",
                        ""enabled"": true
                    }},
                    ""otherServer"": {{
                        ""command"": [""other-command"", ""--other-arg""]
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, existingJson);
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");

            var mcpServers = rootObj!["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should exist");
            Assert.IsNotNull(mcpServers!["otherServer"], "Other server should be preserved");

            // Check that our server entry exists with the correct configuration
            var serverEntry = mcpServers[AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry, "Server entry should exist under DefaultMcpServerName");

            var commandArray = serverEntry!["command"]?.AsArray();
            Assert.IsNotNull(commandArray, "Command array should exist");
            Assert.Greater(commandArray!.Count, 0, "Command array should have elements");

            var executable = commandArray[0]?.GetValue<string>();
            Assert.AreEqual(duplicateCommand, executable, "Executable should match");

            var argsStr = commandArray.ToString();
            Assert.IsTrue(argsStr.Contains($"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"),
                $"Should contain current port, but got: {argsStr}");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_EmptyExistingFile_CreatesNewStructure()
        {
            // Arrange
            var bodyPath = "mcpServers";
            File.WriteAllText(tempConfigPath, "{}");
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.IsNotNull(rootObj!["mcpServers"], "mcpServers should be created");

            var mcpServers = rootObj["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should be an object");
            Assert.Greater(mcpServers!.Count, 0, "mcpServers should contain at least one server entry");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_InvalidJsonFile_ReplacesWithNewConfig()
        {
            // Arrange
            var bodyPath = "mcpServers";
            File.WriteAllText(tempConfigPath, "{ invalid json }");
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.IsNotNull(rootObj!["mcpServers"], "mcpServers should exist");

            yield return null;
        }

        #endregion

        #region IsConfigured - Detection Tests

        [UnityTest]
        public IEnumerator IsConfigured_SimpleBodyPath_DetectsCorrectly()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);
            config.Configure();

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should detect that client is configured");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_NestedBodyPath_DetectsCorrectly()
        {
            // Arrange
            var bodyPath = $"projects{Consts.MCP.Server.BodyPathDelimiter}myProject{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);
            config.Configure();

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should detect that client is configured");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_NonExistentPath_ReturnsFalse()
        {
            // Arrange
            var configuredBodyPath = "mcpServers";
            var queryBodyPath = $"nonExistent{Consts.MCP.Server.BodyPathDelimiter}path{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            var configureConfig = CreateCommandArrayConfig(tempConfigPath, configuredBodyPath);
            configureConfig.Configure();

            var queryConfig = CreateCommandArrayConfig(tempConfigPath, queryBodyPath);

            // Act
            var isConfigured = queryConfig.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false for non-existent path");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "non_existent_config.json");
            var config = CreateCommandArrayConfig(nonExistentPath);

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
            var config = CreateCommandArrayConfig(tempConfigPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false for empty file");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_WrongType_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var executable = McpServerManager.ExecutableFullPath.Replace('\\', '/');
            var wrongTypeJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""command"": [""{executable}"", ""{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}"", ""{Consts.MCP.Server.Args.ClientTransportMethod}=stdio""],
                        ""type"": ""wrong-type"",
                        ""enabled"": true
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, wrongTypeJson);
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when type doesn't match");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_WrongCommandArray_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var wrongCommandJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""command"": [""wrong-executable"", ""{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}""],
                        ""type"": ""local"",
                        ""enabled"": true
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, wrongCommandJson);
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when command array doesn't match");

            yield return null;
        }

        #endregion

        #region ExpectedFileContent Tests

        [UnityTest]
        public IEnumerator ExpectedFileContent_SimpleBodyPath_ReturnsCorrectFormat()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var content = config.ExpectedFileContent;

            // Assert
            var rootObj = JsonNode.Parse(content)?.AsObject();
            Assert.IsNotNull(rootObj, "ExpectedFileContent should be valid JSON");
            Assert.IsNotNull(rootObj!["mcpServers"], "Should contain mcpServers");

            var mcpServers = rootObj["mcpServers"]?.AsObject();
            var serverEntry = mcpServers![AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry, "Should contain server entry");
            Assert.IsNotNull(serverEntry!["command"]?.AsArray(), "Should contain command array");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpectedFileContent_NestedBodyPath_ReturnsCorrectNestedStructure()
        {
            // Arrange
            var bodyPath = $"level1{Consts.MCP.Server.BodyPathDelimiter}level2{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var content = config.ExpectedFileContent;

            // Assert
            var rootObj = JsonNode.Parse(content)?.AsObject();
            Assert.IsNotNull(rootObj, "ExpectedFileContent should be valid JSON");
            Assert.IsNotNull(rootObj!["level1"], "Should contain level1");

            var level1 = rootObj["level1"]?.AsObject();
            Assert.IsNotNull(level1!["level2"], "Should contain level2");

            var level2 = level1["level2"]?.AsObject();
            Assert.IsNotNull(level2!["mcpServers"], "Should contain mcpServers");

            yield return null;
        }

        #endregion

        #region Edge Cases

        [UnityTest]
        public IEnumerator Configure_EmptyBodyPath_HandlesGracefully()
        {
            // Arrange - using default body path
            var bodyPath = Consts.MCP.Server.DefaultBodyPath;
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should handle default body path");
            Assert.IsTrue(config.IsConfigured(), "Should be configured after Configure call");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_EmptyConfigPath_ReturnsFalse()
        {
            // Arrange
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: "",
                bodyPath: "mcpServers")
            .SetProperty("type", JsonValue.Create("local"), requiredForConfiguration: true)
            .SetProperty("enabled", JsonValue.Create(true), requiredForConfiguration: true)
            .SetProperty("command", new JsonArray {
                McpServerManager.ExecutableFullPath.Replace('\\', '/'),
                $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
                $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
                $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
            }, requiredForConfiguration: true);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsFalse(result, "Configure should return false for empty config path");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_MultipleCalls_UpdatesConfiguration()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = CreateCommandArrayConfig(tempConfigPath, bodyPath);

            // Act - configure twice
            var result1 = config.Configure();
            var result2 = config.Configure();

            // Assert
            Assert.IsTrue(result1, "First configure should return true");
            Assert.IsTrue(result2, "Second configure should return true");
            Assert.IsTrue(config.IsConfigured(), "Should be configured after multiple calls");

            // Verify there's only one server entry under DefaultMcpServerName (not duplicated)
            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var mcpServers = rootObj!["mcpServers"]?.AsObject();

            Assert.AreEqual(1, mcpServers!.Count, "Should have exactly one server entry after multiple configures");
            Assert.IsNotNull(mcpServers[AiAgentConfig.DefaultMcpServerName], "Server entry should exist under DefaultMcpServerName");

            yield return null;
        }

        #endregion
    }
}
