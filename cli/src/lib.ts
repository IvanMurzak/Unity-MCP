// Library entry point for `unity-mcp-cli`.
//
// Constraints (enforced by review — see issue #678):
// - NO top-level side effects. Importing this file must not open
//   sockets, spin up spinners, write to stdout/stderr, or parse argv.
// - NO `commander` import reachable from this file.
// - Errors are returned in `{ success: false, error }` results — never
//   thrown past the public boundary.
// - Progress is surfaced via an optional `onProgress` callback, not
//   globals or singletons.
//
// Consumers: `import { installPlugin } from 'unity-mcp-cli'` (maps to
// this file via the `exports` field in package.json).

export { installPlugin } from './lib/install-plugin.js';
export { removePlugin } from './lib/remove-plugin.js';
export { configure } from './lib/configure.js';
export { setupMcp, listAgentIds } from './lib/setup-mcp.js';

export type {
  // Shared
  ProgressEvent,
  ProgressCallback,
  // install-plugin
  InstallPluginOptions,
  InstallResult,
  // remove-plugin
  RemovePluginOptions,
  RemoveResult,
  // configure
  ConfigureOptions,
  ConfigureResult,
  FeatureAction,
  McpFeatureSnapshot,
  // setup-mcp
  SetupMcpOptions,
  SetupMcpResult,
  McpTransport,
} from './lib/types.js';
