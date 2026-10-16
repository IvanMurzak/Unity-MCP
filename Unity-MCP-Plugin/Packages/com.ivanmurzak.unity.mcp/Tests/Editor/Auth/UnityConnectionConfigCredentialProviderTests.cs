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
using NUnit.Framework;

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
    }
}
