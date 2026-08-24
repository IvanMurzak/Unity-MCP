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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using R3;
using UnityEditor;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    /// <summary>Terminal result of one assisted sign-in run (<see cref="AssistedReauthService"/>).</summary>
    public enum AssistedReauthOutcome
    {
        /// <summary>Device grant approved AND the F1 login commit fully landed in the machine store.</summary>
        Committed,

        /// <summary>Device grant approved but the login commit did not fully land (busy lock, exchange failure, declined account switch, ...).</summary>
        CommitFailed,

        /// <summary>The device code expired before anybody approved it. Never re-initiated unattended (D4).</summary>
        Expired,

        /// <summary>The flow was cancelled (user click, disposal, or a cancellation token).</summary>
        Cancelled,

        /// <summary>The flow failed (denied, transport error, or an unexpected exception).</summary>
        Failed,

        /// <summary>Another assisted sign-in run is already in flight; nothing was started.</summary>
        AlreadyRunning,
    }

    /// <summary>
    /// The service-level assisted re-auth entry (oauth-client-error-hygiene 02 §C4, D4 recovery
    /// ladder) — the extraction of the Authorize flow that used to live inline in
    /// <c>MainWindowEditor.Connection.cs</c> and existed only while the window was open. It runs the
    /// whole D4 ladder step 2 with or without any window:
    /// <list type="bullet">
    ///   <item><b>Trigger:</b> subscribes <see cref="PluginCredentialProvider.OnSignInRequired"/> /
    ///   <see cref="PluginCredentialProvider.State"/> (re-attached on every plugin (re)build via
    ///   <see cref="AccountCredentialService.AttachTo"/>). A dead-credential verdict in Cloud mode
    ///   auto-opens the default browser on the device-flow verification URL and polls the token
    ///   endpoint until the user approves — bounded by the server's device-code expiry, never
    ///   re-initiated unattended.</item>
    ///   <item><b>Once-gate + carousel guard:</b> <see cref="AssistedReauthGate"/> over
    ///   <see cref="SessionState"/> — one auto-open per dead token per editor session (surviving
    ///   domain reloads), and the D4 step-3 guard stops auto-opening entirely after a repeat death
    ///   or two unattended expiries; the manual Authorize button stays.</item>
    ///   <item><b>Manual entry:</b> the window's Authorize button delegates to
    ///   <see cref="AuthorizeAsync"/> — no duplicated flow.</item>
    ///   <item><b>On success:</b> the F1 login commit (<see cref="AccountCredentialService.CommitLoginAsync"/>),
    ///   then the existing reload flow — <see cref="AccountCredentialService.Reload"/> + plugin
    ///   rebuild + <c>ConnectIfNeeded</c> (KeepConnected intent respected).
    ///   r2: once the McpPlugin pin carries the a3 coordinator stop/resume, the provider's
    ///   SignInRequired→SignedIn edge resumes a stopped loop by itself.</item>
    /// </list>
    /// No method here ever logs or surfaces token material.
    /// </summary>
    public static class AssistedReauthService
    {
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(AssistedReauthService));
        static readonly object _gate = new object();

        static AssistedReauthGate? _sessionGate;
        static IDisposable? _providerSubscription;
        static Task<AssistedReauthOutcome>? _activeTask;
        static volatile DeviceAuthFlow? _activeFlow;
        static volatile string? _statusMessage;

        /// <summary>
        /// Raised on any observable change (flow state, status message, auth state). May fire on ANY
        /// thread — UI subscribers must marshal to the editor main thread themselves.
        /// </summary>
        public static event Action? Changed;

        /// <summary>The live (or most recent) flow's state; <see cref="DeviceAuthFlowState.Idle"/> when none ran yet.</summary>
        public static DeviceAuthFlowState FlowState => _activeFlow?.State ?? DeviceAuthFlowState.Idle;

        /// <summary>The live (or most recent) flow's user code, for the "Code: XXXX" status line.</summary>
        public static string? UserCode => _activeFlow?.UserCode;

        /// <summary>The live (or most recent) flow's error message, if any.</summary>
        public static string? FlowErrorMessage => _activeFlow?.ErrorMessage;

        /// <summary>
        /// The persistent status line (D4 ladder step 3 / commit progress), or null when the flow
        /// state alone tells the story. Never contains token material.
        /// </summary>
        public static string? StatusMessage => _statusMessage;

        /// <summary>True while a device-authorization flow is in flight (initiating / waiting / polling).</summary>
        public static bool IsFlowRunning => IsRunningState(FlowState);

        static bool IsRunningState(DeviceAuthFlowState state)
            => state == DeviceAuthFlowState.Initiating
            || state == DeviceAuthFlowState.WaitingForUser
            || state == DeviceAuthFlowState.Polling;

        internal static AssistedReauthGate SessionGate
        {
            get
            {
                lock (_gate)
                    return _sessionGate ??= new AssistedReauthGate(new EditorSessionKeyValueStore());
            }
        }

        /// <summary>
        /// Subscribe the dead-credential trigger to <paramref name="provider"/> (02 §C4: the
        /// provider's <c>OnSignInRequired</c>/<c>State</c> had zero Unity readers — this is the
        /// reader). Called by <see cref="AccountCredentialService.AttachTo"/> on every plugin
        /// (re)build, replacing any previous subscription so a provider rebuilt by
        /// <see cref="AccountCredentialService.Reload"/> is always covered.
        /// </summary>
        internal static void Attach(PluginCredentialProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            lock (_gate)
            {
                _providerSubscription?.Dispose();
                var subscriptions = new CompositeDisposable();
                provider.OnSignInRequired
                    .Subscribe(_ => OnSignInRequiredVerdict())
                    .AddTo(subscriptions);
                provider.State
                    .Subscribe(state => OnAuthStateChanged(state))
                    .AddTo(subscriptions);
                _providerSubscription = subscriptions;
            }
        }

        /// <summary>
        /// The manual, service-level Authorize entry — the window's Authorize button (and the auth
        /// alert panel) delegate here, and the automatic dead-credential trigger runs the same flow.
        /// Single-flight: a second call while a flow is in flight returns
        /// <see cref="AssistedReauthOutcome.AlreadyRunning"/> without starting anything.
        /// </summary>
        public static Task<AssistedReauthOutcome> AuthorizeAsync() => RunAsync(auto: false);

        /// <summary>Cancel the in-flight device-authorization flow, if any.</summary>
        public static void CancelFlow() => _activeFlow?.Cancel();

        // ── The dead-credential trigger (02 §C4 step 2) ──────────────────────────────────────────

        static void OnSignInRequiredVerdict()
        {
            // Fires from the provider's refresh path — possibly on a thread-pool thread and while
            // the provider's internal gate is held. Never call back into the provider synchronously
            // here; SessionState and the editor APIs are main-thread-only anyway.
            MainThread.Instance.RunAsync(() => HandleVerdictOnMainThread());
        }

        static void OnAuthStateChanged(AuthState state)
        {
            // A signed-in (or signed-out) provider clears the persistent sign-in-required status;
            // either way the UI re-reads everything it renders.
            if (state == AuthState.SignedIn || state == AuthState.SignedOut)
                SetStatus(null);
            else
                RaiseChanged();
        }

        static void HandleVerdictOnMainThread()
        {
            try
            {
                HandleVerdictCore(
                    isCloudMode: UnityMcpPluginEditor.ConnectionMode == ConnectionMode.Cloud,
                    deadRefreshToken: ReadPluginPlaneRefreshToken(),
                    gate: SessionGate,
                    runFlow: () =>
                    {
                        if (IsFlowRunning)
                            return;
                        Debug.LogWarning("[AI Game Developer] The saved sign-in for this machine is no longer valid. " +
                            "Opening your browser to re-authorize — approve the request there to reconnect.");
                        _ = RunAsync(auto: true);
                    },
                    setStatus: status => SetStatus(status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Assisted re-auth trigger failed: {message}", ex.Message);
            }
        }

        /// <summary>
        /// The verdict trigger's decision core (02 §C4), pure for tests: returns true when the
        /// assisted flow auto-runs for this dead-credential verdict. LocalServer (Custom) mode never
        /// auto-runs (no behavior change); a machine with no stored plugin-plane refresh token has
        /// nothing to re-authorize automatically (the manual prompt path stays); otherwise the
        /// <see cref="AssistedReauthGate"/> once-gate + carousel guard decide.
        /// </summary>
        internal static bool HandleVerdictCore(
            bool isCloudMode,
            string? deadRefreshToken,
            AssistedReauthGate gate,
            Action runFlow,
            Action<string> setStatus)
        {
            if (gate == null) throw new ArgumentNullException(nameof(gate));
            if (runFlow == null) throw new ArgumentNullException(nameof(runFlow));
            if (setStatus == null) throw new ArgumentNullException(nameof(setStatus));

            if (!isCloudMode)
                return false; // LocalServer mode keeps its existing behavior (02 §C4)

            if (string.IsNullOrEmpty(deadRefreshToken))
            {
                // Signed out / store missing: nothing to re-authorize automatically — the manual
                // prompt path (auth alert + Authorize button) stays the way in.
                setStatus(AssistedReauthGate.StatusSignInRequired);
                return false;
            }

            var decision = gate.Decide(AssistedReauthGate.HashToken(deadRefreshToken!));
            if (decision != AssistedReauthDecision.AutoRun)
            {
                // Once-gated this editor session, or the D4 step-3 carousel guard tripped: no
                // auto-open — persistent status + the manual Authorize button.
                setStatus(AssistedReauthGate.StatusSignInRequired);
                return false;
            }

            runFlow();
            return true;
        }

        /// <summary>
        /// The dead refresh token, read from the shared machine store: a failed refresh never
        /// deletes the store (unified-machine-auth 03:52), so on a dead-family verdict the store
        /// still holds the declared-dead token and its hash is the once-gate key. Null when the
        /// store is missing/unreadable or holds no plugin-plane refresh token. The store's read
        /// path adopts v1 token material into families, so the plugin plane is
        /// <c>families.plugin</c> with <c>families.legacy</c> as the v1-adopted fallback (04 §1).
        /// </summary>
        internal static string? ReadPluginPlaneRefreshToken()
        {
            try
            {
                var read = new MachineCredentialStore().TryRead();
                if (read.Status != MachineCredentialStoreStatus.Ok || read.Credentials?.Families == null)
                    return null;
                var family = read.Credentials.Families.Plugin ?? read.Credentials.Families.Legacy;
                return family?.RefreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Reading the machine credential store failed: {message}", ex.Message);
                return null;
            }
        }

        // ── The flow runner ──────────────────────────────────────────────────────────────────────

        static Task<AssistedReauthOutcome> RunAsync(bool auto)
        {
            lock (_gate)
            {
                if (_activeTask != null && !_activeTask.IsCompleted)
                    return Task.FromResult(AssistedReauthOutcome.AlreadyRunning);
                _activeTask = RunFlowAndRecordAsync(auto);
                return _activeTask;
            }
        }

        static async Task<AssistedReauthOutcome> RunFlowAndRecordAsync(bool auto)
        {
            await Task.Yield(); // escape the starter's lock scope before touching other services

            // A run that started while sign-in was required is a RECOVERY run — its success feeds
            // the carousel guard (a fresh manual sign-in from a signed-out machine does not).
            bool recovery;
            try
            {
                recovery = auto || AccountCredentialService.Provider.State.CurrentValue == AuthState.SignInRequired;
            }
            catch (Exception)
            {
                recovery = auto;
            }

            SetStatus(null); // the flow's own state drives the status line while it runs

            AssistedReauthOutcome outcome;
            try
            {
                var cloudBaseUrl = UnityMcpPlugin.UnityConnectionConfig.CloudServerBaseUrl;
                outcome = await RunSignInCoreAsync(
                    client: new DeviceAuthService(cloudBaseUrl), // requests scope=mcp:agent (03 F1.2)
                    serverTarget: cloudBaseUrl,
                    // null ⇒ DeviceAuthFlow's default opener, Application.OpenURL — the ONE
                    // browser-open seam (D4); tests inject a fake so CI never opens a browser.
                    openBrowser: null,
                    delay: null,
                    commit: CommitAndReloadAsync,
                    onFlow: RegisterActiveFlow);
            }
            catch (OperationCanceledException)
            {
                outcome = AssistedReauthOutcome.Cancelled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Assisted sign-in failed: {message}", ex.Message); // never token material
                outcome = AssistedReauthOutcome.Failed;
            }

            RecordOutcome(auto, recovery, outcome);
            RaiseChanged();
            return outcome;
        }

        /// <summary>
        /// One assisted sign-in run, injectable for tests: drive the RFC 8628
        /// <see cref="DeviceAuthFlow"/> (device code → browser open → poll), then hand an approved
        /// grant to <paramref name="commit"/>. Polling is strictly bounded — the flow returns on
        /// approval, denial, the device-code expiry deadline, or cancellation/disposal — and is
        /// NEVER re-initiated here: an expired request surfaces the persistent sign-in-required
        /// status instead (D4).
        /// </summary>
        internal static async Task<AssistedReauthOutcome> RunSignInCoreAsync(
            IDeviceAuthClient client,
            string? serverTarget,
            Action<string>? openBrowser,
            Func<TimeSpan, CancellationToken, Task>? delay,
            Func<DeviceAuthLoginResult, CancellationToken, Task<AssistedReauthOutcome>> commit,
            Action<DeviceAuthFlow>? onFlow = null,
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (commit == null) throw new ArgumentNullException(nameof(commit));

            if (cancellationToken.IsCancellationRequested)
                return AssistedReauthOutcome.Cancelled;

            DeviceAuthLoginResult? login = null;
            var flow = new DeviceAuthFlow(client, result => login = result, serverTarget, openBrowser, delay);
            onFlow?.Invoke(flow);

            // Start FIRST, register the external cancellation after: StartAsync assigns its
            // CancellationTokenSource synchronously before its first await, so a Cancel arriving
            // through the registration always reaches the live token source (registering earlier
            // could fire against the not-yet-created source and be lost).
            var startTask = flow.StartAsync();
            using var cancelFlowOnToken = cancellationToken.Register(flow.Cancel);
            await startTask;

            switch (flow.State)
            {
                case DeviceAuthFlowState.Authorized when login != null:
                    return await commit(login, cancellationToken);
                case DeviceAuthFlowState.Expired:
                    return AssistedReauthOutcome.Expired;
                case DeviceAuthFlowState.Cancelled:
                    return AssistedReauthOutcome.Cancelled;
                default:
                    return AssistedReauthOutcome.Failed;
            }
        }

        static void RegisterActiveFlow(DeviceAuthFlow flow)
        {
            _activeFlow = flow;
            flow.OnStateChanged += _ => RaiseChanged();
            RaiseChanged();
        }

        /// <summary>
        /// Production commit for an approved device grant: the F1 login commit into the shared
        /// machine store (agent family → RFC 8693 exchange → plugin family + v1 mirror), then the
        /// existing reload flow — <see cref="AccountCredentialService.Reload"/> + plugin rebuild +
        /// <c>ConnectIfNeeded</c> — so the next (re)connect presents the fresh credential and the
        /// on-401 coordinator re-attaches, reconnecting only when the user's KeepConnected intent
        /// is on (a deliberately stopped connection is never resurrected).
        /// r2: when the McpPlugin pin carries a3's coordinator stop/resume, the provider's
        /// SignInRequired→SignedIn edge resumes a stopped loop by itself and this rebuild becomes
        /// the fresh-sign-in path only.
        /// </summary>
        static async Task<AssistedReauthOutcome> CommitAndReloadAsync(DeviceAuthLoginResult login, CancellationToken cancellationToken)
        {
            LoginCommitResult commit;
            try
            {
                commit = await AccountCredentialService.CommitLoginAsync(
                    login.AgentFamily,
                    login.Subject,
                    login.ServerTarget,
                    onStatus: status => SetStatus(status), // plain field + event — no marshalling needed
                    // The commit runs ConfigureAwait(false), so this callback may fire on a
                    // background thread — the modal dialog must run on the editor main thread.
                    confirmAccountSwitch: displaced => MainThread.Instance.Run(() => EditorUtility.DisplayDialog(
                        "Switch account on this machine?",
                        "This machine is already signed in to a different AI Game Dev account.\n\n"
                        + "Continuing signs the other account out of every tool on this machine (engine plugins, CLIs, and the desktop app) and replaces it with the account you just authorized.",
                        "Replace Account",
                        "Cancel")),
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return AssistedReauthOutcome.Cancelled;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Login commit failed: {message}", ex.Message); // never token material
                SetStatus("Sign-in failed — see the Console for details.");
                return AssistedReauthOutcome.CommitFailed;
            }

            // Reflect the (possibly) rewritten store in this editor domain — a rebuild through the
            // provider's auto-adopt read, never an echo-write (G-SEC-2).
            AccountCredentialService.Reload();

            if (commit.Status != LoginCommitStatus.FullyCommitted)
                return AssistedReauthOutcome.CommitFailed;

            await MainThread.Instance.RunAsync(() =>
            {
                if (UnityMcpPluginEditor.ConnectionMode != ConnectionMode.Cloud)
                    return;
                UnityMcpPluginEditor.Instance.DisposeMcpPluginInstance();
                UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
                UnityMcpPluginEditor.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
                UnityMcpPluginEditor.ConnectIfNeeded();
            });

            return AssistedReauthOutcome.Committed;
        }

        static void RecordOutcome(bool auto, bool recovery, AssistedReauthOutcome outcome)
        {
            try
            {
                switch (outcome)
                {
                    case AssistedReauthOutcome.Committed:
                        if (recovery)
                            // Carousel input (D4 ladder step 3): assisted recovery succeeded once
                            // this session — if the machine credential dies AGAIN, the trigger stops
                            // auto-opening (a browser carousel would be worse than a red status).
                            SessionGate.RecordAssistedSuccess();
                        _logger.LogInformation("Signed in — the machine credential was renewed.");
                        break;

                    case AssistedReauthOutcome.Expired when auto:
                        // The browser was opened but nobody approved before the device code expired.
                        // NEVER re-initiate unattended (D4) — count it; the second unattended expiry
                        // trips the carousel guard.
                        SessionGate.RecordUnattendedExpiry();
                        SetStatusIfEmpty(AssistedReauthGate.StatusSignInRequired);
                        break;

                    case AssistedReauthOutcome.Failed when auto:
                    case AssistedReauthOutcome.CommitFailed when auto:
                        SetStatusIfEmpty(AssistedReauthGate.StatusSignInRequired);
                        break;

                        // Manual outcomes and Cancelled: the flow's own terminal state (rendered by
                        // the window) says enough; no persistent status.
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Recording the assisted sign-in outcome failed: {message}", ex.Message);
            }
        }

        static void SetStatus(string? message)
        {
            _statusMessage = message;
            RaiseChanged();
        }

        static void SetStatusIfEmpty(string message)
        {
            if (string.IsNullOrEmpty(_statusMessage))
                SetStatus(message);
        }

        static void RaiseChanged()
        {
            try
            {
                Changed?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("An AssistedReauthService.Changed subscriber threw: {message}", ex.Message);
            }
        }
    }
}
