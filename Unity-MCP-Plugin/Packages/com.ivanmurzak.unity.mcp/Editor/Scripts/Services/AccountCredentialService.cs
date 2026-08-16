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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    /// <summary>
    /// Editor-side owner of the shared machine-credential account credential (unified-machine-auth
    /// design 04 store contract / 03 F1+F6). It wraps McpPlugin 8.1's
    /// <see cref="PluginCredentialProvider"/> over the machine store
    /// (<c>~/.ai-game-dev/credentials.json</c>) + the SHARED <see cref="HttpTokenRefresher"/>
    /// (which presents the family's <b>stored</b> <c>clientId</c> and omits <c>scope</c>/<c>resource</c>
    /// entirely on refresh — the P0-3 fix; the old scope-sending <c>UnityTokenRefresher</c> is retired),
    /// all under the cross-process <see cref="MachineCredentialLock"/>:
    /// <list type="bullet">
    ///   <item><b>Auto-adopt:</b> the provider reads the machine store at construction — a present
    ///   plugin-plane credential means <see cref="AuthState.SignedIn"/> with no UI interaction.</item>
    ///   <item><b>Proactive refresh at connect:</b> <see cref="Initialize"/> points
    ///   <see cref="UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider"/> at the provider's
    ///   proactively-refreshed access-token callback (resolved per call, so a <see cref="Reload"/> after
    ///   a login/sign-out is picked up without re-wiring).</item>
    ///   <item><b>On-401 refresh + reconnect:</b> <see cref="AttachTo"/> wires a
    ///   <see cref="ConnectionCredentialCoordinator"/> so a 3-strike authorization rejection refreshes the
    ///   token and reconnects without any UI.</item>
    ///   <item><b>F1 login commit:</b> <see cref="CommitLoginAsync"/> runs the two-lock-hold sequence
    ///   (agent family → RFC 8693 exchange → plugin family + v1 mirror) via
    ///   <see cref="MachineCredentialLoginCommit"/> — never the lock-free, subject-guard-free
    ///   <c>PluginCredentialProvider.Adopt()</c> (G-SEC-2).</item>
    ///   <item><b>F6 sign-out:</b> <see cref="SignOutMachineWideAsync"/> revokes every stored family
    ///   (RFC 7009, best effort) and deletes the store via the lock protocol — signing out ALL tools on
    ///   this machine.</item>
    /// </list>
    /// Credentials NEVER land in a project file / VCS — only in the protected machine store (0600 on
    /// POSIX / DPAPI on Windows), which is shared by every engine plugin, CLI, and the desktop app.
    /// No method here ever logs or returns token material to a UI surface.
    /// </summary>
    public static class AccountCredentialService
    {
        static readonly object _gate = new object();
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(AccountCredentialService));

        static PluginCredentialProvider? _provider;
        static ConnectionCredentialCoordinator? _coordinator;
        static MachineCredentialLock? _machineLock;
        static bool _wired;

        // ── Login-commit retry policy (F1 failure path: "partially authorized, retrying") ───────────

        /// <summary>Backoff (ms) between login-commit retries; the array length caps the retries.</summary>
        internal static readonly int[] DefaultRetryDelaysMs = { 1000, 2000, 4000 };

        /// <summary>The F1 failure-path surface text (03 F1: "P surfaces 'partially authorized, retrying'").</summary>
        internal const string StatusPartiallyAuthorizedRetrying =
            "Partially authorized — retrying the plugin credential derivation...";

        /// <summary>Retry surface for a busy machine-wide credential lock (another tool is writing).</summary>
        internal const string StatusLockBusyRetrying =
            "Another tool is updating this machine's credentials — retrying...";

        /// <summary>
        /// The machine-store-backed credential provider (lazily constructed). Reads
        /// <c>~/.ai-game-dev/credentials.json</c> once per domain; a present plugin-plane credential
        /// auto-adopts as <see cref="AuthState.SignedIn"/>.
        /// </summary>
        public static PluginCredentialProvider Provider
        {
            get
            {
                lock (_gate)
                    return _provider ??= CreateDefaultProvider();
            }
        }

        /// <summary>True when the machine store holds a usable plugin-plane credential (zero-button auto-adopt).</summary>
        public static bool IsSignedIn => Provider.IsSignedIn;

        // ── Wiring seams (d1): ONE factory produces the production refresher/provider, and the
        // integration tests build through the SAME factory (over a temp store + captured HttpClient),
        // so a wiring regression — a scope-sending refresher, a component-default clientId, a
        // disconnected store — reddens the tests rather than only the field. ─────────────────────────

        /// <summary>
        /// The ONE refresher Unity wires into the credential stack: the shared
        /// <see cref="HttpTokenRefresher"/>, which presents the family's stored <c>clientId</c>
        /// (component default only for a legacy family of unknown id, 04 §3.7) and omits
        /// <c>scope</c>/<c>resource</c> entirely (04 §3.3 / P0-3).
        /// </summary>
        internal static ITokenRefresher CreateRefresher(string serverBaseUrl, HttpClient? httpClient = null)
            => new HttpTokenRefresher(serverBaseUrl, DeviceAuthService.DefaultClientId, httpClient);

        /// <summary>
        /// Build a provider over <paramref name="store"/> exactly as production does: shared refresher +
        /// cross-process machine credential lock (04 §2). <paramref name="credentialLock"/> null lets the
        /// provider create the default lock rooted at the store's own directory — which for a temp-dir
        /// test store keeps the lock inside the temp dir too.
        /// </summary>
        internal static PluginCredentialProvider CreateProvider(
            MachineCredentialStore store,
            string serverBaseUrl,
            HttpClient? httpClient = null,
            MachineCredentialLock? credentialLock = null)
            => new PluginCredentialProvider(store, CreateRefresher(serverBaseUrl, httpClient), _logger, credentialLock: credentialLock);

        static PluginCredentialProvider CreateDefaultProvider()
        {
            var store = new MachineCredentialStore();
            return CreateProvider(store, UnityMcpPlugin.UnityConnectionConfig.CloudServerBaseUrl, credentialLock: MachineLock(store));
        }

        /// <summary>
        /// The ONE cross-process credential lock instance this editor domain uses (04 §2), rooted at the
        /// store's directory — shared between the provider's refresh path, the F1 login commit, and the
        /// F6 sign-out so they serialize against each other in-process as well as across processes.
        /// </summary>
        static MachineCredentialLock MachineLock(MachineCredentialStore store)
        {
            lock (_gate)
                return _machineLock ??= new MachineCredentialLock(store.BaseDirectory);
        }

        /// <summary>
        /// Point the connection config's Cloud credential provider at the machine-store-backed,
        /// proactively refreshed access token. Idempotent — the callback resolves the CURRENT provider on
        /// every call, so a provider rebuilt by <see cref="Reload"/> (after a login commit or sign-out) is
        /// presented on the next (re)connect without re-wiring. Safe to call on every editor boot / domain
        /// reload.
        /// </summary>
        public static void Initialize()
        {
            lock (_gate)
            {
                if (_wired)
                    return;
                _ = _provider ??= CreateDefaultProvider();
                UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = () => Provider.GetAccessTokenAsync();
                _wired = true;
            }
        }

        /// <summary>
        /// Attach the auth-rejection → refresh → reconnect coordinator to the freshly-built plugin (design 06,
        /// on-401 recovery). No-op when no machine credential is present (nothing to refresh). Replaces any
        /// previous coordinator so a rebuilt plugin never leaks a stale subscription.
        /// </summary>
        public static void AttachTo(IMcpPlugin? plugin)
        {
            if (plugin == null)
                return;
            Initialize();
            lock (_gate)
            {
                _coordinator?.Dispose();
                _coordinator = null;
                var provider = _provider ??= CreateDefaultProvider();
                if (provider.IsSignedIn)
                    _coordinator = new ConnectionCredentialCoordinator(plugin, provider, _logger);
            }
        }

        /// <summary>
        /// Rebuild the provider from the on-disk store after an EXTERNAL, lock-guarded write (F1 login
        /// commit / F6 sign-out / another tool's rotation the UI wants to reflect now). The provider has
        /// no public re-read seam, and echoing the store back through <c>Adopt()</c> would be a lock-free,
        /// subject-guard-free write (G-SEC-2) — so the rebuild re-runs the provider's auto-adopt
        /// constructor path instead. Never writes. The <see cref="Initialize"/> callback picks the new
        /// provider up automatically; the on-401 coordinator is re-attached on the next plugin build
        /// (<see cref="AttachTo"/>).
        /// </summary>
        public static void Reload()
        {
            lock (_gate)
            {
                _coordinator?.Dispose();
                _coordinator = null;
                _provider?.Dispose();
                _provider = null; // next Provider access reconstructs from the store
            }
        }

        // ── F1: login commit (device flow → agent family → exchange → plugin family + mirror) ───────

        /// <summary>
        /// Commit a freshly minted device-flow agent credential into the shared machine store — the F1
        /// two-lock-hold sequence via <see cref="MachineCredentialLoginCommit"/> (03 F1.3–F1.5):
        /// agent family (first hold, F7 subject guard) → RFC 8693 exchange (no lock) → plugin family +
        /// v1 mirror (second hold). Retries the F1 failure paths with backoff, surfacing
        /// "partially authorized, retrying" through <paramref name="onStatus"/>; a subject mismatch asks
        /// <paramref name="confirmAccountSwitch"/> (F7 dialog; declining revokes the just-minted tokens,
        /// best effort, and aborts with the store untouched). Status strings NEVER carry token material.
        /// </summary>
        /// <param name="agentFamily">The freshly minted agent tokens + the <c>clientId</c> they were minted under (D8).</param>
        /// <param name="subject">The account id decoded from the mint's JWT <c>sub</c> (null for pre-O5 servers).</param>
        /// <param name="serverTarget">The AS/hub target the credential was issued for (null ⇒ the Cloud base URL).</param>
        /// <param name="onStatus">Human-facing progress surface. May be invoked on a BACKGROUND thread
        /// (the commit runs <c>ConfigureAwait(false)</c> so a caller may block on it without deadlocking
        /// the editor main thread) — main-thread marshalling is the caller's job.</param>
        /// <param name="confirmAccountSwitch">F7 confirm: receives the displaced account's subject; return
        /// true to replace. May be invoked on a background thread — marshal any editor dialog to the main
        /// thread. Null ⇒ decline (fail closed).</param>
        public static Task<LoginCommitResult> CommitLoginAsync(
            MachineCredentialFamily agentFamily,
            string? subject,
            string? serverTarget = null,
            Action<string>? onStatus = null,
            Func<string?, bool>? confirmAccountSwitch = null,
            CancellationToken cancellationToken = default)
        {
            var baseUrl = UnityMcpPlugin.UnityConnectionConfig.CloudServerBaseUrl;
            var store = new MachineCredentialStore();
            return CommitLoginCoreAsync(
                store,
                MachineLock(store),
                agentFamily,
                subject,
                serverTarget ?? baseUrl,
                new TokenExchangeClient(baseUrl, DeviceAuthService.DefaultClientId),
                new TokenRevocationClient(baseUrl),
                onStatus,
                confirmAccountSwitch,
                retryDelaysMs: null,
                delay: null,
                cancellationToken);
        }

        /// <summary>
        /// The injectable core of <see cref="CommitLoginAsync"/> (tests drive it over a temp store, a fake
        /// exchange client, and zero delays). Outcome handling:
        /// <list type="bullet">
        ///   <item><see cref="LoginCommitStatus.FullyCommitted"/> — done.</item>
        ///   <item><see cref="LoginCommitStatus.AgentOnly"/> (exchange failed — nothing was minted) and
        ///   <see cref="LoginCommitStatus.Busy"/> — retry the WHOLE commit with backoff.</item>
        ///   <item><see cref="LoginCommitStatus.PluginCommitBusy"/> / <see cref="LoginCommitStatus.StoreUnreadable"/>
        ///   — the plugin family is ALREADY minted (carried in the result); retry only
        ///   <see cref="MachineCredentialLoginCommit.CommitPluginFamilyAsync"/> so no orphan family is minted.</item>
        ///   <item><see cref="LoginCommitStatus.SubjectMismatch"/> / <see cref="LoginCommitStatus.GuardPremiseChanged"/>
        ///   — F7: read the store's current owner, ask <paramref name="confirmAccountSwitch"/>, retry with
        ///   the confirmed displaced subject; decline ⇒ best-effort revoke the just-minted agent family
        ///   (and any minted plugin family carried in the result) and abort, store untouched.</item>
        /// </list>
        /// </summary>
        internal static async Task<LoginCommitResult> CommitLoginCoreAsync(
            MachineCredentialStore store,
            MachineCredentialLock credentialLock,
            MachineCredentialFamily agentFamily,
            string? subject,
            string? serverTarget,
            ITokenExchangeClient exchangeClient,
            ITokenRevocationClient revocationClient,
            Action<string>? onStatus,
            Func<string?, bool>? confirmAccountSwitch,
            int[]? retryDelaysMs = null,
            Func<int, CancellationToken, Task>? delay = null,
            CancellationToken cancellationToken = default)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (credentialLock == null) throw new ArgumentNullException(nameof(credentialLock));
            if (agentFamily == null) throw new ArgumentNullException(nameof(agentFamily));

            var delays = retryDelaysMs ?? DefaultRetryDelaysMs;
            var wait = delay ?? ((ms, ct) => Task.Delay(ms, ct));
            string? confirmedReplaceOfSubject = null;
            var confirmDialogsShown = 0;

            LoginCommitResult result = await MachineCredentialLoginCommit.CommitAsync(
                store, credentialLock, agentFamily, serverTarget, subject, exchangeClient,
                confirmedReplaceOfSubject, revocationClient, _logger, cancellationToken).ConfigureAwait(false);

            var retry = 0;
            while (true)
            {
                switch (result.Status)
                {
                    case LoginCommitStatus.FullyCommitted:
                        onStatus?.Invoke("Authorized — signed in on this machine.");
                        return result;

                    case LoginCommitStatus.SubjectMismatch:
                    case LoginCommitStatus.GuardPremiseChanged:
                    {
                        // F7 account switch. Cap the dialogs: a store whose owner keeps changing under
                        // us is a live conflict the user should resolve by retrying later.
                        var displaced = ReadStoredSubject(store);
                        var confirmed = confirmDialogsShown < 2 && (confirmAccountSwitch?.Invoke(displaced) ?? false);
                        confirmDialogsShown++;
                        if (!confirmed || displaced == null)
                        {
                            onStatus?.Invoke("Sign-in cancelled — this machine keeps its current account.");
                            // F7 decline: best-effort revoke the just-minted tokens so no orphan
                            // device row survives (03 F7.2). The agent family always; a minted plugin
                            // family is carried in the result when the mismatch came from hold 2.
                            await BestEffortRevokeAsync(revocationClient, agentFamily.RefreshToken ?? agentFamily.AccessToken,
                                agentFamily.ClientId, serverTarget, cancellationToken).ConfigureAwait(false);
                            var minted = result.ExchangeResult;
                            if (minted != null && minted.Succeeded)
                                await BestEffortRevokeAsync(revocationClient, minted.RefreshToken ?? minted.AccessToken,
                                    exchangeClient.ClientId, serverTarget, cancellationToken).ConfigureAwait(false);
                            return result;
                        }
                        confirmedReplaceOfSubject = displaced;
                        // F7.2: the user confirmed replacing the displaced account — best-effort
                        // revoke ITS families first, so the switch leaves no orphan device rows.
                        await RevokeAllStoredFamiliesAsync(store, revocationClient, exchangeClient.ClientId, serverTarget, cancellationToken).ConfigureAwait(false);
                        result = await MachineCredentialLoginCommit.CommitAsync(
                            store, credentialLock, agentFamily, serverTarget, subject, exchangeClient,
                            confirmedReplaceOfSubject, revocationClient, _logger, cancellationToken).ConfigureAwait(false);
                        continue; // confirmation retries do not consume the backoff budget
                    }

                    case LoginCommitStatus.StoreSignedOut:
                        // A machine-wide sign-out won the race — the sign-out wins, never recreate (F6).
                        onStatus?.Invoke("Sign-in aborted — this machine was signed out while authorizing. Click Authorize to sign in again.");
                        return result;

                    case LoginCommitStatus.AgentOnly:
                    case LoginCommitStatus.Busy:
                    case LoginCommitStatus.PluginCommitBusy:
                    case LoginCommitStatus.StoreUnreadable:
                    {
                        if (retry >= delays.Length)
                        {
                            onStatus?.Invoke(result.Status == LoginCommitStatus.AgentOnly
                                ? "Partially authorized — the account is committed on this machine, but the plugin credential could not be derived"
                                  + (result.Detail != null ? $" ({result.Detail})" : "") + ". Click Authorize to retry."
                                : "Sign-in could not complete" + (result.Detail != null ? $" ({result.Detail})" : "") + ". Click Authorize to retry.");
                            return result;
                        }

                        onStatus?.Invoke(result.Status == LoginCommitStatus.AgentOnly
                            ? StatusPartiallyAuthorizedRetrying
                            : StatusLockBusyRetrying);
                        await wait(delays[retry], cancellationToken).ConfigureAwait(false);
                        retry++;

                        var mintedPlugin = result.ExchangeResult;
                        if ((result.Status == LoginCommitStatus.PluginCommitBusy || result.Status == LoginCommitStatus.StoreUnreadable)
                            && mintedPlugin != null && mintedPlugin.Succeeded)
                        {
                            // The plugin family is already minted — commit phase 2 alone (no re-exchange,
                            // so the previous mint never becomes an orphan device row).
                            result = await MachineCredentialLoginCommit.CommitPluginFamilyAsync(
                                store, credentialLock, mintedPlugin, exchangeClient.ClientId,
                                expectedSubject: subject, serverTarget, revocationClient, _logger, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            result = await MachineCredentialLoginCommit.CommitAsync(
                                store, credentialLock, agentFamily, serverTarget, subject, exchangeClient,
                                confirmedReplaceOfSubject, revocationClient, _logger, cancellationToken).ConfigureAwait(false);
                        }
                        continue;
                    }

                    default:
                        onStatus?.Invoke("Sign-in failed" + (result.Detail != null ? $": {result.Detail}" : "") + ".");
                        return result;
                }
            }
        }

        /// <summary>Read the store's current subject without the lock (cheap read; F7 dialog input). Null when unknown.</summary>
        internal static string? ReadStoredSubject(MachineCredentialStore store)
        {
            var read = store.TryRead();
            var subject = read.Status == MachineCredentialStoreStatus.Ok ? read.Credentials?.Subject : null;
            return string.IsNullOrEmpty(subject) ? null : subject;
        }

        /// <summary>
        /// Best-effort RFC 7009 revocation of every family currently in the store (F7.2 confirmed
        /// account switch: the displaced account's families must not survive as orphan device rows).
        /// A legacy family of unknown clientId is attempted with <paramref name="componentClientId"/>
        /// (F6.2 — RFC 7009 answers 200 even for a mismatched id). Never throws; never logs tokens.
        /// </summary>
        static async Task RevokeAllStoredFamiliesAsync(
            MachineCredentialStore store, ITokenRevocationClient revocationClient, string componentClientId,
            string? serverTarget, CancellationToken cancellationToken)
        {
            var read = store.TryRead();
            if (read.Status != MachineCredentialStoreStatus.Ok || read.Credentials?.Families == null)
                return;
            var families = read.Credentials.Families;
            foreach (var family in new[] { families.Agent, families.Plugin, families.Legacy })
            {
                if (family == null)
                    continue;
                await BestEffortRevokeAsync(revocationClient, family.RefreshToken ?? family.AccessToken,
                    family.ClientId ?? componentClientId, serverTarget, cancellationToken).ConfigureAwait(false);
            }
        }

        static async Task BestEffortRevokeAsync(
            ITokenRevocationClient revocationClient, string? token, string? clientId, string? serverTarget,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(token))
                return;
            try
            {
                await revocationClient.RevokeAsync(token!, clientId, serverTarget, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning("Best-effort token revocation failed: {message}", ex.Message); // never token material
            }
        }

        // ── F6: machine-wide sign-out ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sign out ALL AI Game Dev tools on this machine (F6): best-effort RFC 7009 revocation of every
        /// stored family (the component default id for a legacy family of unknown id), then the 04 §2
        /// lock-protocol store delete. Offline still deletes locally. The in-process provider is dropped
        /// so the next access reflects the signed-out store. Callers surface the interactive confirm
        /// ("signs out all tools on this machine") BEFORE calling this.
        /// </summary>
        public static async Task<MachineSignOutResult> SignOutMachineWideAsync(CancellationToken cancellationToken = default)
        {
            var baseUrl = UnityMcpPlugin.UnityConnectionConfig.CloudServerBaseUrl;
            var store = new MachineCredentialStore();
            var result = await MachineWideSignOut.SignOutAsync(
                store, MachineLock(store), new TokenRevocationClient(baseUrl),
                DeviceAuthService.DefaultClientId, _logger, cancellationToken).ConfigureAwait(false);
            Reload();
            return result;
        }
    }
}
