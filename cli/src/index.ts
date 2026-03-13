import { Command } from 'commander';
import { createRequire } from 'module';
import { createProjectCommand } from './commands/create-project.js';
import { installEditorCommand } from './commands/install-editor.js';
import { openCommand } from './commands/open.js';
import { installPluginCommand } from './commands/install-plugin.js';
import { configureCommand } from './commands/configure.js';
import { connectCommand } from './commands/connect.js';
import { removePluginCommand } from './commands/remove-plugin.js';

const require = createRequire(import.meta.url);
const pkg = require('../package.json') as { version: string };

const program = new Command();

program
  .name('unity-mcp-cli')
  .description('Cross-platform CLI tool for Unity-MCP operations')
  .version(pkg.version);

program.addCommand(createProjectCommand);
program.addCommand(installEditorCommand);
program.addCommand(openCommand);
program.addCommand(installPluginCommand);
program.addCommand(configureCommand);
program.addCommand(connectCommand);
program.addCommand(removePluginCommand);

program.parseAsync().catch((error) => {
  console.error(error);
  process.exit(1);
});
