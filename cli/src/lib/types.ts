// Shared public types for the unity-mcp-cli library API.
//
// This file is re-exported from `lib.ts` — consumers should import
// from `unity-mcp-cli` (the package root), NOT from deep paths.
//
// No top-level side effects, no runtime deps beyond TypeScript types.

// ---------------------------------------------------------------------------
// Progress events
// ---------------------------------------------------------------------------

/**
 * Discriminated union describing every progress event the library can
 * emit to the optional `onProgress` callback.
 *
 * Consumers can narrow on `event.phase` to decide what to render.
 */
export type ProgressEvent =
  | { phase: 'start'; message: string }
  | { phase: 'manifest-patched'; message: string; manifestPath: string }
  | { phase: 'dependencies-resolved'; message: string; version: string }
  // openProject phases
  | { phase: 'detecting-editor-version'; message: string }
  | { phase: 'editors-located'; message: string; found: boolean }
  | { phase: 'editor-resolved'; message: string; editorPath: string; version?: string }
  | {
      phase: 'connection-details';
      message: string;
      projectPath: string;
      editorPath: string;
      envVars: Record<string, string>;
    }
  | { phase: 'launching-editor'; message: string; editorPath: string; projectPath: string }
  | { phase: 'editor-launched'; message: string; pid?: number }
  | {
      phase: 'launch-errors-dismissed';
      message: string;
      /** Button label that was clicked (e.g. `Ignore`). */
      button: string;
      /** Platform on which the dismiss was performed (`win32` | `darwin` | `linux`). */
      platform: string;
    }
  | { phase: 'done'; message: string };

export type ProgressCallback = (event: ProgressEvent) => void;

// ---------------------------------------------------------------------------
// Result discriminator
// ---------------------------------------------------------------------------

/**
 * Discriminator literal for every library result type. Consumers should
 * narrow on `result.kind === 'success'` (or `=== 'failure'`) to access
 * variant-specific fields with full TypeScript type safety.
 *
 * Wire-compatible note: every result object also carries a `success`
 * boolean that satisfies `success === (kind === 'success')` so existing
 * consumers reading `result.success` continue to work without changes.
 *
 * Exported for consumer ergonomics (e.g. mapping a `ResultKind` to a UI
 * label). DO NOT use as a field type on a result variant — every variant
 * MUST inline the literal (`kind: 'success'` / `kind: 'failure'`) so
 * TypeScript's discriminated-union narrowing works. Declaring
 * `kind: ResultKind` on a variant silently collapses narrowing.
 */
export type ResultKind = 'success' | 'failure';

// ---------------------------------------------------------------------------
// install-plugin
// ---------------------------------------------------------------------------

export interface InstallPluginOptions {
  /** Absolute or relative path to the Unity project's root. */
  unityProjectPath: string;
  /**
   * Plugin version to install. When omitted, the latest version is
   * resolved from OpenUPM.
   */
  version?: string;
  /**
   * Optional progress callback — fires for `start`,
   * `dependencies-resolved` (when the version was auto-resolved),
   * `manifest-patched`, and `done`.
   */
  onProgress?: ProgressCallback;
}

/** Successful `installPlugin` outcome. Narrow with `kind === 'success'`. */
export interface InstallSuccess {
  kind: 'success';
  /** Always `true` for the success variant. Wire-compatible alias for `kind === 'success'`. */
  success: true;
  /** Final plugin version in the manifest. */
  installedVersion: string;
  /** Absolute path to the manifest.json that was inspected / written. */
  manifestPath: string;
  /** Non-fatal warnings collected during the run (e.g. skipped downgrade). */
  warnings: string[];
  /** Suggested next steps for the caller to surface to a human user. */
  nextSteps: string[];
}

/** Failed `installPlugin` outcome. Narrow with `kind === 'failure'`. */
export interface InstallFailure {
  kind: 'failure';
  /** Always `false` for the failure variant. Wire-compatible alias for `kind === 'failure'`. */
  success: false;
  /** Manifest path may be known even on failure (e.g. when validation reaches the manifest). */
  manifestPath?: string;
  /** Non-fatal warnings collected before the failure. */
  warnings: string[];
  /** Suggested next steps the caller may surface to a human user. */
  nextSteps: string[];
  /** The captured error. Never thrown past this boundary. */
  error: Error;
}

export type InstallResult = InstallSuccess | InstallFailure;

// ---------------------------------------------------------------------------
// remove-plugin
// ---------------------------------------------------------------------------

export interface RemovePluginOptions {
  unityProjectPath: string;
  onProgress?: ProgressCallback;
}

export interface RemoveSuccess {
  kind: 'success';
  success: true;
  /** `true` when the plugin dependency was present and has been removed. */
  removed: boolean;
  manifestPath: string;
  warnings: string[];
}

export interface RemoveFailure {
  kind: 'failure';
  success: false;
  manifestPath?: string;
  warnings: string[];
  error: Error;
}

