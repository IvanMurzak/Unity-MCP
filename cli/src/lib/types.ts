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
