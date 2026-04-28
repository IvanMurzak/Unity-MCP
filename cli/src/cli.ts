// CLI entry for `unity-mcp-cli`, exported at the `./cli` subpath.
//
// Importing this file runs the Commander-based CLI exactly as the
// `unity-mcp-cli` binary does — it parses argv, dispatches subcommands,
// and exits the process.
//
// Consumers that want the programmatic, side-effect-free surface
// should import the package root (`import { installPlugin } from
// "unity-mcp-cli"`) — NOT this file.

import './index.js';
