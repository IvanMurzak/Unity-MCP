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
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using NUnit.Framework;
using AgentConnectionMode = com.IvanMurzak.McpPlugin.AgentConfig.ConnectionMode;
using AuthOption = com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server.AuthOption;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Pure-logic tests for the configurator view's decision points: the sign-in state chip, the
    /// credential mode each connection mode writes, and the Cloud project key (project-keys contract
    /// §7) being written as <c>Authorization: Bearer</c> for every agent. No UIToolkit / Editor state is
    /// exercised — the view's decision points are unit-tested through internal static helpers
    /// (same pattern as <c>MainWindowEditorStatusLogicTests</c>).
    /// </summary>
    public class AiAgentConfiguratorViewLogicTests
    {
        #region Sign-in chip reflects credential state

        [Test]
        public void ComputeSignInChip_SignedIn()
        {
            var (text, uss) = AiAgentConfiguratorView.ComputeSignInChip(isSignedIn: true);
            Assert.AreEqual("Signed in", text);
            Assert.AreEqual(AiAgentConfiguratorView.USS_ChipSignedIn, uss);
        }

        [Test]
        public void ComputeSignInChip_SignedOut()
        {
            var (text, uss) = AiAgentConfiguratorView.ComputeSignInChip(isSignedIn: false);
            Assert.AreEqual("Not signed in", text);
            Assert.AreEqual(AiAgentConfiguratorView.USS_ChipSignedOut, uss);
        }

        #endregion

        #region AccessToken mode writes the Bearer shape; the default path omits it

        private static AgentConfiguratorSettings MakeSettings(string? token)
            => new AgentConfiguratorSettings(
                operatingSystem: OperatingSystemKind.Windows,
                projectRootPath: "C:/proj",
                executableFullPath: "C:/proj/server.exe",
                port: 12345,
                timeoutMs: 60000,
                host: "https://ai-game.dev/mcp",
                token: token,
                // Fully qualified: unqualified "ConnectionMode" binds to the Unity enum
                // (com.IvanMurzak.Unity.MCP.ConnectionMode) via enclosing-namespace precedence,
                // not the AgentConfig one this ctor expects.
                connectionMode: com.IvanMurzak.McpPlugin.AgentConfig.ConnectionMode.Cloud);

        [Test]
        public void DefaultOAuthPath_OmitsAccessToken()
        {
            var configurator = AiAgentConfiguratorRegistry.GetByAgentId("claude-code");
            Assert.IsNotNull(configurator);

            // Default HttpCredentialMode is Oauth — credential-free even when settings carry a token.
            var http = configurator!.GetHttpConfig(MakeSettings("SECRET-PAT-XYZ"));
            StringAssert.DoesNotContain("SECRET-PAT-XYZ", http.ExpectedFileContent);
            StringAssert.DoesNotContain("Bearer", http.ExpectedFileContent);
        }

        [Test]
        public void AccessTokenMode_WritesBearerShape()
        {
            var configurator = AiAgentConfiguratorRegistry.GetByAgentId("claude-code");
            Assert.IsNotNull(configurator);

            // HttpCredentialMode.AccessToken writes the Bearer header.
            var http = configurator!.GetHttpConfig(
                MakeSettings("SECRET-PAT-XYZ"),
                credentialMode: HttpCredentialMode.AccessToken);
            StringAssert.Contains("Bearer SECRET-PAT-XYZ", http.ExpectedFileContent);
        }

        #endregion

        #region Local `token` mode Configure writes the Bearer; every other mode stays URL-only (g5/g6)

        private static AgentConfiguratorSettings MakeSettings(AgentConnectionMode connectionMode, AuthOption authOption)
            => new AgentConfiguratorSettings(
                operatingSystem: OperatingSystemKind.Windows,
                projectRootPath: "C:/proj",
                executableFullPath: "C:/proj/server.exe",
                port: 12345,
                timeoutMs: 60000,
                host: "http://localhost:12345",
                token: "LOCAL-SECRET",
                connectionMode: connectionMode,
                authOption: authOption);

        [Test]
        public void ResolveHttpCredentialMode_LocalTokenMode_UsesAccessToken()
        {
            // A loopback token-gated server MUST get the Authorization: Bearer header in the client config.
            var mode = MakeSettings(AgentConnectionMode.Local, AuthOption.token).ResolveHttpCredentialMode();
            Assert.AreEqual(HttpCredentialMode.AccessToken, mode);
        }

        [TestCase(AgentConnectionMode.Local, AuthOption.none)]
        [TestCase(AgentConnectionMode.Local, AuthOption.oauth)]
        [TestCase(AgentConnectionMode.Cloud, AuthOption.token)]
        [TestCase(AgentConnectionMode.Cloud, AuthOption.none)]
        public void ResolveHttpCredentialMode_OtherModes_StayOAuthUrlOnly(AgentConnectionMode connectionMode, AuthOption authOption)
        {
            // none/oauth (local) authorize natively or are anonymous; Cloud WITHOUT a project key (signed
            // out, or the mint failed) keeps the credential-free URL-only default.
            var mode = MakeSettings(connectionMode, authOption).ResolveHttpCredentialMode();
            Assert.AreEqual(HttpCredentialMode.Oauth, mode);
        }

        #endregion

        #region Cloud project key is written for every agent (project-keys contract §7)

        private const string ProjectKey = "agd_pk_TESTKEY-0123456789";

        private static AgentConfiguratorSettings CloudWithKey(string? key)
            => MakeSettings(AgentConnectionMode.Cloud, AuthOption.oauth).WithProjectKey(key);

        [Test]
        public void CloudWithProjectKey_EveryAgentWritesTheKey_AndReadsBackConfigured()
        {
            var settings = CloudWithKey(ProjectKey);
            Assert.AreEqual(HttpCredentialMode.AccessToken, settings.ResolveHttpCredentialMode());

            foreach (var configurator in AiAgentConfiguratorRegistry.All)
            {
                if (configurator is com.IvanMurzak.McpPlugin.AgentConfig.Impl.CustomConfigurator)
                    continue;
                var http = configurator.GetHttpConfig(settings, credentialMode: settings.ResolveHttpCredentialMode());
                StringAssert.Contains(ProjectKey, http.ExpectedFileContent, $"{configurator.AgentId} must carry the project key");
                StringAssert.DoesNotContain("LOCAL-SECRET", http.ExpectedFileContent, $"{configurator.AgentId} must not carry the local secret");
            }
        }

        [Test]
        public void CloudWithoutProjectKey_StaysUrlOnly()
        {
            // Signed out / mint failed / feature off (404): the config is URL-only, never an error.
            var settings = CloudWithKey(null);
            Assert.IsFalse(settings.HasProjectKey);
            Assert.AreEqual(HttpCredentialMode.Oauth, settings.ResolveHttpCredentialMode());

            var configurator = AiAgentConfiguratorRegistry.GetByAgentId("claude-code");
            var http = configurator!.GetHttpConfig(settings, credentialMode: settings.ResolveHttpCredentialMode());
            StringAssert.DoesNotContain("Bearer", http.ExpectedFileContent);
        }

        [Test]
        public void LocalServer_IgnoresProjectKey()
        {
            // Local-server mode is unchanged: a key never replaces the local secret.
            var settings = MakeSettings(AgentConnectionMode.Local, AuthOption.token).WithProjectKey(ProjectKey);
            Assert.IsFalse(settings.HasProjectKey);
            var http = AiAgentConfiguratorRegistry.GetByAgentId("claude-code")!
                .GetHttpConfig(settings, credentialMode: settings.ResolveHttpCredentialMode());
            StringAssert.Contains("Bearer LOCAL-SECRET", http.ExpectedFileContent);
            StringAssert.DoesNotContain(ProjectKey, http.ExpectedFileContent);
        }

        [Test]
        public void Describe_NeverRendersTheRawProjectKey()
        {
            var settings = CloudWithKey(ProjectKey);
            foreach (var configurator in AiAgentConfiguratorRegistry.All)
            {
                var description = configurator.Describe(settings, com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server.TransportMethod.streamableHttp);
                foreach (var section in description.Sections)
                    foreach (var item in section.Items)
                        StringAssert.DoesNotContain(ProjectKey, item.Text ?? string.Empty, $"{configurator.AgentId}: {section.Heading}");
            }
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void DescribeKeyState_SaysWhetherAKeyIsInUse(bool isSignedIn, bool hasKey)
        {
            var text = com.IvanMurzak.Unity.MCP.Editor.Services.ProjectKeyService.DescribeKeyState(isSignedIn, hasKey);
            if (hasKey)
                StringAssert.StartsWith("Project key in use", text);
            else
                StringAssert.Contains("URL-only", text);
            if (!isSignedIn)
                StringAssert.Contains("Sign in", text);
            StringAssert.DoesNotContain("git", text.ToLowerInvariant());
        }

        #endregion
    }
}
