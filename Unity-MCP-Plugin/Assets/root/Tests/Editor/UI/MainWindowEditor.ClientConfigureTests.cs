/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using System;
using System.Collections;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    using Consts = Common.Consts;

    public class MainWindowEditorClientConfigureTests : BaseTest
    {
        private MainWindowEditor mainWindowEditor;
        private string tempConfigPath;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            mainWindowEditor = ScriptableObject.CreateInstance<MainWindowEditor>();
            tempConfigPath = Path.GetTempFileName();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);

            if (mainWindowEditor != null)
                UnityEngine.Object.DestroyImmediate(mainWindowEditor);

            yield return base.TearDown();
        }

        [UnityTest]
        public IEnumerator ConfigureMcpClient_SimpleBodyPath_CreatesCorrectStructure()
        {
            // Arrange
            var bodyPath = "mcpServers";

            // Act
            var result = JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(result, "ConfigureMcpClient should return true");
            Assert.IsTrue(File.Exists(tempConfigPath), "Config file should be created");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.IsNotNull(rootObj["mcpServers"], "mcpServers should exist");

            var mcpServers = rootObj["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should be an object");
            Assert.Greater(mcpServers.Count, 0, "mcpServers should contain at least one server entry");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigureMcpClient_NestedBodyPath_CreatesCorrectStructure()
        {
            // Arrange
            var bodyPath = $"projects{Consts.MCP.Server.BodyPathDelimiter}myProject{Consts.MCP.Server.BodyPathDelimiter}mcpServers";

            // Act
            var result = JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(result, "ConfigureMcpClient should return true");
            Assert.IsTrue(File.Exists(tempConfigPath), "Config file should be created");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.IsNotNull(rootObj["projects"], "projects should exist");

            var projects = rootObj["projects"]?.AsObject();
            Assert.IsNotNull(projects, "projects should be an object");
            Assert.IsNotNull(projects["myProject"], "myProject should exist");

            var myProject = projects["myProject"]?.AsObject();
            Assert.IsNotNull(myProject, "myProject should be an object");
            Assert.IsNotNull(myProject["mcpServers"], "mcpServers should exist in myProject");

            var mcpServers = myProject["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should be an object");
            Assert.Greater(mcpServers.Count, 0, "mcpServers should contain at least one server entry");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigureMcpClient_ExistingFileSimpleStructure_PreservesContent()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var existingJson = @"{
                ""otherProperty"": ""shouldBePreserved"",
                ""mcpServers"": {
                    ""existingServer"": {
                        ""command"": ""other-command"",
                        ""args"": [""--arg1"", ""--arg2""]
                    }
                }
            }";
            File.WriteAllText(tempConfigPath, existingJson);

            // Act
            var result = JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(result, "ConfigureMcpClient should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.AreEqual("shouldBePreserved", rootObj["otherProperty"]?.GetValue<string>(), "Other properties should be preserved");

            var mcpServers = rootObj["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should exist");
            Assert.IsNotNull(mcpServers["existingServer"], "Existing server should be preserved");
            Assert.Greater(mcpServers.Count, 1, "Should have both existing and new server entries");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigureMcpClient_ExistingFileNestedStructure_PreservesContent()
        {
            // Arrange
            var bodyPath = $"projects{Consts.MCP.Server.BodyPathDelimiter}myProject{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            var existingJson = @"{
                ""globalProperty"": ""globalValue"",
                ""projects"": {
                    ""otherProject"": {
                        ""mcpServers"": {
                            ""otherProjectServer"": {
                                ""command"": ""other-project-command""
                            }
                        }
                    },
                    ""myProject"": {
                        ""projectProperty"": ""projectValue"",
                        ""mcpServers"": {
                            ""existingServer"": {
                                ""command"": ""existing-command""
                            }
                        }
                    }
                }
            }";
            File.WriteAllText(tempConfigPath, existingJson);

            // Act
            var result = JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(result, "ConfigureMcpClient should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.AreEqual("globalValue", rootObj["globalProperty"]?.GetValue<string>(), "Global properties should be preserved");

            var projects = rootObj["projects"]?.AsObject();
            Assert.IsNotNull(projects, "projects should exist");
            Assert.IsNotNull(projects["otherProject"], "Other project should be preserved");

            var myProject = projects["myProject"]?.AsObject();
            Assert.IsNotNull(myProject, "myProject should exist");
            Assert.AreEqual("projectValue", myProject["projectProperty"]?.GetValue<string>(), "Project properties should be preserved");

            var mcpServers = myProject["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should exist");
            Assert.IsNotNull(mcpServers["existingServer"], "Existing server should be preserved");
            Assert.Greater(mcpServers.Count, 1, "Should have both existing and new server entries");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigureMcpClient_ExistingFileWithDuplicateCommand_ReplacesEntry()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var duplicateCommand = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var existingJson = $@"{{
                ""mcpServers"": {{
                    ""Unity-MCP-Duplicate"": {{
                        ""command"": ""{duplicateCommand}"",
                        ""args"": [""--old-port=9999""],
                        ""type"": ""stdio""
                    }},
                    ""otherServer"": {{
                        ""command"": ""other-command"",
                        ""args"": [""--other-arg""]
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, existingJson);

            // Act
            var result = JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(result, "ConfigureMcpClient should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");

            var mcpServers = rootObj["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should exist");
            Assert.IsNotNull(mcpServers["otherServer"], "Other server should be preserved");

            // Check that the duplicate was replaced with new configuration
            var hasUnityMcpServer = false;
            foreach (var kv in mcpServers)
            {
                var command = kv.Value?["command"]?.GetValue<string>();
                if (command == duplicateCommand)
                {
                    hasUnityMcpServer = true;
                    var args = kv.Value?["args"]?.AsArray();
                    Assert.IsNotNull(args, "Args should exist for Unity-MCP server");

                    var portArg = args.ToString();
                    Assert.IsTrue(portArg.Contains($"--port={UnityMcpPlugin.Port}"),
                        $"Should contain current port, but got: {portArg}");
                    break;
                }
            }

            Assert.IsTrue(hasUnityMcpServer, "Should have Unity-MCP server with correct command");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigureMcpClient_EmptyExistingFile_CreatesNewStructure()
        {
            // Arrange
            var bodyPath = "mcpServers";
            File.WriteAllText(tempConfigPath, "{}");

            // Act
            var result = JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(result, "ConfigureMcpClient should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.IsNotNull(rootObj["mcpServers"], "mcpServers should be created");

            var mcpServers = rootObj["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should be an object");
            Assert.Greater(mcpServers.Count, 0, "mcpServers should contain at least one server entry");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigureMcpClient_InvalidJsonFile_ReplacesWithNewConfig()
        {
            // Arrange
            var bodyPath = "mcpServers";
            File.WriteAllText(tempConfigPath, "{ invalid json }");

            // Act
            var result = JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(result, "ConfigureMcpClient should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.IsNotNull(rootObj["mcpServers"], "mcpServers should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsMcpClientConfigured_SimpleBodyPath_DetectsCorrectly()
        {
            // Arrange
            var bodyPath = "mcpServers";
            JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Act
            var isConfigured = JsonClientConfig.IsMcpClientConfigured(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(isConfigured, "Should detect that client is configured");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsMcpClientConfigured_NestedBodyPath_DetectsCorrectly()
        {
            // Arrange
            var bodyPath = $"projects{Consts.MCP.Server.BodyPathDelimiter}myProject{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Act
            var isConfigured = JsonClientConfig.IsMcpClientConfigured(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(isConfigured, "Should detect that client is configured");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsMcpClientConfigured_NonExistentPath_ReturnsFalse()
        {
            // Arrange
            var bodyPath = $"nonExistent{Consts.MCP.Server.BodyPathDelimiter}path{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, "mcpServers");

            // Act
            var isConfigured = JsonClientConfig.IsMcpClientConfigured(tempConfigPath, bodyPath);

            // Assert
            Assert.IsFalse(isConfigured, "Should return false for non-existent path");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigureMcpClient_DeepNestedBodyPath_CreatesFullStructure()
        {
            // Arrange
            var bodyPath = $"level1{Consts.MCP.Server.BodyPathDelimiter}level2{Consts.MCP.Server.BodyPathDelimiter}level3{Consts.MCP.Server.BodyPathDelimiter}mcpServers";

            // Act
            var result = JsonClientConfig.ConfigureJsonMcpClient(tempConfigPath, bodyPath);

            // Assert
            Assert.IsTrue(result, "ConfigureMcpClient should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj, "Root object should not be null");
            Assert.IsNotNull(rootObj["level1"], "level1 should exist");

            var level1 = rootObj["level1"]?.AsObject();
            Assert.IsNotNull(level1, "level1 should be an object");
            Assert.IsNotNull(level1["level2"], "level2 should exist");

            var level2 = level1["level2"]?.AsObject();
            Assert.IsNotNull(level2, "level2 should be an object");
            Assert.IsNotNull(level2["level3"], "level3 should exist");

            var level3 = level2["level3"]?.AsObject();
            Assert.IsNotNull(level3, "level3 should be an object");
            Assert.IsNotNull(level3["mcpServers"], "mcpServers should exist");

            var mcpServers = level3["mcpServers"]?.AsObject();
            Assert.IsNotNull(mcpServers, "mcpServers should be an object");
            Assert.Greater(mcpServers.Count, 0, "mcpServers should contain at least one server entry");

            yield return null;
        }
    }
}