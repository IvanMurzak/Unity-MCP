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
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using NUnit.Framework;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The machine store is the ONLY Cloud-mode credential source (mcp-authorize design 06 / D12; T9 —
    /// the legacy cloudToken UserSettings mirror was removed): Cloud mode presents the shared account JWT
    /// when signed in and an anonymous (null) credential otherwise, while Custom/Local mode ignores the
    /// machine store and uses the local token.
    /// </summary>
    public class UnityConnectionConfigCredentialProviderTests
    {
        System.Func<Task<string?>>? _saved;

        [SetUp]
        public void SetUp()
        {
            _saved = UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider;
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = null;
        }

        [TearDown]
        public void TearDown()
        {
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = _saved;
        }

        static UnityMcpPlugin.UnityConnectionConfig NewConfig(ConnectionMode mode, string? localToken)
        {
            var config = new UnityMcpPlugin.UnityConnectionConfig
            {
                ConnectionMode = mode,
                LocalToken = localToken,
            };
            return config;
        }

        static string? Resolve(UnityMcpPlugin.UnityConnectionConfig config)
            => config.CredentialProvider!().GetAwaiter().GetResult();

        [Test]
        public void Cloud_WithMachineCredential_PresentsMachineJwt()
        {
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Task.FromResult<string?>("machine-jwt");
            var config = NewConfig(ConnectionMode.Cloud, localToken: "local-tok");
            Assert.AreEqual("machine-jwt", Resolve(config));
        }

        [Test]
        public void Cloud_MachineCredentialEmpty_ReturnsNull()
        {
            // T9: the machine store is the ONLY Cloud-mode credential source — the legacy cloudToken mirror
            // was removed, so an empty machine credential yields an anonymous (null) connection, no fallback.
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Task.FromResult<string?>(null);
            var config = NewConfig(ConnectionMode.Cloud, localToken: "local-tok");
            Assert.IsNull(Resolve(config));
        }

        [Test]
        public void Cloud_NoMachineProvider_ReturnsNull()
        {
            // CloudCredentialProvider is null (set in SetUp). With no machine store wired and no cloudToken
            // fallback (T9), Cloud mode presents no credential (anonymous connection).
            var config = NewConfig(ConnectionMode.Cloud, localToken: "local-tok");
            Assert.IsNull(Resolve(config));
        }

        [Test]
        public void Custom_IgnoresMachineCredential_UsesLocalToken()
        {
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Task.FromResult<string?>("machine-jwt");
            var config = NewConfig(ConnectionMode.Custom, localToken: "local-tok");
            Assert.AreEqual("local-tok", Resolve(config));
        }

        static UnityMcpPlugin.UnityConnectionConfig CustomOAuth(string host, string? localToken)
        {
            var config = NewConfig(ConnectionMode.Custom, localToken);
            config.AuthOption = AuthOption.oauth;
            config.LocalHost = host;
            return config;
        }

        [TestCase("http://localhost:5000")]
        [TestCase("http://127.0.0.1:5000")]
        [TestCase("http://[::1]:5000")]
        public void CustomOAuth_Loopback_PresentsMachineJwt(string host)
        {
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Task.FromResult<string?>("machine-jwt");
            Assert.AreEqual("machine-jwt", Resolve(CustomOAuth(host, localToken: "local-tok")));
        }

        [Test]
        public void CustomOAuth_Loopback_SignedOut_UsesLocalToken()
        {
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Task.FromResult<string?>(null);
            Assert.AreEqual("local-tok", Resolve(CustomOAuth("http://localhost:5000", localToken: "local-tok")));
        }

        [TestCase("https://my-server.example.com")]
        [TestCase("http://localhost@evil.com")]
        [TestCase("http://127.0.0.1.evil.com")]
        public void CustomOAuth_Remote_NeverPresentsMachineJwt(string host)
        {
            var read = false;
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => { read = true; return Task.FromResult<string?>("machine-jwt"); };
            Assert.AreEqual("local-tok", Resolve(CustomOAuth(host, localToken: "local-tok")));
            Assert.IsFalse(read);
        }

        [Test]
        public void CustomOAuth_ProviderKeepsItsDialHost_WhenHostLaterTurnsLoopback()
        {
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Task.FromResult<string?>("machine-jwt");
            var config = CustomOAuth("https://my-server.example.com", localToken: "local-tok");
            var remoteLink = config.CredentialProvider!; // what the live remote HubConnection holds
            config.LocalHost = "http://localhost:5000";
            Assert.AreEqual("local-tok", remoteLink().GetAwaiter().GetResult());
            Assert.AreEqual("machine-jwt", Resolve(config)); // a new link to localhost
        }

        [Test]
        public void CustomOAuth_Remote_WithoutToken_Refuses()
        {
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Task.FromResult<string?>("machine-jwt");
            var ex = Assert.Throws<CredentialScopeException>(() => Resolve(CustomOAuth("https://my-server.example.com", localToken: null)));
            Assert.AreEqual("Refusing to send your ai-game.dev account token to my-server.example.com; " +
                            "set UNITY_MCP_TOKEN or use a localhost server.", ex!.Message);
        }
    }
}
