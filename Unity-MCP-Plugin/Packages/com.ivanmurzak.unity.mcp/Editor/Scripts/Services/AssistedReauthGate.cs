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
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    /// <summary>
    /// What the once-gate + carousel guard decided for a dead-credential verdict
    /// (oauth-client-error-hygiene 02 §C4, D4 recovery ladder).
    /// </summary>
    internal enum AssistedReauthDecision
    {
        /// <summary>Auto-open the browser and run the assisted device flow (D4 ladder step 2).</summary>
        AutoRun,

        /// <summary>This verdict already auto-ran once this editor session — no second auto-open.</summary>
        OnceGated,

        /// <summary>
        /// The D4 ladder step 3 guard: assisted recovery already succeeded once this session and the
        /// credential died again, or the device code expired unattended twice — stop auto-opening;
        /// persistent status + the manual Authorize button take over.
        /// </summary>
        CarouselStopped,
    }

    /// <summary>
    /// Session-scoped key-value store seam. Production is <see cref="EditorSessionKeyValueStore"/>
    /// (Unity's <see cref="SessionState"/>: survives domain reloads, dies with the editor session);
    /// tests inject a dictionary store so a "domain reload" is a new gate over the same store and a
    /// "fresh editor session" is a new store.
    /// </summary>
    internal interface ISessionKeyValueStore
    {
        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
    }

    /// <summary>The production <see cref="ISessionKeyValueStore"/> over <see cref="SessionState"/>. Main thread only.</summary>
    internal sealed class EditorSessionKeyValueStore : ISessionKeyValueStore
    {
        public string GetString(string key, string defaultValue) => SessionState.GetString(key, defaultValue);
        public void SetString(string key, string value) => SessionState.SetString(key, value);
        public int GetInt(string key, int defaultValue) => SessionState.GetInt(key, defaultValue);
        public void SetInt(string key, int value) => SessionState.SetInt(key, value);
    }

    /// <summary>
    /// The assisted re-auth once-gate + carousel guard (oauth-client-error-hygiene 02 §C4).
    /// Anchored in <see cref="SessionState"/> — NOT <c>EditorPrefs</c> (too durable): the gate
    /// survives domain reloads but a full editor restart deliberately re-fires the assisted flow
    /// (D4 wants the user involved until authorization succeeds). Keys are hashes of the dead
    /// refresh token — raw token material NEVER lands in <see cref="SessionState"/>.
    /// </summary>
    internal sealed class AssistedReauthGate
    {
        internal const string HandledVerdictHashKey = "com.IvanMurzak.Unity.MCP.AssistedReauth.HandledVerdictHash";
        internal const string AssistedSuccessKey = "com.IvanMurzak.Unity.MCP.AssistedReauth.AssistedSuccess";
        internal const string UnattendedExpiryCountKey = "com.IvanMurzak.Unity.MCP.AssistedReauth.UnattendedExpiryCount";
        internal const string CarouselStoppedKey = "com.IvanMurzak.Unity.MCP.AssistedReauth.CarouselStopped";

        /// <summary>
        /// The persistent D4 status shown when the assisted flow will not auto-open (once-gated,
        /// carousel-stopped, expired unattended, or nothing to re-authorize) — the manual Authorize
        /// button stays the way forward.
        /// r2: the pinned McpPlugin 8.1.0 does not expose the verdict reason
        /// (<c>PluginCredentialProvider.SignInRequiredReason</c> arrives with the r2 pin bump), so
        /// every terminal verdict renders this generic status; r2 adds the distinct
        /// "server configuration error" rendering for <c>invalid_target</c> verdicts.
        /// </summary>
        internal const string StatusSignInRequired =
            "Sign in required — your session expired. Use the Authorize button to sign in.";

        readonly ISessionKeyValueStore _session;

        public AssistedReauthGate(ISessionKeyValueStore session)
            => _session = session ?? throw new ArgumentNullException(nameof(session));

        /// <summary>True when the D4 ladder step 3 guard tripped this editor session: no more auto-opens.</summary>
        public bool CarouselStopped => _session.GetInt(CarouselStoppedKey, 0) != 0;

        /// <summary>
        /// Decide what a dead-credential verdict keyed by <paramref name="deadTokenHash"/>
        /// (<see cref="HashToken"/> of the dead refresh token) may do this editor session.
        /// </summary>
        public AssistedReauthDecision Decide(string deadTokenHash)
        {
            if (deadTokenHash == null)
                throw new ArgumentNullException(nameof(deadTokenHash));

            if (CarouselStopped)
                return AssistedReauthDecision.CarouselStopped;

            // D4 ladder step 3: assisted re-auth already succeeded once this editor session and the
            // machine credential is dead AGAIN — auto-opening the browser again would be a sign-in
            // carousel; trip the guard instead.
            // r2: key this on the verdict's reason class (invalid_grant vs invalid_target) once the
            // pinned McpPlugin exposes PluginCredentialProvider.SignInRequiredReason.
            if (_session.GetInt(AssistedSuccessKey, 0) != 0)
            {
                StopCarousel();
                return AssistedReauthDecision.CarouselStopped;
            }

            // Once per editor session per verdict: SessionState survives domain reloads, so the
            // browser opens exactly once for one dead token no matter how many reloads happen in
            // between; a DIFFERENT family dying later is a new verdict and auto-runs again; a full
            // editor restart clears SessionState and deliberately re-fires (D4).
            if (string.Equals(_session.GetString(HandledVerdictHashKey, string.Empty), deadTokenHash, StringComparison.Ordinal))
                return AssistedReauthDecision.OnceGated;

            _session.SetString(HandledVerdictHashKey, deadTokenHash);
            return AssistedReauthDecision.AutoRun;
        }

        /// <summary>
        /// An assisted flow (triggered by, or recovering from, a dead-credential verdict) committed
        /// successfully. If the freshly authorized credential dies again this session, the next
        /// <see cref="Decide"/> trips the carousel guard.
        /// </summary>
        public void RecordAssistedSuccess()
        {
            _session.SetInt(AssistedSuccessKey, 1);
            _session.SetInt(UnattendedExpiryCountKey, 0); // the user was present — restart the unattended count
        }

        /// <summary>
        /// An auto-opened device-code request expired with nobody approving it. The flow is NEVER
        /// re-initiated unattended (D4); the second unattended expiry trips the carousel guard.
        /// </summary>
        public void RecordUnattendedExpiry()
        {
            var count = _session.GetInt(UnattendedExpiryCountKey, 0) + 1;
            _session.SetInt(UnattendedExpiryCountKey, count);
            if (count >= 2)
                StopCarousel();
        }

        void StopCarousel() => _session.SetInt(CarouselStoppedKey, 1);

        /// <summary>
        /// SHA-256 hex of a refresh token — the <see cref="SessionState"/> once-gate key. The raw
        /// token never lands in <see cref="SessionState"/> (07 rule 2: no token material outside the
        /// protected machine store).
        /// </summary>
        public static string HashToken(string refreshToken)
        {
            if (refreshToken == null)
                throw new ArgumentNullException(nameof(refreshToken));

            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(refreshToken));
            var builder = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}
