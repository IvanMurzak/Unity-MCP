import { runTool } from '../lib/run-tool.js';
import { buildRunToolCommand } from './run-tool-builder.js';

export const runToolCommand = buildRunToolCommand({
  name: 'run-tool',
  description: 'Execute an MCP tool via the HTTP API',
  argDescription: 'Name of the MCP tool to execute',
  routePrefix: '/api/tools',
  verboseLabel: 'Tool',
  headingLabel: 'Run Tool',
  errorNoun: 'tool',
  invoke: runTool,
});
