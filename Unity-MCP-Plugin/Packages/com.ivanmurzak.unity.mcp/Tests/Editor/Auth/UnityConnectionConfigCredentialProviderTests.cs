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
    /// The machine-store credential wins over the mode-routed token in Cloud mode, and is ignored in
    /// Custom/Local mode (mcp-authorize design 06 / D12). This is the seam that makes zero-button boot
    /// present the shared account JWT while preserving the anonymous/local behaviour when signed out.
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

        static UnityMcpPlugin.UnityConnectionConfig NewConfig(ConnectionMode mode, string? cloudToken, string? localToken)
        {
            var config = new UnityMcpPlugin.UnityConnectionConfig
            {
                ConnectionMode = mode,
                CloudToken = cloudToken,
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
            var config = NewConfig(ConnectionMode.Cloud, cloudToken: "cloud-tok", localToken: "local-tok");
            Assert.AreEqual("machine-jwt", Resolve(config));
        }

        [Test]
        public void Cloud_MachineCredentialEmpty_FallsBackToCloudToken()
        {
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Task.FromResult<string?>(null);
            var config = NewConfig(ConnectionMode.Cloud, cloudToken: "cloud-tok", localToken: "local-tok");
            Assert.AreEqual("cloud-tok", Resolve(config));
        }

        [Test]
        public void Cloud_NoMachineProvider_UsesCloudToken()
        {
            // CloudCredentialProvider is null (set in SetUp) — behaviour identical to the pre-b7 static token.
            var config = NewConfig(ConnectionMode.Cloud, cloudToken: "cloud-tok", localToken: "local-tok");
            Assert.AreEqual("cloud-tok", Resolve(config));
        }

        [Test]
        public void Custom_IgnoresMachineCredential_UsesLocalToken()
        {
            UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Task.FromResult<string?>("machine-jwt");
            var config = NewConfig(ConnectionMode.Custom, cloudToken: "cloud-tok", localToken: "local-tok");
            Assert.AreEqual("local-tok", Resolve(config));
        }
    }
}