export type RemoveResult = RemoveSuccess | RemoveFailure;

// ---------------------------------------------------------------------------
// configure
// ---------------------------------------------------------------------------

/** Action applied to a set of MCP features (tools, prompts, or resources). */
export interface FeatureAction {
  /** Explicit names to enable. */
  enableNames?: string[];
  /** Explicit names to disable. */
  disableNames?: string[];
  /** Enable every feature already present in the config. */
  enableAll?: boolean;
  /** Disable every feature already present in the config. */
  disableAll?: boolean;
}

export interface ConfigureOptions {
  unityProjectPath: string;
  /** Whether to apply changes to tools. Omit to leave tools untouched. */
  tools?: FeatureAction;
  prompts?: FeatureAction;
  resources?: FeatureAction;
  onProgress?: ProgressCallback;
}

export interface McpFeatureSnapshot {
  name: string;
  enabled: boolean;
}

export interface ConfigureSnapshot {
  host?: string;
  keepConnected?: boolean;
  transportMethod?: string;
  authOption?: string;
  tools: McpFeatureSnapshot[];
  prompts: McpFeatureSnapshot[];
  resources: McpFeatureSnapshot[];
}

export interface ConfigureSuccess {
  kind: 'success';
  success: true;
  /** Absolute path to the `AI-Game-Developer-Config.json` that was written. */
  configPath: string;
  /** A read-only snapshot of the post-write config. */
  snapshot: ConfigureSnapshot;
  warnings: string[];
}

export interface ConfigureFailure {
  kind: 'failure';
  success: false;
  warnings: string[];
  error: Error;
}

export type ConfigureResult = ConfigureSuccess | ConfigureFailure;

// ---------------------------------------------------------------------------
// setup-mcp
// ---------------------------------------------------------------------------

export type McpTransport = 'stdio' | 'http';

export interface SetupMcpOptions {
  /**
   * Agent to configure. Use `listAgentIds()` to discover valid values
   * (e.g. `'claude-code'`, `'cursor'`, `'codex'`, …).
   */
  agentId: string;
  /** Optional Unity project path. Defaults to `process.cwd()` if omitted. */
  unityProjectPath?: string;
  /** Transport to write — defaults to `'http'`. */
  transport?: McpTransport;
  /** Explicit server URL override (for http transport). */
  url?: string;
  /** Auth token override. */
  token?: string;
  onProgress?: ProgressCallback;
}

export interface SetupMcpSuccess {
  kind: 'success';
  success: true;
  /** The agent whose config file was written. */
  agentId: string;
  /** Absolute path to the agent config file that was written. */
  configPath: string;
  /** Transport actually written. */
  transport: McpTransport;
  warnings: string[];
  nextSteps: string[];
}

export interface SetupMcpFailure {
  kind: 'failure';
  success: false;
  warnings: string[];
  nextSteps: string[];
  error: Error;
}

export type SetupMcpResult = SetupMcpSuccess | SetupMcpFailure;

// ---------------------------------------------------------------------------
// open-project
// ---------------------------------------------------------------------------

/** Auth option propagated to the Editor as `UNITY_MCP_AUTH_OPTION`. */
export type OpenProjectAuthOption = 'none' | 'required';

/** Transport propagated to the Editor as `UNITY_MCP_TRANSPORT`. */
export type OpenProjectTransport = 'streamableHttp' | 'stdio';

