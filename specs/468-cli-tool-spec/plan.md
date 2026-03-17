# Implementation Plan: Unity-MCP CLI Tool

**Branch**: `468-cli-tool-spec` | **Date**: 2026-03-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/468-cli-tool-spec/spec.md`

## Summary

Refactor and enhance the existing `unity-mcp-cli` Node.js CLI tool. Key changes: merge `open` and `connect` commands into a single `open` command with `--no-connect` flag, add TTY auto-detection for CI-safe output, replace silent network fallbacks with explicit error handling, add global `--verbose` flag, and increase test coverage from ~65% to 80%+.

## Technical Context

**Language/Version**: TypeScript 5.8+ compiled to ES modules, Node.js ^20.19.0 || >=22.12.0
**Primary Dependencies**: commander ^13.1.0 (CLI framework), chalk ^5.6.2 (terminal styling), yocto-spinner ^1.1.0 (progress spinners)
**Storage**: File system only — reads/writes `manifest.json`, `ProjectVersion.txt`, `AI-Game-Developer-Config.json`
**Testing**: vitest ^3.1.0 (unit + integration tests)
**Target Platform**: Cross-platform CLI (Windows, macOS, Linux)
**Project Type**: CLI tool
**Performance Goals**: All commands provide visual feedback within 1 second of invocation
**Constraints**: Zero known vulnerabilities in dependency tree, auto-detect TTY for CI compatibility
**Scale/Scope**: Single-user CLI tool, 7 source files + 6 utility modules, ~2000 LOC total

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applicable | Status | Notes |
|-----------|-----------|--------|-------|
| I. Main-Thread Safety | No | N/A | Node.js CLI, not Unity Plugin |
| II. Nullable & Type Safety | Partial | ✅ | TypeScript strict mode enforces null safety |
| III. Structured Tool Returns | No | N/A | CLI tool, not MCP tool |
| IV. Naming Conventions | Yes | ✅ | Commands use kebab-case (`install-plugin`, `create-project`) |
| V. Test-First Development | Yes | ⚠️ | Currently ~65% coverage, must reach 80% |
| VI. Immutability | Yes | ✅ | Config/manifest operations create new objects |
| VII. Security By Default | Yes | ✅ | No hardcoded secrets; inputs validated; errors don't leak paths |
| VIII. No Reflection | No | N/A | TypeScript/Node.js |
| IX. Mandatory Tool Input | No | N/A | CLI tool, not MCP tool |
| Deterministic Port | Yes | ✅ | SHA256-based port in `port.ts`, range 20000-29999 |
| Three-Tier Architecture | Yes | ✅ | CLI interacts with server tier, doesn't bypass |
| File Size Discipline | Yes | ✅ | All source files under 300 lines |

**Gate Result**: PASS — no violations. Test coverage (V) is a gap to address during implementation.

## Project Structure

### Documentation (this feature)

```text
specs/468-cli-tool-spec/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── cli-commands.md  # CLI command interface contracts
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
cli/
├── bin/
│   └── unity-mcp-cli.js          # Entry point (shebang + imports dist/index.js)
├── src/
│   ├── index.ts                  # Main CLI setup — command registration
│   ├── commands/
│   │   ├── create-project.ts     # create-project command
│   │   ├── install-unity.ts      # install-unity command
│   │   ├── open.ts               # open command (merged open + connect)
│   │   ├── install-plugin.ts     # install-plugin command
│   │   ├── remove-plugin.ts      # remove-plugin command
│   │   └── configure.ts          # configure command
│   └── utils/
│       ├── ui.ts                 # UI rendering (chalk + yocto-spinner + TTY detection)
│       ├── config.ts             # AI-Game-Developer-Config.json management
│       ├── manifest.ts           # Packages/manifest.json management
│       ├── port.ts               # Deterministic port generation (SHA256)
│       ├── unity-editor.ts       # Editor discovery & launch
│       └── unity-hub.ts          # Unity Hub CLI integration
├── tests/
│   ├── cli.test.ts               # Integration tests (all commands)
│   ├── manifest.test.ts          # Manifest operations
│   ├── config.test.ts            # Config operations
│   ├── port.test.ts              # Port generation
│   ├── unity-editor.test.ts      # Editor version parsing
│   ├── unity-hub.test.ts         # Hub version validation
│   ├── ui.test.ts                # NEW: TTY detection, verbose flag
│   └── open.test.ts              # NEW: Merged open command tests
├── package.json
└── tsconfig.json
```

**Structure Decision**: Existing single-project structure is appropriate. No new directories needed. The `connect.ts` file will be removed after merging into `open.ts`.

## Complexity Tracking

No constitution violations to justify — all gates pass.
