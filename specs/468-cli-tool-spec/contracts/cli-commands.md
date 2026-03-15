# CLI Command Contracts: Unity-MCP CLI Tool

**Branch**: `468-cli-tool-spec` | **Date**: 2026-03-14

## Global Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--version` | flag | — | Display CLI version |
| `--verbose` | flag | false | Enable detailed diagnostic output |
| `--help` | flag | — | Display help for command |

## Commands

### `install-unity [version]`

Install Unity Editor via Unity Hub.

| Argument/Option | Type | Required | Default | Description |
|-----------------|------|----------|---------|-------------|
| `version` | positional | No | Latest stable | Unity version to install (e.g., `2022.3.10f1`) |
| `--path <path>` | string | No | — | Read version from existing project's ProjectVersion.txt |

**Exit codes**: 0 = success, 1 = error (Hub not found, version invalid, network failure)

**Stdout** (TTY): Spinner during installation, success/error message
**Stdout** (non-TTY): Plain text progress, success/error message

---

### `create-project <path>`

Create a new Unity project.

| Argument/Option | Type | Required | Default | Description |
|-----------------|------|----------|---------|-------------|
| `path` | positional | Yes | — | Directory path for new project |
| `--unity <version>` | string | No | Latest installed | Unity version to use |

**Exit codes**: 0 = success, 1 = error (path exists, no editor found)

**Stdout** (TTY): Spinner during creation, success/error message
**Stdout** (non-TTY): Plain text status, success/error message

---

### `open <path>`

Open a Unity project in the editor. By default, sets MCP connection environment variables.

| Argument/Option | Type | Required | Default | Description |
|-----------------|------|----------|---------|-------------|
| `path` | positional | Yes | — | Unity project path |
| `--unity <version>` | string | No | From ProjectVersion.txt | Unity version to use |
| `--no-connect` | flag | No | false | Open without MCP connection env vars |
| `--url <url>` | string | No | — | MCP server URL |
| `--tools <names>` | string | No | — | Comma-separated tool names |
| `--token <token>` | string | No | — | Authentication token |
| `--auth <option>` | enum | No | — | `none` or `required` |
| `--keep-connected` | flag | No | false | Maintain persistent connection |
| `--transport <method>` | enum | No | — | `streamableHttp` or `stdio` |
| `--start-server <value>` | enum | No | — | `true` or `false` |

**Behavior**:
- Without `--no-connect`: Sets `UNITY_MCP_*` env vars if connection params are provided
- With `--no-connect`: Opens project without any MCP environment variables
- Resolves Unity version from ProjectVersion.txt if `--unity` not specified

**Exit codes**: 0 = success, 1 = error (project not found, editor not found)

---

### `install-plugin <path>`

Install Unity-MCP plugin into a project.

| Argument/Option | Type | Required | Default | Description |
|-----------------|------|----------|---------|-------------|
| `path` | positional | Yes | — | Unity project path |
| `--plugin-version <version>` | string | No | Latest from OpenUPM | Plugin version to install |

**Behavior**:
- Resolves latest version from OpenUPM (fails with error if network unavailable and no `--plugin-version` specified)
- Adds scoped registry for OpenUPM if not present
- Never downgrades an existing installation

**Exit codes**: 0 = success, 1 = error (network failure without manual version, manifest not found)

---

### `remove-plugin <path>`

Remove Unity-MCP plugin from a project.

| Argument/Option | Type | Required | Default | Description |
|-----------------|------|----------|---------|-------------|
| `path` | positional | Yes | — | Unity project path |

**Behavior**:
- Removes plugin from manifest dependencies
- Preserves scoped registries and other package scopes
- Informs user if plugin is not installed

**Exit codes**: 0 = success, 1 = error (manifest not found)

---

### `configure <path>`

Configure MCP tools, prompts, and resources.

| Argument/Option | Type | Required | Default | Description |
|-----------------|------|----------|---------|-------------|
| `path` | positional | Yes | — | Unity project path |
| `--list` | flag | No | false | Display current configuration |
| `--enable-tools <names>` | string | No | — | Comma-separated tool names to enable |
| `--disable-tools <names>` | string | No | — | Comma-separated tool names to disable |
| `--enable-all-tools` | flag | No | false | Enable all tools |
| `--disable-all-tools` | flag | No | false | Disable all tools |
| `--enable-prompts <names>` | string | No | — | Comma-separated prompt names to enable |
| `--disable-prompts <names>` | string | No | — | Comma-separated prompt names to disable |
| `--enable-all-prompts` | flag | No | false | Enable all prompts |
| `--disable-all-prompts` | flag | No | false | Disable all prompts |
| `--enable-resources <names>` | string | No | — | Comma-separated resource names to enable |
| `--disable-resources <names>` | string | No | — | Comma-separated resource names to disable |
| `--enable-all-resources` | flag | No | false | Enable all resources |
| `--disable-all-resources` | flag | No | false | Disable all resources |

**Exit codes**: 0 = success, 1 = error (project not found)

## Error Message Contract

All errors follow this structure:

```text
✖ [Error summary]

  [Detailed explanation]

  Suggestion: [Actionable next step]
```

In non-TTY mode, the Unicode symbols are replaced with plain text prefixes:
```text
ERROR: [Error summary]
[Detailed explanation]
Suggestion: [Actionable next step]
```

## Verbose Output Contract

When `--verbose` is enabled, diagnostic lines are prefixed:
```text
[verbose] Resolved Unity path: /path/to/editor
[verbose] Detected project version: 2022.3.10f1
[verbose] Setting UNITY_MCP_URL=http://localhost:51234
```
