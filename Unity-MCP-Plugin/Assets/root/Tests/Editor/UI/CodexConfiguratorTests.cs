/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Markus Wellmann (https://github.com/mrwellmann)         │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class CodexConfiguratorTests : BaseTest
    {
        private string _originalHost = string.Empty;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();
            _originalHost = UnityMcpPluginEditor.Host;
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            UnityMcpPluginEditor.Host = _originalHost;
            yield return base.TearDown();
        }

        [UnityTest]
        public IEnumerator ConfigHttp_HostWithoutPath_AppendsMcp()
        {
            UnityMcpPluginEditor.Host = "http://localhost:8080";

            var configurator = new CodexConfigurator();
            var content = configurator.ConfigHttp.ExpectedFileContent;

            Assert.IsTrue(content.Contains("url = \"http://localhost:8080/mcp\""));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigHttp_HostWithTrailingSlash_AppendsMcpOnce()
        {
            UnityMcpPluginEditor.Host = "http://localhost:8080/";

            var configurator = new CodexConfigurator();
            var content = configurator.ConfigHttp.ExpectedFileContent;

            Assert.IsTrue(content.Contains("url = \"http://localhost:8080/mcp\""));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigHttp_HostAlreadyHasMcp_DoesNotDuplicate()
        {
            UnityMcpPluginEditor.Host = "http://localhost:8080/mcp";

            var configurator = new CodexConfigurator();
            var content = configurator.ConfigHttp.ExpectedFileContent;

            Assert.IsTrue(content.Contains("url = \"http://localhost:8080/mcp\""));
            Assert.IsFalse(content.Contains("/mcp/mcp"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigHttp_HostWithCustomPath_AppendsMcp()
        {
            UnityMcpPluginEditor.Host = "http://localhost:8080/api";

            var configurator = new CodexConfigurator();
            var content = configurator.ConfigHttp.ExpectedFileContent;

            Assert.IsTrue(content.Contains("url = \"http://localhost:8080/api/mcp\""));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ManualHttpCommand_UsesNormalizedUrl()
        {
            UnityMcpPluginEditor.Host = "http://localhost:8080";

            var configurator = new CodexConfigurator();
            var root = configurator.CreateUI(new VisualElement());

            Assert.IsNotNull(root, "Configurator UI root should be created.");

            var textFields = root!.Query<TextField>().ToList();
            var command = textFields
                .Select(tf => tf.value)
                .FirstOrDefault(v => !string.IsNullOrEmpty(v) && v.Contains("codex mcp add") && v.Contains("--url"));

            Assert.AreEqual("codex mcp add ai-game-developer --url http://localhost:8080/mcp", command);
            yield return null;
        }
    }
}
