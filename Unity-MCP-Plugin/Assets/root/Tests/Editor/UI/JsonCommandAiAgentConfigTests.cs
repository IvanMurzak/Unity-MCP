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
using System.Text.RegularExpressions;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    using Consts = McpPlugin.Common.Consts;

    public class JsonCommandAiAgentConfigTests : BaseTest
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

        #region Configure - Command Array Format

        [UnityTest]
        public IEnumerator Configure_SimpleBodyPath_CreatesCommandArrayFormat()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var duplicateCommand = Startup.Server.ExecutableFullPath.Replace('\\', '/');
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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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

            // Check that the duplicate was replaced with new configuration
            var hasUnityMcpServer = false;
            foreach (var kv in mcpServers)
            {
                var commandArray = kv.Value?["command"]?.AsArray();
                if (commandArray == null || commandArray.Count == 0)
                    continue;

                var command = commandArray[0]?.GetValue<string>();
                if (command == duplicateCommand)
                {
                    hasUnityMcpServer = true;

                    var portArgFound = false;
                    for (int i = 1; i < commandArray.Count; i++)
                    {
                        var arg = commandArray[i]?.GetValue<string>();
                        if (arg?.Contains($"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}") == true)
                        {
                            portArgFound = true;
                            break;
                        }
                    }

                    Assert.IsTrue(portArgFound, "Should contain current port argument");
                    break;
                }
            }

            Assert.IsTrue(hasUnityMcpServer, "Should have Unity-MCP server with correct command");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_EmptyExistingFile_CreatesNewStructure()
        {
            // Arrange
            var bodyPath = "mcpServers";
            File.WriteAllText(tempConfigPath, "{}");
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);
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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);
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
            var configureConfig = new JsonCommandAiAgentConfig("Test", tempConfigPath, configuredBodyPath);
            configureConfig.Configure();

            var queryConfig = new JsonCommandAiAgentConfig("Test", tempConfigPath, queryBodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", nonExistentPath, "mcpServers");

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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, "mcpServers");

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false for empty file");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_WrongExecutable_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var wrongExecutableJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""command"": [""wrong-executable"", ""{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}""],
                        ""type"": ""local"",
                        ""enabled"": true
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, wrongExecutableJson);
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when executable doesn't match");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_MissingPortArgument_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var missingPortJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""command"": [""{executable}"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}""],
                        ""type"": ""local"",
                        ""enabled"": true
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, missingPortJson);
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when port argument is missing");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_MissingTimeoutArgument_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var missingTimeoutJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""command"": [""{executable}"", ""{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}""],
                        ""type"": ""local"",
                        ""enabled"": true
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, missingTimeoutJson);
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when timeout argument is missing");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_WrongPortValue_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var wrongPortJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""command"": [""{executable}"", ""{Consts.MCP.Server.Args.Port}=99999"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}""],
                        ""type"": ""local"",
                        ""enabled"": true
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, wrongPortJson);
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when port value doesn't match");

            yield return null;
        }

        #endregion

        #region ExpectedFileContent Tests

        [UnityTest]
        public IEnumerator ExpectedFileContent_SimpleBodyPath_ReturnsCorrectFormat()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            // Arrange - using default body path (empty string would use DefaultBodyPath)
            var bodyPath = Consts.MCP.Server.DefaultBodyPath;
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

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
            var config = new JsonCommandAiAgentConfig("Test", "", "mcpServers");

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsFalse(result, "Configure should return false for empty config path");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_TraditionalArgsFormat_ReturnsFalse()
        {
            // Arrange - write traditional format (command + args separate)
            var bodyPath = "mcpServers";
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var traditionalFormatJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""command"": ""{executable}"",
                        ""args"": [""{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}""],
                        ""type"": ""stdio""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, traditionalFormatJson);
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

            // Expect error and exception logs when trying to parse command as array (it's a string in traditional format)
            LogAssert.Expect(LogType.Error, new Regex("Error reading config file.*"));
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException.*"));

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            // Traditional format should NOT be detected as configured by JsonCommandAiAgentConfig
            // because it expects command array format
            Assert.IsFalse(isConfigured, "Should return false for traditional args format (command as string, not array)");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_MultipleCalls_UpdatesConfiguration()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonCommandAiAgentConfig("Test", tempConfigPath, bodyPath);

            // Act - configure twice
            var result1 = config.Configure();
            var result2 = config.Configure();

            // Assert
            Assert.IsTrue(result1, "First configure should return true");
            Assert.IsTrue(result2, "Second configure should return true");
            Assert.IsTrue(config.IsConfigured(), "Should be configured after multiple calls");

            // Verify there's only one server entry (not duplicated)
            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var mcpServers = rootObj!["mcpServers"]?.AsObject();

            var matchingServerCount = 0;
            foreach (var kv in mcpServers!)
            {
                var commandArray = kv.Value?["command"]?.AsArray();
                if (commandArray != null && commandArray.Count > 0)
                {
                    var executable = commandArray[0]?.GetValue<string>();
                    if (executable?.Contains("unity-mcp-server") == true)
                        matchingServerCount++;
                }
            }

            Assert.AreEqual(1, matchingServerCount, "Should have exactly one Unity-MCP server entry after multiple configures");

            yield return null;
        }

        #endregion
    }
}
