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
    /// Pure-logic tests for the mcp-authorize PR5 configurator-UI deltas (design 06):
    /// the sign-in state chip, the default view omitting the token field, and the advanced
    /// access-token path writing the legacy Bearer-header shape. No UIToolkit / Editor state is
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

        #region Default view omits the token field (advanced-affordance gating)

        [Test]
        public void ShouldOfferAccessTokenAffordance_HiddenForOAuthClients()
        {
            // Default (SupportsOAuth == true) path is credential-free: no token field is offered.
            Assert.IsFalse(AiAgentConfiguratorView.ShouldOfferAccessTokenAffordance(supportsOAuth: true));
        }

        [Test]
        public void ShouldOfferAccessTokenAffordance_ShownForNonOAuthClients()
        {
            // Only a client that cannot do MCP OAuth surfaces the legacy token field.
            Assert.IsTrue(AiAgentConfiguratorView.ShouldOfferAccessTokenAffordance(supportsOAuth: false));
        }

        #endregion

        #region Advanced path writes the Bearer shape; default path omits it

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
        public void AdvancedAccessTokenPath_WritesBearerShape()
        {
            var configurator = AiAgentConfiguratorRegistry.GetByAgentId("claude-code");
            Assert.IsNotNull(configurator);

            // The advanced escape hatch (HttpCredentialMode.AccessToken) writes the legacy Bearer header.
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
            var mode = AiAgentConfiguratorView.ResolveHttpCredentialMode(
                MakeSettings(AgentConnectionMode.Local, AuthOption.token));
            Assert.AreEqual(HttpCredentialMode.AccessToken, mode);
        }

        [TestCase(AgentConnectionMode.Local, AuthOption.none)]
        [TestCase(AgentConnectionMode.Local, AuthOption.oauth)]
        [TestCase(AgentConnectionMode.Cloud, AuthOption.token)]
        [TestCase(AgentConnectionMode.Cloud, AuthOption.none)]
        public void ResolveHttpCredentialMode_OtherModes_StayOAuthUrlOnly(AgentConnectionMode connectionMode, AuthOption authOption)
        {
            // none/oauth (local) authorize natively or are anonymous; Cloud uses the credential-free
            // OAuth golden path — all keep the URL-only default.
            var mode = AiAgentConfiguratorView.ResolveHttpCredentialMode(
                MakeSettings(connectionMode, authOption));
            Assert.AreEqual(HttpCredentialMode.Oauth, mode);
        }

        #endregion
    }
}
