/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Runtime.InteropServices;
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginSkillType]
    public static class Skill_InitialSetup
    {
        public const string SkillId = "unity-initial-setup";

        [McpPluginSkill(SkillId,
@"Provides an initial setup for AI Skills, `unity-mcp-cli` command line tool installation
and everything else that is helpful to set up at the start of the project. Essential packages,
and basic configurations.")]
        public static string Markdown
        {
            get
            {
                var nodeInstall = GetNodeInstallInstructions();
                var globalInstallNote = GetGlobalInstallNote();

                return $@"
# AI Game Developer — Initial Setup

This guide walks through installing the `unity-mcp-cli` command-line tool and using it to set
up a Unity project with AI Skills and MCP integration.

---

## Prerequisites

### Install Node.js

`unity-mcp-cli` requires **Node.js ^20.19.0 || >=22.12.0** (Node 21.x is not supported). If you don't have Node.js installed:

{nodeInstall}

After installation, verify both `node` and `npm` are available:
```
node --version
npm --version
```

---

## Install `unity-mcp-cli` Globally

Install the CLI globally so the `unity-mcp-cli` command is available system-wide:

```bash
npm install -g unity-mcp-cli
```

Verify installation:
```bash
unity-mcp-cli --version
```
{globalInstallNote}

> **Alternative**: Run any command without installing globally using `npx`:
> ```bash
> npx unity-mcp-cli --help
> ```

---

## Quick Start — Full Project Setup

### 1. Install the Unity-MCP Plugin

Add the plugin to an existing Unity project:
```bash
unity-mcp-cli install-plugin /path/to/unity/project
```

### 2. Configure MCP Tools

Enable all tools, prompts, and resources:
```bash
unity-mcp-cli configure /path/to/unity/project \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

### 3. Set Up MCP for Your AI Agent

Configure MCP integration for your AI agent (e.g., Claude Code, Cursor, Copilot):
```bash
unity-mcp-cli setup-mcp claude-code /path/to/unity/project
```

List all supported agents:
```bash
unity-mcp-cli setup-mcp --list
```

### 4. Generate AI Skills

Generate skill files for your AI agent (requires Unity Editor to be running with the plugin):
```bash
unity-mcp-cli setup-skills claude-code /path/to/unity/project
```

### 5. Open Unity with MCP Connection

```bash
unity-mcp-cli open /path/to/unity/project
```

---

## Common Commands Reference

| Command | Description |
|---|---|
| `unity-mcp-cli install-plugin <path>` | Install Unity-MCP plugin into a project |
| `unity-mcp-cli remove-plugin <path>` | Remove Unity-MCP plugin from a project |
| `unity-mcp-cli configure <path> --list` | List current MCP configuration |
| `unity-mcp-cli setup-mcp <agent> <path>` | Write MCP config for an AI agent |
| `unity-mcp-cli setup-skills <agent> <path>` | Generate skill files for an AI agent |
| `unity-mcp-cli create-project <path>` | Create a new Unity project |
| `unity-mcp-cli install-unity <version>` | Install a Unity Editor version |
| `unity-mcp-cli open <path>` | Open a Unity project in the Editor |
| `unity-mcp-cli run-tool <tool> <path>` | Execute an MCP tool via HTTP API |

Add `--verbose` to any command for detailed diagnostic output.

---

## Troubleshooting

- **`npm` not found**: Node.js is not installed or not in your PATH. Reinstall Node.js and restart your terminal.
- **Plugin not appearing in Unity**: After `install-plugin`, open the project in Unity Editor. The package manager resolves dependencies on project open.
- **Skills generation fails**: Ensure Unity Editor is running with the MCP plugin installed and connected before running `setup-skills`.
";
            }
        }

        static string GetNodeInstallInstructions()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return @"Download the installer from https://nodejs.org/ and run it, or use a package manager:
```
winget install OpenJS.NodeJS.LTS
```";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return @"Use Homebrew:
```
brew install node@22
```

Or download the installer from https://nodejs.org/.";
            }

            // Linux
            return @"Install via package manager:
```
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt-get install -y nodejs
```

Or download the binary from https://nodejs.org/.";
        }

        static string GetGlobalInstallNote()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "";
            }

            // macOS and Linux may need permission workaround
            return @"
> **Permission errors?** Use `sudo npm install -g unity-mcp-cli` or configure npm to use a directory you own:
> ```bash
> npm config set prefix ~/.npm-global
> ```
> Then add `~/.npm-global/bin` to your PATH.";
        }
    }
}
