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
        /// credential died again with the same reason class, or the device code expired unattended
        /// twice — stop auto-opening; persistent status + the manual Authorize button take over.
        /// </summary>
        CarouselStopped,
    }

    /// <summary>
    /// Reason class of a dead-credential verdict (oauth-client-error-hygiene 02 §C2): drives the
    /// persistent-status rendering and keys the carousel guard. Derived from the pinned McpPlugin's
    /// <c>PluginCredentialProvider.SignInRequiredReason</c> (8.3.0+), whose <c>invalid_target</c>
    /// verdicts carry the <c>"server configuration error"</c> class prefix.
    /// </summary>
    internal enum AssistedReauthReasonClass
    {
        /// <summary>invalid_grant and every other terminal verdict: signing in again is the cure.</summary>
        SessionExpired,

        /// <summary>
        /// invalid_target — the "server configuration error" class: a server-side audience/resource
        /// mismatch that re-auth does not reliably cure, so it renders its own status.
        /// </summary>
        ServerConfiguration,
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
        internal const string AssistedSuccessReasonClassKey = "com.IvanMurzak.Unity.MCP.AssistedReauth.AssistedSuccessReasonClass";
        internal const string LastVerdictReasonClassKey = "com.IvanMurzak.Unity.MCP.AssistedReauth.LastVerdictReasonClass";
        internal const string UnattendedExpiryCountKey = "com.IvanMurzak.Unity.MCP.AssistedReauth.UnattendedExpiryCount";
        internal const string CarouselStoppedKey = "com.IvanMurzak.Unity.MCP.AssistedReauth.CarouselStopped";

        /// <summary>
        /// The persistent D4 status shown when the assisted flow will not auto-open (once-gated,
        /// carousel-stopped, expired unattended, or nothing to re-authorize) — the manual Authorize
        /// button stays the way forward. Rendered for <see cref="AssistedReauthReasonClass.SessionExpired"/>
        /// verdicts; <c>invalid_target</c> verdicts render <see cref="StatusServerConfigurationError"/>
        /// instead (02 §C2 — pick via <see cref="StatusFor"/>).
        /// </summary>
        internal const string StatusSignInRequired =
            "Sign in required — your session expired. Use the Authorize button to sign in.";

        /// <summary>
        /// The persistent D4 status for the <c>invalid_target</c> "server configuration error" class
        /// (02 §C2): a server-side audience/resource mismatch that signing in again does not reliably
        /// cure — the status says so instead of pretending the session merely expired.
        /// </summary>
        internal const string StatusServerConfigurationError =
            "Server configuration error — the server rejected this machine's credential (invalid_target). " +
            "Signing in again may not fix this. Use the Authorize button to retry.";

        /// <summary>
        /// The <c>SignInRequiredReason</c> class prefix the pinned McpPlugin (8.3.0+) stamps on an
        /// <c>invalid_target</c> dead-family verdict, so engine UIs classify without parsing OAuth
        /// error codes (02 §C2). Matched at the START of the reason only.
        /// </summary>
        internal const string ServerConfigurationErrorReasonPrefix = "server configuration error";

        /// <summary>
        /// Classify the provider's <c>SignInRequiredReason</c> (02 §C2). Null (boot-time
        /// unreadable-store SignInRequired, or any state that predates a refresh verdict) and every
        /// unprefixed reason are the generic session-expired class.
        /// </summary>
        public static AssistedReauthReasonClass ClassifyReason(string? signInRequiredReason)
            => signInRequiredReason != null
                && signInRequiredReason.StartsWith(ServerConfigurationErrorReasonPrefix, StringComparison.OrdinalIgnoreCase)
                ? AssistedReauthReasonClass.ServerConfiguration
                : AssistedReauthReasonClass.SessionExpired;

        /// <summary>The persistent status text for a verdict of <paramref name="reasonClass"/> (02 §C4).</summary>
        public static string StatusFor(AssistedReauthReasonClass reasonClass)
            => reasonClass == AssistedReauthReasonClass.ServerConfiguration
                ? StatusServerConfigurationError
                : StatusSignInRequired;

        readonly ISessionKeyValueStore _session;

        public AssistedReauthGate(ISessionKeyValueStore session)
            => _session = session ?? throw new ArgumentNullException(nameof(session));

        /// <summary>True when the D4 ladder step 3 guard tripped this editor session: no more auto-opens.</summary>
        public bool CarouselStopped => _session.GetInt(CarouselStoppedKey, 0) != 0;

        /// <summary>
        /// Decide what a dead-credential verdict keyed by <paramref name="deadTokenHash"/>
        /// (<see cref="HashToken"/> of the dead refresh token) may do this editor session.
        /// <paramref name="reasonClass"/> is the verdict's 02 §C2 class (from
        /// <see cref="ClassifyReason"/> over the provider's <c>SignInRequiredReason</c>): it keys
        /// the carousel guard and is snapshotted for <see cref="RecordAssistedSuccess"/>.
        /// </summary>
        public AssistedReauthDecision Decide(string deadTokenHash, AssistedReauthReasonClass reasonClass)
        {
            if (deadTokenHash == null)
                throw new ArgumentNullException(nameof(deadTokenHash));

            // Recorded unconditionally so RecordAssistedSuccess can snapshot the class of the
            // verdict its recovery run recovered from (the most recent one decided).
            _session.SetString(LastVerdictReasonClassKey, reasonClass.ToString());

            if (CarouselStopped)
                return AssistedReauthDecision.CarouselStopped;

            // D4 ladder step 3, keyed on the verdict's reason class (02 §C2/§C4): assisted re-auth
            // already succeeded once this editor session and the machine credential is dead AGAIN
            // with the SAME reason class — re-auth is evidently not curing this failure mode, so
            // auto-opening the browser again would be a sign-in carousel; trip the guard instead.
            // A DIFFERENT class is a NEW failure mode: consume the success record and let the new
            // verdict run its own D4 ladder (its recurrence then trips the guard as usual).
            if (_session.GetInt(AssistedSuccessKey, 0) != 0)
            {
                // Absent snapshot (defensive default) counts as the same class, preserving the
                // strictly-safer pre-r2 behavior: any repeat death after a success trips the guard.
                var successClass = _session.GetString(AssistedSuccessReasonClassKey, reasonClass.ToString());
                if (string.Equals(successClass, reasonClass.ToString(), StringComparison.Ordinal))
                {
                    StopCarousel();
                    return AssistedReauthDecision.CarouselStopped;
                }
                _session.SetInt(AssistedSuccessKey, 0);
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
        /// successfully. If the freshly authorized credential dies again this session with the same
        /// reason class, the next <see cref="Decide"/> trips the carousel guard; a different class
        /// is a new failure mode and gets its own ladder run (02 §C4).
        /// </summary>
        public void RecordAssistedSuccess()
        {
            _session.SetInt(AssistedSuccessKey, 1);
            // Snapshot the class of the verdict this recovery recovered from — the most recent one
            // decided. A success with no prior verdict this session (fresh manual sign-in) is not
            // recorded by the service at all, so the default only covers a domain-reload edge; it
            // falls back to session-expired, the common class.
            _session.SetString(AssistedSuccessReasonClassKey,
                _session.GetString(LastVerdictReasonClassKey, AssistedReauthReasonClass.SessionExpired.ToString()));
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