export interface OpenProjectOptions {
  /**
   * Path to the Unity project to open. Absolute or relative; defaults
   * to `process.cwd()` if omitted.
   */
  projectPath?: string;
  /** Specific Unity Editor version to use (e.g. `"2022.3.62f3"`). */
  unityVersion?: string;
  /**
   * If `true`, skip wiring the MCP connection environment variables
   * onto the spawned editor process. Mirrors the CLI's `--no-connect`
   * flag semantics. Defaults to `false`.
   */
  noConnect?: boolean;
  /** MCP server URL — sets `UNITY_MCP_HOST` on the editor process. */
  url?: string;
  /** Auth token — sets `UNITY_MCP_TOKEN` on the editor process. */
  token?: string;
  /** Auth option — sets `UNITY_MCP_AUTH_OPTION` on the editor process. */
  auth?: OpenProjectAuthOption;
  /** Comma-separated list of tool names — sets `UNITY_MCP_TOOLS`. */
  tools?: string;
  /**
   * If `true`, sets `UNITY_MCP_KEEP_CONNECTED=true`. Auto-enabled by
   * Cloud-mode auto-detection when a `cloudToken` is present in the
   * project's config.
   */
  keepConnected?: boolean;
  /** Transport — sets `UNITY_MCP_TRANSPORT`. */
  transport?: OpenProjectTransport;
  /**
   * If set, sets `UNITY_MCP_START_SERVER=true|false`. Use a boolean to
   * avoid the CLI's stringly-typed `"true"`/`"false"` parse step.
   */
  startServer?: boolean;
  /**
   * If `true` (the default), poll for the Unity Editor's
   * "compile errors at launch" dialog after the editor process has
   * been spawned and click `Ignore` (or the platform-equivalent
   * button) so the editor finishes initialising. Set to `false` to
   * disable the polling loop entirely — corresponds to the CLI's
   * `--no-auto-dismiss-launch-errors` flag.
   *
   * The polling loop runs concurrently with the existing wait-for-
   * ready logic (which is the authoritative ready signal); when no
   * dialog appears, behaviour is unchanged from the pre-feature
   * baseline (no spurious clicks, no extra delay).
   */
  autoDismissLaunchErrors?: boolean;
  /**
   * Overall timeout (milliseconds) for the launch-errors dismissal
   * polling loop. The loop ticks every
   * `launchDismissPollIntervalMs` until either the dialog is
   * dismissed, this timeout elapses, or `openProject` returns. Default
   * `30000` (30 s).
   */
  launchDismissTimeoutMs?: number;
  /**
   * Polling tick interval (milliseconds) for the launch-errors
   * dismissal loop. Default `1500`.
   */
  launchDismissPollIntervalMs?: number;
  /**
   * Optional abort signal that, when fired, stops the launch-errors
   * dismissal polling loop early. Intended for callers that have an
   * authoritative "Unity is ready" signal in scope (e.g. a parallel
   * `wait-for-ready` poll) so the dismissal loop does not keep
   * ticking after Unity has finished initialising.
   *
   * When omitted, the loop falls back to a grace window after the
   * editor process is spawned: if no dialog has been observed within
   * ~15s of polling, the loop exits early on the assumption that the
   * dialog is not going to appear for this launch. The grace window
   * has to cover Unity's full startup phase (process spawn → Package
   * Manager connect → first compile pass) because the launch-errors
   * dialog (`"Enter Safe Mode?"` on Unity 2020.2+) appears at the end
   * of that phase, not the start of it (issue #737).
   */
  launchDismissAbortSignal?: AbortSignal;
  /**
   * Optional progress callback — fires for `start`,
   * `detecting-editor-version`, `editors-located`, `editor-resolved`,
   * `connection-details`, `launching-editor`, `editor-launched`,
   * `launch-errors-dismissed` (only when a dialog was actually
   * dismissed), and `done`.
   */
  onProgress?: ProgressCallback;
}

/** Successful `openProject` outcome. Narrow with `kind === 'success'`. */
export interface OpenProjectSuccess {
  kind: 'success';
  /** Always `true` for the success variant. */
  success: true;
  /** Absolute path to the Unity Editor binary that was launched. */
  editorPath: string;
  /**
   * PID of the spawned editor process. May be `undefined` if the OS
   * has not yet reported a PID by the time the call returns (rare;
   * the value is captured asynchronously from the child process's
   * `spawn` event).
   */
  editorPid?: number;
  /** Editor version string used (e.g. `"2022.3.62f3"`), if known. */
  unityVersion?: string;
  /** Resolved absolute project path. */
  projectPath: string;
  /** Non-fatal warnings collected during the run. */
  warnings: string[];
  /**
   * `true` when an existing Unity Editor process was already running
   * with this project and a launch was therefore skipped. The
   * `editorPid` will be the existing process's PID in that case.
   */
  alreadyRunning?: boolean;
}

/** Failed `openProject` outcome. Narrow with `kind === 'failure'`. */
export interface OpenProjectFailure {
  kind: 'failure';
  /** Always `false` for the failure variant. */
  success: false;
  /** Resolved absolute project path, if it could be determined. */
  projectPath?: string;
  /** Editor path, if locating the editor succeeded. */
  editorPath?: string;
  /** Editor version, if it could be detected before failure. */
  unityVersion?: string;
  /** Non-fatal warnings collected before the failure. */
  warnings: string[];
  /** Human-readable error message — never thrown past the public boundary. */
  errorMessage: string;
  /** The captured error. */
  error: Error;
}

export type OpenProjectResult = OpenProjectSuccess | OpenProjectFailure;

/**
 * Subset of `OpenProjectOptions` consumed by `buildOpenEnv` — every
 * field on this interface is potentially mapped to a `UNITY_MCP_*`
 * environment variable. Declared as a dedicated interface (rather
 * than a `Pick<OpenProjectOptions, …>` re-listed inline) so adding a
 * new env-bearing option to `OpenProjectOptions` is a one-step change
 * here that `buildOpenEnv` picks up by signature, with no risk of the
 * Pick list silently drifting.
 */
export interface OpenEnvInputs {
  noConnect?: OpenProjectOptions['noConnect'];
  url?: OpenProjectOptions['url'];
  token?: OpenProjectOptions['token'];
  auth?: OpenProjectOptions['auth'];
  tools?: OpenProjectOptions['tools'];
  keepConnected?: OpenProjectOptions['keepConnected'];
  transport?: OpenProjectOptions['transport'];
  startServer?: OpenProjectOptions['startServer'];
}
