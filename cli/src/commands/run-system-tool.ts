import { runSystemTool } from '../lib/run-tool.js';
import { buildRunToolCommand } from './run-tool-builder.js';

export const runSystemToolCommand = buildRunToolCommand({
  name: 'run-system-tool',
  description: 'Execute a system tool via the HTTP API (not exposed to MCP clients)',
  argDescription: 'Name of the system tool to execute',
  routePrefix: '/api/system-tools',
  verboseLabel: 'System tool',
  headingLabel: 'Run System Tool',
  errorNoun: 'system tool',
  invoke: runSystemTool,
});
