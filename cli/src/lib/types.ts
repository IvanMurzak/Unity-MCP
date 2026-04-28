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

export interface InstallResult {
  /** `true` when the manifest was updated (or already correct); `false` on error. */
  success: boolean;
  /** Final plugin version in the manifest. Populated on success. */
  installedVersion?: string;
  /** Absolute path to the manifest.json that was inspected / written. */
  manifestPath?: string;
  /** Non-fatal warnings collected during the run (e.g. skipped downgrade). */
  warnings: string[];
  /** Suggested next steps for the caller to surface to a human user. */
  nextSteps: string[];
  /** Populated when `success === false`. Never thrown past this boundary. */
  error?: Error;
}

// ---------------------------------------------------------------------------
// remove-plugin
// ---------------------------------------------------------------------------

export interface RemovePluginOptions {
  unityProjectPath: string;
  onProgress?: ProgressCallback;
}

export interface RemoveResult {
  success: boolean;
  /** `true` when the plugin dependency was present and has been removed. */
  removed?: boolean;
  manifestPath?: string;
  warnings: string[];
  error?: Error;
}

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

export interface ConfigureResult {
  success: boolean;
  /** Absolute path to the `AI-Game-Developer-Config.json` that was written. */
  configPath?: string;
  /** A read-only snapshot of the post-write config. */
  snapshot?: {
    host?: string;
    keepConnected?: boolean;
    transportMethod?: string;
    authOption?: string;
    tools: McpFeatureSnapshot[];
    prompts: McpFeatureSnapshot[];
    resources: McpFeatureSnapshot[];
  };
  warnings: string[];
  error?: Error;
}

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

export interface SetupMcpResult {
  success: boolean;
  /** The agent whose config file was written (undefined on error). */
  agentId?: string;
  /** Absolute path to the agent config file that was written. */
  configPath?: string;
  /** Transport actually written. */
  transport?: McpTransport;
  warnings: string[];
  nextSteps: string[];
  error?: Error;
}
