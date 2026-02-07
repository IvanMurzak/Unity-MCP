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

        private JsonAiAgentConfig CreateStdioConfig(string configPath, string bodyPath = "mcpServers")
        {
            return new JsonAiAgentConfig(
                name: "Test",
                configPath: configPath,
                bodyPath: bodyPath)
            .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
            .SetProperty("command", JsonValue.Create(Startup.Server.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true)
            .SetProperty("args", new JsonArray {
                $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
                $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
                $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
            }, requiredForConfiguration: true)
            .SetPropertyToRemove("url");
        }

        private JsonAiAgentConfig CreateHttpConfig(string configPath, string bodyPath = "mcpServers")
        {
            return new JsonAiAgentConfig(
                name: "Test",
                configPath: configPath,
                bodyPath: bodyPath)
            .SetProperty("type", JsonValue.Create($"{TransportMethod.streamableHttp}"), requiredForConfiguration: true)
            .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true)
            .SetPropertyToRemove("command")
            .SetPropertyToRemove("args");
        }

        #region Configure - Stdio Transport

        [UnityTest]
        public IEnumerator Configure_Stdio_CreatesCorrectFormat()
        {
            // Arrange
            var bodyPath = "mcpServers";
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

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
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var stdioConfig = CreateStdioConfig(tempConfigPath, bodyPath);
            stdioConfig.Configure();

            // Verify stdio properties exist
            var json1 = File.ReadAllText(tempConfigPath);
            var rootObj1 = JsonNode.Parse(json1)?.AsObject();
            var serverEntry1 = rootObj1!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry1!["command"], "command should exist after stdio configure");
            Assert.IsNotNull(serverEntry1["args"], "args should exist after stdio configure");

            // Act - configure with http
            var httpConfig = CreateHttpConfig(tempConfigPath, bodyPath);
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
            var httpConfig = CreateHttpConfig(tempConfigPath, bodyPath);
            httpConfig.Configure();

            // Verify http properties exist
            var json1 = File.ReadAllText(tempConfigPath);
            var rootObj1 = JsonNode.Parse(json1)?.AsObject();
            var serverEntry1 = rootObj1!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();
            Assert.IsNotNull(serverEntry1!["url"], "url should exist after http configure");

            // Act - configure with stdio
            var stdioConfig = CreateStdioConfig(tempConfigPath, bodyPath);
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
            var stdioConfig = CreateStdioConfig(tempConfigPath, bodyPath);
            stdioConfig.Configure();

            // Act - switch to http
            var httpConfig = CreateHttpConfig(tempConfigPath, bodyPath);
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
            var config = CreateStdioConfig(tempConfigPath, bodyPath);
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
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

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
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

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
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);
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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var httpConfig = CreateHttpConfig(tempConfigPath, bodyPath);
            httpConfig.Configure();

            // Check with stdio transport
            var stdioConfig = CreateStdioConfig(tempConfigPath, bodyPath);

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
            var stdioConfig = CreateStdioConfig(tempConfigPath, bodyPath);
            stdioConfig.Configure();

            // Check with streamableHttp transport
            var httpConfig = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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
            var stdioConfig = CreateStdioConfig(nonExistentPath);
            var httpConfig = CreateHttpConfig(nonExistentPath);

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
            var stdioConfig = CreateStdioConfig(tempConfigPath);
            var httpConfig = CreateHttpConfig(tempConfigPath);

            // Act & Assert
            Assert.IsFalse(stdioConfig.IsConfigured(), "stdio: Should return false for empty file");
            Assert.IsFalse(httpConfig.IsConfigured(), $"{TransportMethod.streamableHttp}: Should return false for empty file");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_EmptyConfigPath_ReturnsFalse()
        {
            // Arrange
            var stdioConfig = CreateStdioConfig("");
            var httpConfig = CreateHttpConfig("");

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
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

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

        #region IsConfigured - Scoped to DefaultMcpServerName

        [UnityTest]
        public IEnumerator IsConfigured_OtherServerMatches_ButDefaultMissing_ReturnsFalse()
        {
            // Arrange - another server entry has matching properties, but DefaultMcpServerName doesn't exist
            var bodyPath = "mcpServers";
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var json = $@"{{
                ""mcpServers"": {{
                    ""otherServer"": {{
                        ""type"": ""stdio"",
                        ""command"": ""{executable}"",
                        ""args"": [""{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}"", ""{Consts.MCP.Server.Args.ClientTransportMethod}=stdio""]
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, json);
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when only a different server name matches, not DefaultMcpServerName");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_OtherServerMatches_DefaultHasWrongValues_ReturnsFalse()
        {
            // Arrange - another server matches, but the DefaultMcpServerName entry has wrong values
            var bodyPath = "mcpServers";
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var json = $@"{{
                ""mcpServers"": {{
                    ""otherServer"": {{
                        ""type"": ""stdio"",
                        ""command"": ""{executable}"",
                        ""args"": [""{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}"", ""{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}"", ""{Consts.MCP.Server.Args.ClientTransportMethod}=stdio""]
                    }},
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""type"": ""stdio"",
                        ""command"": ""wrong-command"",
                        ""args"": [""wrong-arg""]
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, json);
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when DefaultMcpServerName has wrong values, even if another server matches");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_Http_OtherServerMatches_ButDefaultMissing_ReturnsFalse()
        {
            // Arrange - another server entry has matching http properties, but DefaultMcpServerName doesn't exist
            var bodyPath = "mcpServers";
            var url = UnityMcpPlugin.Host;
            var json = $@"{{
                ""mcpServers"": {{
                    ""otherServer"": {{
                        ""type"": ""{TransportMethod.streamableHttp}"",
                        ""url"": ""{url}""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, json);
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

            // Act
            var isConfigured = config.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured, "Should return false when only a different server name matches for http, not DefaultMcpServerName");

            yield return null;
        }

        #endregion

        #region Deterministic Property Order

        [UnityTest]
        public IEnumerator ExpectedFileContent_PropertiesInAlphabeticalOrder()
        {
            // Arrange - add properties in reverse alphabetical order
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcpServers")
            .SetProperty("zebra", JsonValue.Create("last"))
            .SetProperty("alpha", JsonValue.Create("first"))
            .SetProperty("middle", JsonValue.Create("mid"));

            // Act
            var content = config.ExpectedFileContent;
            var rootObj = JsonNode.Parse(content)?.AsObject();
            var serverEntry = rootObj!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();

            // Assert - properties should appear in sorted order
            var keys = new System.Collections.Generic.List<string>();
            foreach (var kv in serverEntry!)
                keys.Add(kv.Key);

            Assert.AreEqual("alpha", keys[0], "First property should be 'alpha'");
            Assert.AreEqual("middle", keys[1], "Second property should be 'middle'");
            Assert.AreEqual("zebra", keys[2], "Third property should be 'zebra'");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_PropertiesInAlphabeticalOrder()
        {
            // Arrange - add properties in reverse alphabetical order
            if (File.Exists(tempConfigPath))
                File.Delete(tempConfigPath);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcpServers")
            .SetProperty("zebra", JsonValue.Create("last"))
            .SetProperty("alpha", JsonValue.Create("first"))
            .SetProperty("middle", JsonValue.Create("mid"));

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(content)?.AsObject();
            var serverEntry = rootObj!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();

            var keys = new System.Collections.Generic.List<string>();
            foreach (var kv in serverEntry!)
                keys.Add(kv.Key);

            Assert.AreEqual("alpha", keys[0], "First property should be 'alpha'");
            Assert.AreEqual("middle", keys[1], "Second property should be 'middle'");
            Assert.AreEqual("zebra", keys[2], "Third property should be 'zebra'");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_ExistingFile_MergedPropertiesInAlphabeticalOrder()
        {
            // Arrange - existing file with a server entry, then configure with new properties in reverse order
            var existingJson = $@"{{
                ""mcpServers"": {{
                    ""{AiAgentConfig.DefaultMcpServerName}"": {{
                        ""existing"": ""value""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, existingJson);

            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: "mcpServers")
            .SetProperty("zebra", JsonValue.Create("last"))
            .SetProperty("alpha", JsonValue.Create("first"));

            // Act
            config.Configure();

            // Assert
            var content = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(content)?.AsObject();
            var serverEntry = rootObj!["mcpServers"]?[AiAgentConfig.DefaultMcpServerName]?.AsObject();

            var keys = new System.Collections.Generic.List<string>();
            foreach (var kv in serverEntry!)
                keys.Add(kv.Key);

            // "existing" was already there, new props should be sorted among themselves
            var alphaIdx = keys.IndexOf("alpha");
            var zebraIdx = keys.IndexOf("zebra");
            Assert.IsTrue(alphaIdx < zebraIdx, "alpha should appear before zebra in the output");

            yield return null;
        }

        #endregion

        #region Duplicate Server Entry Removal

        [UnityTest]
        public IEnumerator Configure_Stdio_RemovesDuplicateByCommand()
        {
            // Arrange - existing file with the same server under a custom name
            var bodyPath = "mcpServers";
            var executable = Startup.Server.ExecutableFullPath.Replace('\\', '/');
            var existingJson = $@"{{
                ""mcpServers"": {{
                    ""my-custom-name"": {{
                        ""type"": ""stdio"",
                        ""command"": ""{executable}"",
                        ""args"": [""--old-arg""]
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, existingJson);
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

            // Act
            config.Configure();

            // Assert
            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var mcpServers = rootObj!["mcpServers"]?.AsObject();

            Assert.IsNull(mcpServers!["my-custom-name"], "Duplicate entry with same command should be removed");
            Assert.IsNotNull(mcpServers[AiAgentConfig.DefaultMcpServerName], "Default entry should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_Http_RemovesDuplicateByUrl()
        {
            // Arrange - existing file with the same server under a custom name
            var bodyPath = "mcpServers";
            var url = UnityMcpPlugin.Host;
            var existingJson = $@"{{
                ""mcpServers"": {{
                    ""my-custom-name"": {{
                        ""type"": ""{TransportMethod.streamableHttp}"",
                        ""url"": ""{url}""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, existingJson);
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

            // Act
            config.Configure();

            // Assert
            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var mcpServers = rootObj!["mcpServers"]?.AsObject();

            Assert.IsNull(mcpServers!["my-custom-name"], "Duplicate entry with same url should be removed");
            Assert.IsNotNull(mcpServers[AiAgentConfig.DefaultMcpServerName], "Default entry should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_Http_RemovesDuplicateByServerUrl()
        {
            // Arrange - existing file with the same server under a custom name using serverUrl
            var bodyPath = "mcpServers";
            var url = UnityMcpPlugin.Host;
            var existingJson = $@"{{
                ""mcpServers"": {{
                    ""my-custom-name"": {{
                        ""serverUrl"": ""{url}""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, existingJson);
            var config = new JsonAiAgentConfig(
                name: "Test",
                configPath: tempConfigPath,
                bodyPath: bodyPath)
            .AddIdentityKey("serverUrl")
            .SetProperty("serverUrl", JsonValue.Create(url), requiredForConfiguration: true)
            .SetPropertyToRemove("command")
            .SetPropertyToRemove("args")
            .SetPropertyToRemove("url");

            // Act
            config.Configure();

            // Assert
            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var mcpServers = rootObj!["mcpServers"]?.AsObject();

            Assert.IsNull(mcpServers!["my-custom-name"], "Duplicate entry with same serverUrl should be removed");
            Assert.IsNotNull(mcpServers[AiAgentConfig.DefaultMcpServerName], "Default entry should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_Http_DefaultIdentityKeys_DoNotRemoveByServerUrl()
        {
            // Arrange - existing file with a server using serverUrl but config does NOT add serverUrl identity key
            var bodyPath = "mcpServers";
            var url = UnityMcpPlugin.Host;
            var existingJson = $@"{{
                ""mcpServers"": {{
                    ""my-custom-name"": {{
                        ""serverUrl"": ""{url}""
                    }}
                }}
            }}";
            File.WriteAllText(tempConfigPath, existingJson);
            var config = CreateHttpConfig(tempConfigPath, bodyPath);

            // Act
            config.Configure();

            // Assert - without adding serverUrl as identity key, the entry should be preserved
            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var mcpServers = rootObj!["mcpServers"]?.AsObject();

            Assert.IsNotNull(mcpServers!["my-custom-name"], "Entry with serverUrl should be preserved when serverUrl is not an identity key");
            Assert.IsNotNull(mcpServers[AiAgentConfig.DefaultMcpServerName], "Default entry should exist");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_PreservesUnrelatedServers()
        {
            // Arrange - existing file with a different server (different command)
            var bodyPath = "mcpServers";
            var existingJson = @"{
                ""mcpServers"": {
                    ""other-server"": {
                        ""type"": ""stdio"",
                        ""command"": ""completely-different-command"",
                        ""args"": [""--some-arg""]
                    }
                }
            }";
            File.WriteAllText(tempConfigPath, existingJson);
            var config = CreateStdioConfig(tempConfigPath, bodyPath);

            // Act
            config.Configure();

            // Assert
            var json = File.ReadAllText(tempConfigPath);
            var rootObj = JsonNode.Parse(json)?.AsObject();
            var mcpServers = rootObj!["mcpServers"]?.AsObject();

            Assert.IsNotNull(mcpServers!["other-server"], "Unrelated server should be preserved");
            Assert.IsNotNull(mcpServers[AiAgentConfig.DefaultMcpServerName], "Default entry should exist");

            yield return null;
        }

        #endregion

        #region ValueComparisonMode - Path and URL Normalization

        [UnityTest]
        public IEnumerator IsConfigured_PathComparison_BackslashEqualsForwardSlash()
        {
            // Arrange - write config with backslashes for the command path
            var bodyPath = "mcpServers";
            var backslashPath = "C:\\Users\\test\\app.exe";
            var forwardSlashPath = "C:/Users/test/app.exe";

            File.WriteAllText(tempConfigPath, $@"{{
  ""{bodyPath}"": {{
    ""{AiAgentConfig.DefaultMcpServerName}"": {{
      ""command"": ""{backslashPath.Replace("\\", "\\\\")}""
    }}
  }}
}}");

            var config = new JsonAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("command", JsonValue.Create(forwardSlashPath), requiredForConfiguration: true, comparison: ValueComparisonMode.Path);

            // Act & Assert
            Assert.IsTrue(config.IsConfigured(), "IsConfigured should return true when paths differ only in separator style");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_PathComparison_TrailingSlashIgnored()
        {
            // Arrange
            var bodyPath = "mcpServers";

            File.WriteAllText(tempConfigPath, $@"{{
  ""{bodyPath}"": {{
    ""{AiAgentConfig.DefaultMcpServerName}"": {{
      ""command"": ""C:/Users/test/app/""
    }}
  }}
}}");

            var config = new JsonAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("command", JsonValue.Create("C:/Users/test/app"), requiredForConfiguration: true, comparison: ValueComparisonMode.Path);

            // Act & Assert
            Assert.IsTrue(config.IsConfigured(), "IsConfigured should return true when paths differ only by trailing slash");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_UrlComparison_TrailingSlashIgnored()
        {
            // Arrange
            var bodyPath = "mcpServers";

            File.WriteAllText(tempConfigPath, $@"{{
  ""{bodyPath}"": {{
    ""{AiAgentConfig.DefaultMcpServerName}"": {{
      ""url"": ""http://localhost:5000/mcp/""
    }}
  }}
}}");

            var config = new JsonAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("url", JsonValue.Create("http://localhost:5000/mcp"), requiredForConfiguration: true, comparison: ValueComparisonMode.Url);

            // Act & Assert
            Assert.IsTrue(config.IsConfigured(), "IsConfigured should return true when URLs differ only by trailing slash");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_UrlComparison_SchemeCaseInsensitive()
        {
            // Arrange
            var bodyPath = "mcpServers";

            File.WriteAllText(tempConfigPath, $@"{{
  ""{bodyPath}"": {{
    ""{AiAgentConfig.DefaultMcpServerName}"": {{
      ""url"": ""HTTP://LOCALHOST:5000/mcp""
    }}
  }}
}}");

            var config = new JsonAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("url", JsonValue.Create("http://localhost:5000/mcp"), requiredForConfiguration: true, comparison: ValueComparisonMode.Url);

            // Act & Assert
            Assert.IsTrue(config.IsConfigured(), "IsConfigured should return true when URLs differ only in scheme/host casing");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConfigured_ExactComparison_RejectsDifferentPaths()
        {
            // Arrange - without ValueComparisonMode.Path, backslash vs forward slash should NOT match
            var bodyPath = "mcpServers";

            File.WriteAllText(tempConfigPath, $@"{{
  ""{bodyPath}"": {{
    ""{AiAgentConfig.DefaultMcpServerName}"": {{
      ""command"": ""C:\\Users\\test\\app.exe""
    }}
  }}
}}");

            var config = new JsonAiAgentConfig("Test", tempConfigPath, bodyPath)
                .SetProperty("command", JsonValue.Create("C:/Users/test/app.exe"), requiredForConfiguration: true);

            // Act & Assert
            Assert.IsFalse(config.IsConfigured(), "IsConfigured should return false with Exact comparison when paths have different separators");

            yield return null;
        }

        #endregion
    }
}
