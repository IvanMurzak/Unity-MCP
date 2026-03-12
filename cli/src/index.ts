import { Command } from 'commander';
import { createProjectCommand } from './commands/create-project.js';
import { installEditorCommand } from './commands/install-editor.js';
import { openCommand } from './commands/open.js';
import { installPluginCommand } from './commands/install-plugin.js';
import { configureCommand } from './commands/configure.js';
import { connectCommand } from './commands/connect.js';

const program = new Command();

program
  .name('unity-mcp')
  .description('Cross-platform CLI tool for Unity-MCP operations')
  .version('0.51.6');

program.addCommand(createProjectCommand);
program.addCommand(installEditorCommand);
program.addCommand(openCommand);
program.addCommand(installPluginCommand);
program.addCommand(configureCommand);
program.addCommand(connectCommand);

program.parse();
