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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The Unity F1 login-commit surface (unified-machine-auth 03 F1.3–F1.5), driven through
    /// <see cref="AccountCredentialService.CommitLoginCoreAsync"/> over a REAL temp-dir store + REAL
    /// cross-process lock, with the exchange/revocation network seams faked: a fresh login commits
    /// the agent family, derives the plugin family (stamped with Unity's own client id), stamps the
    /// v1 mirror; a failed exchange leaves the committed agent family and surfaces
    /// "partially authorized, retrying"; a subject mismatch runs the F7 confirm and, declined,
    /// leaves the store untouched and best-effort revokes the just-minted tokens.
    /// </summary>
    public class LoginCommitServiceTests
    {
        sealed class FakeExchangeClient : ITokenExchangeClient
        {
            readonly Func<int, TokenExchangeResult> _respond;
            public int Calls { get; private set; }
            public string? LastSubjectToken { get; private set; }

            public FakeExchangeClient(Func<int, TokenExchangeResult> respond) => _respond = respond;

            public string ClientId => "unity-mcp-plugin";

            public Task<TokenExchangeResult> ExchangeAsync(string subjectAccessToken, string? serverTarget, CancellationToken cancellationToken = default)
            {
                Calls++;
                LastSubjectToken = subjectAccessToken;
                return Task.FromResult(_respond(Calls));
            }
        }

        sealed class FakeRevocationClient : ITokenRevocationClient
        {
            public readonly List<(string Token, string? ClientId)> Revoked = new List<(string, string?)>();

            public Task<bool> RevokeAsync(string token, string? clientId, string? serverTarget, CancellationToken cancellationToken = default)
            {
                lock (Revoked)
                    Revoked.Add((token, clientId));
                return Task.FromResult(true);
            }
        }

        static TokenExchangeResult ExchangeSuccess() => TokenExchangeResult.Success(
            "plugin.jwt", "rt-plugin-1", DateTimeOffset.UtcNow.AddHours(1),
            scope: "mcp:plugin", subject: "usr_42", issuedTokenType: "urn:ietf:params:oauth:token-type:access_token");

        static MachineCredentialFamily AgentFamily() => new MachineCredentialFamily
        {
            AccessToken = "agent.jwt",
            RefreshToken = "rt-agent-1",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            ClientId = "unity-mcp-plugin",
            Scope = "mcp:agent",
        };

        string _dir = null!;
        MachineCredentialStore _store = null!;
        MachineCredentialLock _lock = null!;
        List<string> _statuses = null!;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "agd-logincommit-tests-" + Guid.NewGuid().ToString("N"));
            _store = new MachineCredentialStore(_dir);
            _lock = new MachineCredentialLock(_dir);
            _statuses = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }

        Task<LoginCommitResult> Commit(
            ITokenExchangeClient exchange,
            ITokenRevocationClient? revocation = null,
            string? subject = "usr_42",
            Func<string?, bool>? confirmAccountSwitch = null)
            => AccountCredentialService.CommitLoginCoreAsync(
                _store, _lock, AgentFamily(), subject, "https://ai-game.dev",
                exchange, revocation ?? new FakeRevocationClient(),
                onStatus: _statuses.Add,
                confirmAccountSwitch: confirmAccountSwitch,
                retryDelaysMs: new[] { 0, 0 },
                delay: (_, __) => Task.CompletedTask);

        [Test]
        public void FreshLogin_Commits_AgentAndPluginFamilies_AndV1Mirror()
        {
            var exchange = new FakeExchangeClient(_ => ExchangeSuccess());

            var result = Commit(exchange).GetAwaiter().GetResult();

            Assert.AreEqual(LoginCommitStatus.FullyCommitted, result.Status);
            Assert.AreEqual(1, exchange.Calls);
            Assert.AreEqual("agent.jwt", exchange.LastSubjectToken, "the agent access token is the exchange subject");

            var stored = _store.Read()!;
            // Agent family: the mint, under the clientId it was minted with (D8) and the agent scope.
            Assert.AreEqual("agent.jwt", stored.Families!.Agent!.AccessToken);
            Assert.AreEqual("rt-agent-1", stored.Families.Agent.RefreshToken);
            Assert.AreEqual("unity-mcp-plugin", stored.Families.Agent.ClientId);
            Assert.AreEqual("mcp:agent", stored.Families.Agent.Scope);
            // Plugin family: the derivation, stamped with the id the exchange PRESENTED.
            Assert.AreEqual("plugin.jwt", stored.Families.Plugin!.AccessToken);
            Assert.AreEqual("rt-plugin-1", stored.Families.Plugin.RefreshToken);
            Assert.AreEqual("unity-mcp-plugin", stored.Families.Plugin.ClientId);
            Assert.AreEqual("mcp:plugin", stored.Families.Plugin.Scope);
            // v1 compat mirror tracks the plugin family; subject recorded for the D6/F7 guard.
            Assert.AreEqual("plugin.jwt", stored.AccessToken);
            Assert.AreEqual("rt-plugin-1", stored.RefreshToken);
            Assert.AreEqual("usr_42", stored.Subject);
            Assert.AreEqual(2, stored.Version);
        }

        [Test]
        public void ExchangeFailure_LeavesCommittedAgentFamily_AndSurfacesPartiallyAuthorizedRetrying()
        {
            var exchange = new FakeExchangeClient(_ => TokenExchangeResult.Failure("AS unreachable"));

            var result = Commit(exchange).GetAwaiter().GetResult();

            // F1 failure path: the agent family STAYS committed (two separate lock holds by design)…
            Assert.AreEqual(LoginCommitStatus.AgentOnly, result.Status);
            var stored = _store.Read()!;
            Assert.AreEqual("agent.jwt", stored.Families!.Agent!.AccessToken);
            Assert.IsNull(stored.Families.Plugin);
            // …the exchange was retried with backoff (1 initial + 2 configured retries)…
            Assert.AreEqual(3, exchange.Calls);
            // …and the UI surface said "partially authorized, retrying" while it did (03 F1).
            Assert.Contains(AccountCredentialService.StatusPartiallyAuthorizedRetrying, _statuses);
            StringAssert.Contains("Partially authorized", _statuses[_statuses.Count - 1]);
        }

        [Test]
        public void ExchangeFailure_ThenSuccessOnRetry_FullyCommits()
        {
            var exchange = new FakeExchangeClient(call => call == 1
                ? TokenExchangeResult.Failure("transient 503")
                : ExchangeSuccess());

            var result = Commit(exchange).GetAwaiter().GetResult();

            Assert.AreEqual(LoginCommitStatus.FullyCommitted, result.Status);
            Assert.AreEqual(2, exchange.Calls);
            Assert.Contains(AccountCredentialService.StatusPartiallyAuthorizedRetrying, _statuses);
            var stored = _store.Read()!;
            Assert.AreEqual("plugin.jwt", stored.Families!.Plugin!.AccessToken);
        }

        [Test]
        public void SubjectMismatch_Declined_LeavesStoreUntouched_AndRevokesTheMint()
        {
            // The machine already belongs to a DIFFERENT account.
            _store.Write(new MachineCredentials
            {
                Subject = "usr_other",
                Families = new MachineCredentialFamilies
                {
                    Plugin = new MachineCredentialFamily { AccessToken = "other.jwt", RefreshToken = "rt-other", ClientId = "app-dcr-1" },
                },
            });
            var exchange = new FakeExchangeClient(_ => ExchangeSuccess());
            var revocation = new FakeRevocationClient();
            string? displacedSeen = null;

            var result = Commit(exchange, revocation, confirmAccountSwitch: displaced =>
            {
                displacedSeen = displaced;
                return false; // user declines the account switch
            }).GetAwaiter().GetResult();

            Assert.AreEqual(LoginCommitStatus.SubjectMismatch, result.Status);
            Assert.AreEqual("usr_other", displacedSeen, "the F7 dialog names the displaced account");
            Assert.AreEqual(0, exchange.Calls, "hold-1 refusal aborts before any exchange");

            // Store untouched: still the other account's credential, no agent family added.
            var stored = _store.Read()!;
            Assert.AreEqual("usr_other", stored.Subject);
            Assert.AreEqual("other.jwt", stored.Families!.Plugin!.AccessToken);
            Assert.IsNull(stored.Families.Agent);

            // F7 decline: the just-minted agent tokens are best-effort revoked (no orphan device row).
            Assert.AreEqual(1, revocation.Revoked.Count);
            Assert.AreEqual("rt-agent-1", revocation.Revoked[0].Token);
        }

        [Test]
        public void SubjectMismatch_Confirmed_ReplacesTheAccount()
        {
            _store.Write(new MachineCredentials
            {
                Subject = "usr_other",
                Families = new MachineCredentialFamilies
                {
                    Plugin = new MachineCredentialFamily { AccessToken = "other.jwt", RefreshToken = "rt-other", ClientId = "app-dcr-1" },
                },
            });
            var exchange = new FakeExchangeClient(_ => ExchangeSuccess());
            var revocation = new FakeRevocationClient();

            var result = Commit(exchange, revocation, confirmAccountSwitch: _ => true).GetAwaiter().GetResult();

            Assert.AreEqual(LoginCommitStatus.FullyCommitted, result.Status);
            var stored = _store.Read()!;
            Assert.AreEqual("usr_42", stored.Subject);
            Assert.AreEqual("agent.jwt", stored.Families!.Agent!.AccessToken);
            Assert.AreEqual("plugin.jwt", stored.Families.Plugin!.AccessToken);

            // F7.2: the displaced account's family was best-effort revoked, under ITS stored clientId.
            Assert.AreEqual(1, revocation.Revoked.Count);
            Assert.AreEqual("rt-other", revocation.Revoked[0].Token);
            Assert.AreEqual("app-dcr-1", revocation.Revoked[0].ClientId);
        }

        [Test]
        public void NoConfirmCallback_FailsClosed_OnSubjectMismatch()
        {
            _store.Write(new MachineCredentials { Subject = "usr_other", AccessToken = "other.jwt" });
            var exchange = new FakeExchangeClient(_ => ExchangeSuccess());

            var result = Commit(exchange, confirmAccountSwitch: null).GetAwaiter().GetResult();

            Assert.AreEqual(LoginCommitStatus.SubjectMismatch, result.Status);
            Assert.AreEqual("usr_other", _store.Read()!.Subject);
        }
    }
}
