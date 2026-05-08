// Library entry point for `unity-mcp-cli`.
//
// Constraints (enforced by review — see issue #678):
// - NO top-level side effects. Importing this file must not open
//   sockets, spin up spinners, write to stdout/stderr, or parse argv.
// - NO `commander` import reachable from this file.
// - Every result is a discriminated union keyed on `kind`. Successes are
//   `{ kind: 'success', success: true, ...variant fields }`; failures are
//   `{ kind: 'failure', success: false, error }`. Errors are never thrown
//   past the public boundary. Narrow on `kind` for type-safe access to
//   variant-specific fields; the boolean `success` mirror is preserved
//   for wire compatibility (`success === (kind === 'success')`).
// - Progress is surfaced via an optional `onProgress` callback, not
//   globals or singletons.
//
// Consumers: `import { installPlugin } from 'unity-mcp-cli'` (maps to
// this file via the `exports` field in package.json).

export { installPlugin } from './lib/install-plugin.js';
export { removePlugin } from './lib/remove-plugin.js';
export { configure } from './lib/configure.js';
export { setupMcp, listAgentIds } from './lib/setup-mcp.js';
export { openProject } from './lib/open.js';

export type {
  // Shared
  ProgressEvent,
  ProgressCallback,
  ResultKind,
  // install-plugin
  InstallPluginOptions,
  InstallResult,
  InstallSuccess,
  InstallFailure,
  // remove-plugin
  RemovePluginOptions,
  RemoveResult,
  RemoveSuccess,
  RemoveFailure,
  // configure
  ConfigureOptions,
  ConfigureResult,
  ConfigureSuccess,
  ConfigureFailure,
  ConfigureSnapshot,
  FeatureAction,
  McpFeatureSnapshot,
  // setup-mcp
  SetupMcpOptions,
  SetupMcpResult,
  SetupMcpSuccess,
  SetupMcpFailure,
  McpTransport,
  // open-project
  OpenProjectOptions,
  OpenProjectResult,
  OpenProjectSuccess,
  OpenProjectFailure,
  OpenProjectAuthOption,
  OpenProjectTransport,
} from './lib/types.js';
