# Research: Unity-MCP CLI Tool

**Branch**: `468-cli-tool-spec` | **Date**: 2026-03-14

## R1: TTY Auto-Detection for Spinners

**Decision**: Use `process.stdout.isTTY` to conditionally disable yocto-spinner and chalk styling in non-interactive environments.

**Rationale**: chalk v5 already auto-detects color support via `supports-color`, but yocto-spinner does not handle non-TTY gracefully — it outputs ANSI escape codes and control characters that corrupt CI logs. Wrapping spinner creation with a TTY check is the simplest and most reliable approach.

**Alternatives considered**:
- **`is-interactive` npm package**: Adds a dependency for a one-line check. Rejected — `process.stdout.isTTY` is sufficient.
- **`NO_COLOR` / `FORCE_COLOR` env vars**: chalk already respects these. Adding support for `NO_COLOR` to suppress spinners is a good complementary approach but TTY detection should be the primary mechanism.
- **Custom `--no-color` flag**: Adds explicit user control. Not needed as primary mechanism — TTY auto-detect covers 95% of cases. Could be added later if requested.

**Implementation approach**:
- In `ui.ts`, wrap `startSpinner()` to return a no-op spinner when `!process.stdout.isTTY`
- Progress bar should output plain text progress (e.g., `[50%] Installing...`) in non-TTY mode
- chalk auto-handles color stripping — no changes needed for colored output

## R2: Network Failure Error Handling (Replace Hardcoded Fallback)

**Decision**: Remove the `FALLBACK_VERSION` constant and hardcoded fallback in `resolveLatestVersion()`. On network failure, throw an error with an actionable message suggesting retry or manual `--plugin-version` flag.

**Rationale**: Silent fallback to a hardcoded version risks installing outdated or incompatible packages. The user has clarified this must fail explicitly (FR-018). The `--plugin-version` flag already exists for manual override.

**Alternatives considered**:
- **Fallback with prominent warning**: Less risky than silent fallback but still installs a potentially stale version. Rejected per user clarification.
- **Retry with exponential backoff**: Over-engineering for a CLI that the user can simply re-run. Rejected.
- **Cache last-known version locally**: Adds state management complexity. Rejected — the user can pass `--plugin-version` manually.

**Implementation approach**:
- Remove `FALLBACK_VERSION` constant from `manifest.ts`
- In `resolveLatestVersion()`, throw a descriptive error on network failure instead of returning fallback
- Error message should suggest: `"Failed to resolve latest plugin version from OpenUPM. Check your network connection and retry, or specify a version manually with --plugin-version <version>"`
- In `install-plugin.ts`, catch this error and display via `ui.error()`

## R3: Merging `open` and `connect` Commands

**Decision**: Merge `connect.ts` into `open.ts` as a single `open` command. By default, `open` sets MCP environment variables (current `connect` behavior). Add `--no-connect` flag to skip MCP env vars (current `open` behavior).

**Rationale**: The user explicitly requested a single command. The current `connect` command is a superset of `open` — it does everything `open` does plus sets env vars. Merging reduces command count and cognitive load.

**Alternatives considered**:
- **Keep separate with aliasing**: `connect` as alias for `open --connect`. Adds confusion. Rejected.
- **Make `connect` a subcommand**: `open connect`. Unusual pattern for CLIs. Rejected.

**Implementation approach**:
- Move all `connect.ts` options into `open.ts`: `--url`, `--tools`, `--token`, `--auth`, `--keep-connected`, `--transport`, `--start-server`
- Add `--no-connect` flag (boolean, default false) — when set, skips all MCP env var setup
- When `--no-connect` is NOT set AND connection params are provided, set `UNITY_MCP_*` env vars
- When `--no-connect` is NOT set AND no connection params are provided, still open normally (backward compatible)
- Remove `connect.ts` and its import/registration from `index.ts`
- Update `cli.test.ts` to test the merged command

## R4: Global `--verbose` Flag

**Decision**: Add a global `--verbose` option on the root Commander program that enables detailed diagnostic output across all commands.

**Rationale**: Users need troubleshooting capability when things go wrong (Unity Hub not found, wrong paths, version mismatches). A single global flag is the standard CLI convention.

**Alternatives considered**:
- **Per-command `--verbose`**: Inconsistent UX, requires adding to each command separately. Rejected.
- **`DEBUG` environment variable**: Standard in Node.js ecosystem but less discoverable. Could complement `--verbose` but shouldn't replace it.
- **Tiered logging (`--verbose` + `--debug`)**: Over-engineering for a tool with straightforward operations. Rejected.

**Implementation approach**:
- Add `.option('-v, --verbose', 'Enable verbose output')` to the root program in `index.ts`
- Create a `verbose()` function in `ui.ts` that only outputs when verbose mode is enabled
- Pass verbose state via Commander's `opts()` or a module-level flag set during initialization
- Verbose output includes: resolved paths, detected Unity versions, environment variables being set, command arguments parsed, network request URLs

## R5: Dependency Security Audit

**Decision**: The current dependency tree (chalk, commander, yocto-spinner) is minimal and well-maintained. No known vulnerabilities as of 2026-03-14.

**Rationale**: FR-013 requires zero known vulnerabilities. The 3 production dependencies are widely used, actively maintained packages with strong security track records.

**Implementation approach**:
- Run `npm audit` as part of CI pipeline
- Add `npm audit --audit-level=moderate` to prepublishOnly script
- Keep dependencies up to date with regular audits
