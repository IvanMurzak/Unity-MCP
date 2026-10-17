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
using System;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    /// <summary>Custom + oauth: the ai-game.dev account token reaches a loopback host only.</summary>
    public class LoopbackTokenScopeTests
    {
        [Theory]
        [InlineData("http://localhost")]
        [InlineData("http://localhost:8080")]
        [InlineData("https://LOCALHOST:443/hub")]
        [InlineData("http://127.0.0.1:11111")]
        [InlineData("http://127.255.255.254")]
        [InlineData("http://[::1]")]
        [InlineData("http://[::1]:8080")]
        [InlineData("http://[::ffff:127.0.0.1]:8080")]
        public void IsLoopback_True(string url) => Assert.True(LoopbackTokenScope.IsLoopback(url));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("localhost:8080")]
        [InlineData("https://ai-game.dev")]
        [InlineData("http://evil.com")]
        [InlineData("http://localhost@evil.com")]
        [InlineData("http://user:pass@localhost:8080")]
        [InlineData("http://127.0.0.1.evil.com")]
        [InlineData("http://localhost.evil.com")]
        [InlineData("http://evil.localhost")]
        [InlineData("http://localhost.")]
        [InlineData("http://128.0.0.1")]
        [InlineData("http://0.0.0.0")]
        [InlineData("http://192.168.1.10:8080")]
        [InlineData("http://[::2]")]
        [InlineData("ftp://localhost")]
        public void IsLoopback_False(string? url) => Assert.False(LoopbackTokenScope.IsLoopback(url));

        static Func<Task<string?>> Account(string? token, Action? onRead = null) => () =>
        {
            onRead?.Invoke();
            return Task.FromResult(token);
        };

        [Fact]
        public async Task Loopback_PrefersAccountToken()
            => Assert.Equal("account", await LoopbackTokenScope.ResolveAsync("http://localhost:5000", Account("account"), "local", "UNITY_MCP_TOKEN"));

        [Fact]
        public async Task Loopback_SignedOut_FallsBackToConfiguredToken()
            => Assert.Equal("local", await LoopbackTokenScope.ResolveAsync("http://[::1]:5000", Account(null), "local", "UNITY_MCP_TOKEN"));

        [Fact]
        public async Task Loopback_NothingAvailable_IsAnonymous()
            => Assert.Null(await LoopbackTokenScope.ResolveAsync("http://127.0.0.1", null, null, "UNITY_MCP_TOKEN"));

        [Fact]
        public async Task Remote_UsesConfiguredToken_NeverReadsAccount()
        {
            var read = false;
            Assert.Equal("local", await LoopbackTokenScope.ResolveAsync("https://my-server.example.com", Account("account", () => read = true), "local", "UNITY_MCP_TOKEN"));
            Assert.False(read);
        }

        [Theory]
        [InlineData("https://my-server.example.com")]
        [InlineData("http://localhost@evil.com")]
        [InlineData("http://127.0.0.1.evil.com:9000")]
        public async Task Remote_WithoutToken_Refuses(string host)
        {
            var read = false;
            var ex = await Assert.ThrowsAsync<CredentialScopeException>(() =>
                LoopbackTokenScope.ResolveAsync(host, Account("account", () => read = true), null, "UNITY_MCP_TOKEN"));
            Assert.False(read);
            Assert.StartsWith("Refusing to send your ai-game.dev account token to ", ex.Message);
            Assert.EndsWith("; set UNITY_MCP_TOKEN or use a localhost server.", ex.Message);
            Assert.DoesNotContain("localhost@", ex.Message);
        }
    }
}
