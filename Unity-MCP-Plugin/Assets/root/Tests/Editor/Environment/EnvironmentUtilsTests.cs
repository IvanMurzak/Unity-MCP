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
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class EnvironmentUtilsTests
    {
        [TearDown]
        public void TearDown()
        {
            System.Environment.SetEnvironmentVariable(EnvironmentUtils.McpServerUrlEnvVar, null);
        }

        [Test]
        public void McpServerUrlEnvVar_ShouldHaveCorrectValue()
        {
            Assert.AreEqual("UNITY_MCP_SERVER_URL", EnvironmentUtils.McpServerUrlEnvVar);
        }

        [Test]
        public void GetMcpServerUrl_WhenEnvVarNotSet_ShouldReturnNull()
        {
            System.Environment.SetEnvironmentVariable(EnvironmentUtils.McpServerUrlEnvVar, null);

            var result = EnvironmentUtils.GetMcpServerUrl();

            Assert.IsNull(result, "GetMcpServerUrl should return null when env var is not set");
        }

        [Test]
        public void GetMcpServerUrl_WhenEnvVarIsSet_ShouldReturnUrl()
        {
            const string expectedUrl = "http://localhost:8080";
            System.Environment.SetEnvironmentVariable(EnvironmentUtils.McpServerUrlEnvVar, expectedUrl);

            var result = EnvironmentUtils.GetMcpServerUrl();

            Assert.AreEqual(expectedUrl, result, "GetMcpServerUrl should return the URL from the env var");
        }

        [Test]
        public void GetMcpServerUrl_WhenEnvVarSetToPortUrl_ShouldReturnPortUrl()
        {
            const string expectedUrl = "http://localhost:55123";
            System.Environment.SetEnvironmentVariable(EnvironmentUtils.McpServerUrlEnvVar, expectedUrl);

            var result = EnvironmentUtils.GetMcpServerUrl();

            Assert.AreEqual(expectedUrl, result, "GetMcpServerUrl should return the full URL including port");
        }

        [Test]
        public void GetMcpServerUrl_WhenEnvVarChanges_ShouldReturnNewValue()
        {
            const string firstUrl = "http://localhost:8080";
            const string secondUrl = "http://localhost:9090";

            System.Environment.SetEnvironmentVariable(EnvironmentUtils.McpServerUrlEnvVar, firstUrl);
            Assert.AreEqual(firstUrl, EnvironmentUtils.GetMcpServerUrl(), "Should return first URL");

            System.Environment.SetEnvironmentVariable(EnvironmentUtils.McpServerUrlEnvVar, secondUrl);
            Assert.AreEqual(secondUrl, EnvironmentUtils.GetMcpServerUrl(), "Should return updated URL");
        }
    }
}
