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

    public class JsonAiAgentConfigTests : BaseTest
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

        #region Configure - Stdio Transport

        [UnityTest]
        public IEnumerator Configure_Stdio_CreatesCorrectFormat()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);

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

            var serverEntry = mcpServers![AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry, "Server entry should exist");

            // Verify stdio properties exist
            Assert.IsNotNull(serverEntry!["command"], "command should exist for stdio");
            Assert.IsNotNull(serverEntry["args"], "args should exist for stdio");

            // Verify http properties do NOT exist
            Assert.IsNull(serverEntry["url"], "url should NOT exist for stdio");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_Stdio_ContainsCorrectArguments()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var serverEntry = rootObj!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();

            var command = serverEntry!["command"]?.GetValue<string>();
            Assert.IsNotNull(command, "command should not be null");
            Assert.IsTrue(command!.Contains("unity-mcp-server"), "command should contain executable name");

            var args = serverEntry["args"]?.AsArray();
            Assert.IsNotNull(args, "args should not be null");

            var hasPortArg = false;
            var hasTimeoutArg = false;

            foreach (var arg in args!)
            {
                var argStr = arg?.GetValue<string>();
                if (argStr?.StartsWith($"{Consts.MCP.Server.Args.Port}=") == true)
                    hasPortArg = true;
                if (argStr?.StartsWith($"{Consts.MCP.Server.Args.PluginTimeout}=") == true)
                    hasTimeoutArg = true;
            }

            Assert.IsTrue(hasPortArg, "args should contain port argument");
            Assert.IsTrue(hasTimeoutArg, "args should contain timeout argument");

            yield return null;
        }

        #endregion

        #region Configure - http Transport

        [UnityTest]
        public IEnumerator Configure_Http_CreatesCorrectFormat()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

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

            var serverEntry = mcpServers![AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry, "Server entry should exist");

            // Verify http properties exist
            Assert.IsNotNull(serverEntry!["url"], "url should exist for http");
            Assert.AreEqual($"{TransportMethod.streamableHttp}", serverEntry["type"]?.GetValue<string>(), $"type should be '{TransportMethod.streamableHttp}'");

            // Verify stdio properties do NOT exist
            Assert.IsNull(serverEntry["command"], "command should NOT exist for http");
            Assert.IsNull(serverEntry["args"], "args should NOT exist for http");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_Http_ContainsCorrectUrl()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var serverEntry = rootObj!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();

            var url = serverEntry!["url"]?.GetValue<string>();
            Assert.IsNotNull(url, "url should not be null");
            Assert.AreEqual(UnityMcpPlugin.Host, url, "url should match McpServerUrl");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_Http_NestedBodyPath_CreatesCorrectStructure()
        {
            // Arrange
            var bodyPath = $"projects{Consts.MCP.Server.BodyPathDelimiter}myProject{Consts.MCP.Server.BodyPathDelimiter}mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

            // Act
            var result = config.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();

            Assert.IsNotNull(rootObj!["projects"], "projects should exist");
            var projects = rootObj["projects"]?.AsObject();
            Assert.IsNotNull(projects!["myProject"], "myProject should exist");
            var myProject = projects["myProject"]?.AsObject();
            Assert.IsNotNull(myProject!["mcpServers"], "mcpServers should exist");

            var mcpServers = myProject["mcpServers"]?.AsObject();
            var serverEntry = mcpServers![AiAgentConfig.DefaultMcpServerName]?.AsObject();

            Assert.IsNotNull(serverEntry!["url"], "url should exist in nested structure");
            Assert.AreEqual($"{TransportMethod.streamableHttp}", serverEntry["type"]?.GetValue<string>());

            yield return null;
        }

        #endregion

        #region Configure - Transport Switching

        [UnityTest]
        public IEnumerator Configure_SwitchFromStdioToHttp_RemovesStdioProperties()
        {
            // Arrange - first configure with stdio
            var bodyPath = "mcpServers";
            var stdioConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);
            stdioConfig.Configure();

            // Verify stdio properties exist
            var json1 = File.ReadAllText(tempConfigPath);
            var rootObj1 = JsonNode.Parse(json1)?.AsObject();
            var serverEntry1 = rootObj1!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry1!["command"], "command should exist after stdio configure");
            Assert.IsNotNull(serverEntry1["args"], "args should exist after stdio configure");

            // Act - configure with http
            var httpConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);
            var result = httpConfig.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json2 = File.ReadAllText(tempConfigPath);
            var rootObj2 = JsonNode.Parse(json2)?.AsObject();
            var serverEntry2 = rootObj2!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();

            // http properties should exist
            Assert.IsNotNull(serverEntry2!["url"], "url should exist after http configure");
            Assert.AreEqual($"{TransportMethod.streamableHttp}", serverEntry2["type"]?.GetValue<string>());

            // stdio properties should NOT exist
            Assert.IsNull(serverEntry2["command"], "command should NOT exist after switching to http");
            Assert.IsNull(serverEntry2["args"], "args should NOT exist after switching to http");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_SwitchFromHttpToStdio_RemovesHttpProperties()
        {
            // Arrange - first configure with http
            var bodyPath = "mcpServers";
            var httpConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);
            httpConfig.Configure();

            // Verify http properties exist
            var json1 = File.ReadAllText(tempConfigPath);
            var rootObj1 = JsonNode.Parse(json1)?.AsObject();
            var serverEntry1 = rootObj1!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry1!["url"], "url should exist after http configure");

            // Act - configure with stdio
            var stdioConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);
            var result = stdioConfig.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json2 = File.ReadAllText(tempConfigPath);
            var rootObj2 = JsonNode.Parse(json2)?.AsObject();
            var serverEntry2 = rootObj2!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();

            // stdio properties should exist
            Assert.IsNotNull(serverEntry2!["command"], "command should exist after stdio configure");
            Assert.IsNotNull(serverEntry2["args"], "args should exist after stdio configure");

            // http properties should NOT exist
            Assert.IsNull(serverEntry2["url"], "url should NOT exist after switching to stdio");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_SwitchTransport_PreservesOtherServers()
        {
            // Arrange - create file with other servers
            var bodyPath = "mcpServers";
            var existingJson = @"{
                ""mcpServers"": {
                    ""otherServer"": {
                        ""command"": ""other-command"",
                        ""args"": [""--other-arg""]
                    }
                }
            }";
            File.WriteAllText(tempConfigPath, existingJson);

            // Configure with stdio first
            var stdioConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);
            stdioConfig.Configure();

            // Act - switch to http
            var httpConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);
            var result = httpConfig.Configure();

            // Assert
            Assert.IsTrue(result, "Configure should return true");

            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var mcpServers = rootObj!["mcpServers"]?.AsObject();

            // Other server should be preserved
            Assert.IsNotNull(mcpServers!["otherServer"], "Other server should be preserved");

            // Our server should have http config
            var serverEntry = mcpServers[AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry!["url"], "url should exist");

            yield return null;
        }

        #endregion

        #region IsConfigured - Stdio Transport

        [UnityTest]
        public IEnumerator IsConfigured_Stdio_ValidConfig_ReturnsTrue()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);
            config.Configure();

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should detect that client is configured");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_Stdio_WithUrlProperty_ReturnsFalse()
        {
            // Arrange - create config with both stdio and url properties (invalid state)
            var bodyPath = "mcpServers";
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var mixedJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""command"": ""{executable}"",
                        ""args"": [""{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}""],
                        ""url"": ""http://localhost:50000/mcp""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, mixedJson);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when url property exists for stdio transport");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_Stdio_MissingCommand_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var missingCommandJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""args"": [""{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}""]
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, missingCommandJson);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when command is missing");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_Stdio_WrongPort_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var wrongPortJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""command"": ""{executable}"",
                        ""args"": [""{Consts.MCP.Server.Args.Port}=99999"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}""]
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, wrongPortJson);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when port doesn't match");

            yield return null;
        }

        #endregion

        #region IsConfigured - Http Transport

        [UnityTest]
        public IEnumerator IsConfigured_Http_ValidConfig_ReturnsTrue()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);
            config.Configure();

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsTrue(isConfigured, "Should detect that client is configured");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_Http_WithCommandProperty_ReturnsFalse()
        {
            // Arrange - create config with both http and command properties (invalid state)
            var bodyPath = "mcpServers";
            var url = UnityMcpPlugin.Host;
            var mixedJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""url"": ""{url}"",
                        ""type"": ""{TransportMethod.streamableHttp}"",
                        ""command"": ""some-command""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, mixedJson);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when command property exists for http transport");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_Http_WithArgsProperty_ReturnsFalse()
        {
            // Arrange - create config with both http and args properties (invalid state)
            var bodyPath = "mcpServers";
            var url = UnityMcpPlugin.Host;
            var mixedJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""url"": ""{url}"",
                        ""type"": ""{TransportMethod.streamableHttp}"",
                        ""args"": [""--some-arg""]
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, mixedJson);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when args property exists for streamableHttp transport");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_Http_MissingUrl_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var missingUrlJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""type"": ""{TransportMethod.streamableHttp}""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, missingUrlJson);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when url is missing");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_Http_WrongType_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var url = UnityMcpPlugin.Host;
            var wrongTypeJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""url"": ""{url}"",
                        ""type"": ""stdio""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, wrongTypeJson);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, $"Should return false when type is not '{TransportMethod.streamableHttp}'");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_Http_WrongUrl_ReturnsFalse()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var wrongUrlJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""url"": ""http://localhost:99999/wrong-path"",
                        ""type"": ""{TransportMethod.streamableHttp}""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, wrongUrlJson);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when url doesn't match");

            yield return null;
        }

        #endregion

        #region IsConfigured - Cross Transport Validation

        [UnityTest]
        public IEnumerator IsConfigured_StdioTransport_WithHttpConfig_ReturnsFalse()
        {
            // Arrange - configure with http
            var bodyPath = "mcpServers";
            var httpConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);
            httpConfig.Configure();

            // Check with stdio transport
            var stdioConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = stdioConfig.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "stdio transport should return false when config has streamableHttp format");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_HttpTransport_WithStdioConfig_ReturnsFalse()
        {
            // Arrange - configure with stdio
            var bodyPath = "mcpServers";
            var stdioConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);
            stdioConfig.Configure();

            // Check with streamableHttp transport
            var httpConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

            // Act
            var isConfigured = httpConfig.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "streamableHttp transport should return false when config has stdio format");

            yield return null;
        }

        #endregion

        #region ExpectedFileContent Tests

        [UnityTest]
        public IEnumerator ExpectedFileContent_Stdio_ReturnsCorrectFormat()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: bodyPath);

            // Act
            var content = config.ExpectedFileContent;

            // Assert
            var rootObj = JsonNode.Parse(content)?.AsObject();
            Assert.IsNotNull(rootObj, "ExpectedFileContent should be valid JSON");

            var mcpServers = rootObj!["mcpServers"]?.AsObject();
            var serverEntry = mcpServers![AiAgentConfig.DefaultMcpServerName]?.AsObject();

            Assert.IsNotNull(serverEntry!["command"], "Should contain command");
            Assert.IsNotNull(serverEntry["args"], "Should contain args");
            Assert.IsNull(serverEntry["url"], "Should NOT contain url");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpectedFileContent_Http_ReturnsCorrectFormat()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

            // Act
            var content = config.ExpectedFileContent;

            // Assert
            var rootObj = JsonNode.Parse(content)?.AsObject();
            Assert.IsNotNull(rootObj, "ExpectedFileContent should be valid JSON");

            var mcpServers = rootObj!["mcpServers"]?.AsObject();
            var serverEntry = mcpServers![AiAgentConfig.DefaultMcpServerName]?.AsObject();

            Assert.IsNotNull(serverEntry!["url"], "Should contain url");
            Assert.AreEqual($"{TransportMethod.streamableHttp}", serverEntry["type"]?.GetValue<string>(), "Should have correct type");
            Assert.IsNull(serverEntry["command"], "Should NOT contain command");
            Assert.IsNull(serverEntry["args"], "Should NOT contain args");

            yield return null;
        }

        #endregion

        #region Edge Cases

        [UnityTest]
        public IEnumerator IsConfigured_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "non_existent_config_12345.json");
            var stdioConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: nonExistentPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: "mcpServers");
            var httpConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: nonExistentPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: "mcpServers");

            // Act & Assert
            Assert.IsFalse(stdioConfig.IsConfigured(), "stdio: Should return false for non-existent file");
            Assert.IsFalse(httpConfig.IsConfigured(), $"{TransportMethod.streamableHttp}: Should return false for non-existent file");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_EmptyFile_ReturnsFalse()
        {
            // Arrange
            File.WriteAllText(tempConfigPath, "");
            var stdioConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: "mcpServers");
            var httpConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: "mcpServers");

            // Act & Assert
            Assert.IsFalse(stdioConfig.IsConfigured(), "stdio: Should return false for empty file");
            Assert.IsFalse(httpConfig.IsConfigured(), $"{TransportMethod.streamableHttp}: Should return false for empty file");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_EmptyConfigPath_ReturnsFalse()
        {
            // Arrange
            var stdioConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: "",
                transportMethod: TransportMethod.stdio,
                transportMethodValue: $"{TransportMethod.stdio}",
                bodyPath: "mcpServers");
            var httpConfig = new JsonAiAgentConfig(
                name: "Test",
                configPath: "",
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: "mcpServers");

            // Act & Assert
            Assert.IsFalse(stdioConfig.Configure(), "stdio: Configure should return false for empty config path");
            Assert.IsFalse(httpConfig.Configure(), $"{TransportMethod.streamableHttp}: Configure should return false for empty config path");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_MultipleCalls_SameTransport_UpdatesConfiguration()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                transportMethod: TransportMethod.streamableHttp,
                transportMethodValue: $"{TransportMethod.streamableHttp}",
                bodyPath: bodyPath);

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
                var url = kv.Value?["url"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(url))
                    matchingServerCount++;
            }

            Assert.AreEqual(1, matchingServerCount, "Should have exactly one server entry with url after multiple configures");

            yield return null;
        }

        #endregion
    }
}
