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
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The assisted re-auth once-gate + carousel guard (oauth-client-error-hygiene 02 §C4, D4
    /// recovery ladder), driven through the same decision core the production verdict trigger uses
    /// (<see cref="AssistedReauthService.HandleVerdictCore"/>). The session store is faked so a
    /// "domain reload" is a NEW gate over the SAME store (SessionState survives reloads) and a
    /// "fresh editor session" is a new store (SessionState dies with the editor).
    /// </summary>
    public class AssistedReauthGateTests
    {
        sealed class FakeSessionStore : ISessionKeyValueStore
        {
            readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
            readonly Dictionary<string, int> _ints = new Dictionary<string, int>();

            public string GetString(string key, string defaultValue) => _strings.TryGetValue(key, out var value) ? value : defaultValue;
            public void SetString(string key, string value) => _strings[key] = value;
            public int GetInt(string key, int defaultValue) => _ints.TryGetValue(key, out var value) ? value : defaultValue;
            public void SetInt(string key, int value) => _ints[key] = value;
        }

        /// <summary>One verdict arriving at the production decision core; counts flow invocations.</summary>
        static bool Verdict(FakeSessionStore session, string? deadToken, ref int invoked, out string? status,
            bool isCloudMode = true,
            AssistedReauthReasonClass reasonClass = AssistedReauthReasonClass.SessionExpired)
        {
            string? captured = null;
            var invokedLocal = 0;
            var ran = AssistedReauthService.HandleVerdictCore(
                isCloudMode: isCloudMode,
                deadRefreshToken: deadToken,
                reasonClass: reasonClass,
                gate: new AssistedReauthGate(session), // a fresh gate instance per verdict — statics never carry the state
                runFlow: () => invokedLocal++,
                setStatus: s => captured = s);
            invoked += invokedLocal;
            status = captured;
            return ran;
        }

        [Test]
        public void DeadFamilyVerdict_AutoRunsExactlyOnce_AcrossSimulatedDomainReload_AndAgainInFreshSession()
        {
            var session = new FakeSessionStore(); // one editor session
            var invoked = 0;

            // First verdict: the assisted flow auto-runs.
            Assert.IsTrue(Verdict(session, "rt-dead", ref invoked, out _));
            Assert.AreEqual(1, invoked);

            // The same verdict repeated in the same domain: once-gated, no second auto-run.
            Assert.IsFalse(Verdict(session, "rt-dead", ref invoked, out var status));
            Assert.AreEqual(1, invoked);
            Assert.IsNotNull(status); // persistent "sign in required" status instead of a re-open

            // Simulated domain reload: in-memory state dies, SessionState survives — a NEW gate
            // over the SAME session store still refuses to re-run for the same dead token.
            Assert.IsFalse(Verdict(session, "rt-dead", ref invoked, out _));
            Assert.AreEqual(1, invoked);

            // Fresh editor session: SessionState is cleared — the verdict fires again (deliberate,
            // D4 wants the user involved until authorization succeeds).
            Assert.IsTrue(Verdict(new FakeSessionStore(), "rt-dead", ref invoked, out _));
            Assert.AreEqual(2, invoked);
        }

        [Test]
        public void DifferentDeadToken_IsANewVerdict_AndAutoRunsAgain()
        {
            var session = new FakeSessionStore();
            var invoked = 0;

            Assert.IsTrue(Verdict(session, "rt-dead-1", ref invoked, out _));
            // A different family died later (e.g. a peer surface re-logged-in and THAT credential
            // died too): a new verdict — auto-runs again in the same session.
            Assert.IsTrue(Verdict(session, "rt-dead-2", ref invoked, out _));
            Assert.AreEqual(2, invoked);
        }

        [Test]
        public void CarouselGuard_VerdictAfterAssistedSuccess_DoesNotAutoOpen_AndSetsStatus()
        {
            var session = new FakeSessionStore();
            var invoked = 0;

            // The assisted flow ran and committed successfully...
            Assert.IsTrue(Verdict(session, "rt-dead-1", ref invoked, out _));
            new AssistedReauthGate(session).RecordAssistedSuccess();

            // ...and the freshly authorized credential died AGAIN (new token, new verdict): the D4
            // ladder step-3 guard stops the auto-open — persistent status + manual Authorize only.
            Assert.IsFalse(Verdict(session, "rt-dead-2", ref invoked, out var status));
            Assert.AreEqual(1, invoked);
            Assert.IsNotNull(status);
            StringAssert.Contains("Sign in required", status);

            // The guard survives a domain reload (SessionState anchor).
            Assert.IsTrue(new AssistedReauthGate(session).CarouselStopped);
            Assert.IsFalse(Verdict(session, "rt-dead-3", ref invoked, out _));
            Assert.AreEqual(1, invoked);
        }

        [Test]
        public void CarouselGuard_TwoUnattendedExpiries_StopAutoOpening()
        {
            var session = new FakeSessionStore();
            var gate = new AssistedReauthGate(session);

            Assert.AreEqual(AssistedReauthDecision.AutoRun, gate.Decide(AssistedReauthGate.HashToken("rt-1"), AssistedReauthReasonClass.SessionExpired));
            gate.RecordUnattendedExpiry();
            Assert.IsFalse(gate.CarouselStopped); // one unattended expiry: a NEW verdict may still auto-run

            Assert.AreEqual(AssistedReauthDecision.AutoRun, gate.Decide(AssistedReauthGate.HashToken("rt-2"), AssistedReauthReasonClass.SessionExpired));
            gate.RecordUnattendedExpiry();
            Assert.IsTrue(gate.CarouselStopped); // twice unattended — D4 ladder step 3

            // Any further verdict, any token, ANY reason class, even after a domain reload: no auto-open.
            Assert.AreEqual(AssistedReauthDecision.CarouselStopped,
                new AssistedReauthGate(session).Decide(AssistedReauthGate.HashToken("rt-3"), AssistedReauthReasonClass.ServerConfiguration));
        }

        // ── r2 (pin 8.3.0): reason-class keying + rendering (02 §C2 / §C4) ─────────────────────────

        [Test]
        public void CarouselGuard_KeysOnReasonClass_DifferentClassGetsItsOwnLadderRun()
        {
            var session = new FakeSessionStore();
            var invoked = 0;

            // Assisted recovery from a session-expired (invalid_grant) verdict succeeded...
            Assert.IsTrue(Verdict(session, "rt-dead-1", ref invoked, out _));
            new AssistedReauthGate(session).RecordAssistedSuccess();

            // ...then the fresh family died with a DIFFERENT reason class (invalid_target): a new
            // failure mode, not a carousel — it gets its own D4 ladder run (02 §C4).
            Assert.IsTrue(Verdict(session, "rt-dead-2", ref invoked, out _,
                reasonClass: AssistedReauthReasonClass.ServerConfiguration));
            Assert.AreEqual(2, invoked);
            Assert.IsFalse(new AssistedReauthGate(session).CarouselStopped);

            // That run also succeeded — and the family died AGAIN with the same (server-config)
            // class: re-auth is evidently not curing it, so NOW the guard trips, rendering the
            // class-specific status.
            new AssistedReauthGate(session).RecordAssistedSuccess();
            Assert.IsFalse(Verdict(session, "rt-dead-3", ref invoked, out var status,
                reasonClass: AssistedReauthReasonClass.ServerConfiguration));
            Assert.AreEqual(2, invoked);
            Assert.IsTrue(new AssistedReauthGate(session).CarouselStopped);
            Assert.AreEqual(AssistedReauthGate.StatusServerConfigurationError, status);
        }

        [Test]
        public void InvalidTargetVerdict_RendersServerConfigurationErrorStatus_WhereGenericRendersSignInRequired()
        {
            var session = new FakeSessionStore();
            var invoked = 0;

            // First server-config verdict auto-runs (same D4 ladder as invalid_grant, 02 §C2)...
            Assert.IsTrue(Verdict(session, "rt-dead", ref invoked, out _,
                reasonClass: AssistedReauthReasonClass.ServerConfiguration));

            // ...its once-gated repeat parks with the DISTINCT server-configuration status.
            Assert.IsFalse(Verdict(session, "rt-dead", ref invoked, out var status,
                reasonClass: AssistedReauthReasonClass.ServerConfiguration));
            Assert.AreEqual(1, invoked);
            Assert.AreEqual(AssistedReauthGate.StatusServerConfigurationError, status);
            StringAssert.Contains("Server configuration error", status);

            // Control (same fixture shape, generic class): the generic status renders instead.
            var generic = new FakeSessionStore();
            Assert.IsTrue(Verdict(generic, "rt-dead", ref invoked, out _));
            Assert.IsFalse(Verdict(generic, "rt-dead", ref invoked, out var genericStatus));
            Assert.AreEqual(AssistedReauthGate.StatusSignInRequired, genericStatus);
            Assert.AreNotEqual(status, genericStatus);
        }

        [Test]
        public void ClassifyReason_KeysOnTheServerConfigurationPrefix_AtTheStartOnly()
        {
            // The pinned McpPlugin stamps invalid_target verdicts with the class prefix followed by
            // the raw OAuth error (PluginCredentialProvider.SignInRequiredReason contract, 02 §C2).
            Assert.AreEqual(AssistedReauthReasonClass.ServerConfiguration,
                AssistedReauthGate.ClassifyReason("server configuration error: invalid_target: audience mismatch"));

            // invalid_grant reasons are raw — generic class.
            Assert.AreEqual(AssistedReauthReasonClass.SessionExpired,
                AssistedReauthGate.ClassifyReason("invalid_grant: revoked"));

            // Null (boot-time unreadable store, or no verdict yet) is generic.
            Assert.AreEqual(AssistedReauthReasonClass.SessionExpired,
                AssistedReauthGate.ClassifyReason(null));

            // The prefix must be at the START — a reason merely MENTIONING it stays generic.
            Assert.AreEqual(AssistedReauthReasonClass.SessionExpired,
                AssistedReauthGate.ClassifyReason("invalid_grant: server configuration error mentioned downstream"));
        }

        [Test]
        public void LocalServerMode_NeverAutoRuns_AndSetsNoStatus()
        {
            var session = new FakeSessionStore();
            var invoked = 0;

            Assert.IsFalse(Verdict(session, "rt-dead", ref invoked, out var status, isCloudMode: false));
            Assert.AreEqual(0, invoked);
            Assert.IsNull(status); // no behavior change for LocalServer (Custom) mode
        }

        [Test]
        public void MissingStoredToken_DoesNotAutoRun_ButSetsStatus()
        {
            var session = new FakeSessionStore();
            var invoked = 0;

            // Signed out / store missing: nothing to re-authorize automatically — the manual
            // prompt path stays, with the persistent status set.
            Assert.IsFalse(Verdict(session, deadToken: null, ref invoked, out var status));
            Assert.AreEqual(0, invoked);
            Assert.IsNotNull(status);
        }

        [Test]
        public void HashToken_IsDeterministic_AndNeverTheRawToken()
        {
            var hash = AssistedReauthGate.HashToken("rt-secret");
            Assert.AreEqual(64, hash.Length); // SHA-256 hex
            Assert.AreEqual(hash, AssistedReauthGate.HashToken("rt-secret"));
            Assert.AreNotEqual(hash, AssistedReauthGate.HashToken("rt-other"));
            StringAssert.DoesNotContain("rt-secret", hash); // raw token material never lands in SessionState
        }
    }
}
