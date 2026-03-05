#!/usr/bin/env node

import { Command } from 'commander';
import { registerOpenCommand } from '../src/commands/open.js';
import { registerInstallMcpCommand } from '../src/commands/install-mcp.js';
import { registerSetFeaturesCommands } from '../src/commands/set-features.js';
import { registerConnectCommand } from '../src/commands/connect.js';

const program = new Command();

program
  .name('gamedev')
  .description('CLI tool for Unity project operations with Unity MCP integration.\n\nUsage with npx:  npx gamedev-cli <command> [options]\nUsage after global install:  gamedev <command> [options]')
  .version('1.0.0');

registerOpenCommand(program);
registerInstallMcpCommand(program);
registerSetFeaturesCommands(program);
registerConnectCommand(program);

program.parse();
