// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

/**
 * The AI-agent registry now lives in `@baizor/gamedev-cli-core` (auth-fixes T7 / D1): ONE
 * engine-neutral registry (agent id → config path, body path, format, per-transport prop builders)
 * shared by the three engine CLIs, with the server-entry name + stdio-args vector supplied by the
 * engine adapter. The registry's config WRITERS are the golden-vector-gated `JsonAiAgentConfig` /
 * `TomlAiAgentConfig` (byte-for-byte parity with the C# Editor Configure), reached through
 * cli-core's `setupMcp`.
 *
 * This module re-exports that registry and keeps the two Unity-CLI-local bits cli-core does not own:
 *   - `MCP_SERVER_NAME` — the Unity server-entry name (`ai-game-developer`), from `unityAdapter`.
 *   - `listAgentTable` — the terminal-table renderer used by `setup-mcp --list` / `setup-skills
 *     --list` (a CLI presentation concern, not shared library logic).
 */

import chalk from 'chalk';
import {
  agentRegistry,
  getAgentById,
  getAgentIds,
  unityAdapter,
  type AgentDefinition,
} from '@baizor/gamedev-cli-core';

export { agentRegistry, getAgentById, getAgentIds };
export type { AgentDefinition };

/** The Unity MCP server-entry name written under an agent config's body path. */
const MCP_SERVER_NAME = unityAdapter.serverName;
export { MCP_SERVER_NAME };

// ---------------------------------------------------------------------------
// Terminal table renderer (CLI presentation — not part of the shared library)
// ---------------------------------------------------------------------------

export function listAgentTable(
  heading: string,
  locationLabel: string,
  locationFn: (agent: AgentDefinition) => string,
): void {
  const sorted = [...agentRegistry].sort((a, b) => a.id.localeCompare(b.id));

  const colId = 'ID';
  const colLoc = locationLabel;

  const wId = Math.max(colId.length, ...sorted.map((a) => a.id.length));
  const wLoc = Math.max(colLoc.length, ...sorted.map((a) => locationFn(a).length));

  const sep = chalk.dim;
  const hBar = (w: number) => '─'.repeat(w);

  console.log(`\n${chalk.bold.cyan(heading)}\n`);

  // Header
  console.log(
    sep('  ┌─') + sep(hBar(wId)) + sep('─┬─') + sep(hBar(wLoc)) + sep('─┐'),
  );
  console.log(
    sep('  │ ') + chalk.bold.white(colId.padEnd(wId)) + sep(' │ ') + chalk.bold.white(colLoc.padEnd(wLoc)) + sep(' │'),
  );
  console.log(
    sep('  ├─') + sep(hBar(wId)) + sep('─┼─') + sep(hBar(wLoc)) + sep('─┤'),
  );

  // Rows
  for (const agent of sorted) {
    const loc = locationFn(agent);
    console.log(
      sep('  │ ') + chalk.yellow(agent.id.padEnd(wId)) + sep(' │ ') + chalk.green(loc.padEnd(wLoc)) + sep(' │'),
    );
  }

  // Footer
  console.log(
    sep('  └─') + sep(hBar(wId)) + sep('─┴─') + sep(hBar(wLoc)) + sep('─┘'),
  );
  console.log('');
}
