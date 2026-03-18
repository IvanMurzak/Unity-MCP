# Quickstart: Unity-MCP CLI Tool

**Branch**: `468-cli-tool-spec` | **Date**: 2026-03-14

## Prerequisites

- Node.js ^20.19.0 or >=22.12.0
- npm (comes with Node.js)

## Setup

```bash
# Clone and navigate to CLI directory
cd Unity-MCP/cli

# Install dependencies
npm install

# Build TypeScript
npm run build

# Run tests
npm test
```

## Development Workflow

### 1. Make changes

Edit files in `cli/src/`. Key directories:
- `src/commands/` — One file per CLI command
- `src/utils/` — Shared utilities (UI, manifest, config, port, Unity Hub/Editor)

### 2. Build and test

```bash
npm run build && npm test
```

### 3. Run locally

```bash
# Via node directly
node bin/unity-mcp-cli.js --help

# Or link globally for development
npm link
unity-mcp-cli --help
```

## Key Implementation Files

| File | Purpose | Change Required |
|------|---------|-----------------|
| `src/index.ts` | Command registration | Remove `connect` import, add `--verbose` global option |
| `src/commands/open.ts` | Open command | Merge `connect.ts` options, add `--no-connect` flag |
| `src/commands/connect.ts` | Connect command | DELETE — merged into `open.ts` |
| `src/utils/ui.ts` | UI rendering | Add TTY detection, verbose output function |
| `src/utils/manifest.ts` | Manifest management | Remove `FALLBACK_VERSION`, throw on network failure |

## Testing Approach

```bash
# Run all tests
npm test

# Run specific test file
npx vitest run tests/manifest.test.ts

# Watch mode for development
npm run test:watch
```

### Test categories
- **Unit tests**: manifest operations, config operations, port generation, version parsing
- **Integration tests**: full command execution via subprocess (`cli.test.ts`)
- **New tests needed**: TTY detection (`ui.test.ts`), merged open command (`open.test.ts`)

## Publishing

```bash
# Audit dependencies first
npm audit

# Publish to NPMJS
npm publish
```
