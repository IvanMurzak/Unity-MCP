# Tasks: Unity-MCP CLI Tool

**Input**: Design documents from `/specs/468-cli-tool-spec/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/cli-commands.md

**Tests**: Included per constitution Principle V (Test-First Development, 80% coverage target).

**Organization**: Tasks grouped by user story. This is a refactor of an existing CLI — tasks focus on changes needed, not greenfield implementation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Dependency audit and project validation

- [x] T001 Run `npm audit` in `cli/` and verify zero known vulnerabilities in dependency tree
- [x] T002 Verify Node.js engine constraint in `cli/package.json` matches `^20.19.0 || >=22.12.0`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-cutting infrastructure changes that MUST be complete before user story work

**CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundational

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T003 [P] Write TTY detection tests in `cli/tests/ui.test.ts` — verify `startSpinner()` returns no-op when `process.stdout.isTTY` is false, verify `createProgressBar()` outputs plain text in non-TTY mode
- [x] T004 [P] Write verbose output tests in `cli/tests/ui.test.ts` — verify `verbose()` function only outputs when verbose mode is enabled, verify `[verbose]` prefix format

### Implementation for Foundational

- [x] T005 Add TTY auto-detection to `cli/src/utils/ui.ts` — wrap `startSpinner()` to return a no-op spinner object when `!process.stdout.isTTY`, make `createProgressBar()` output plain text percentage in non-TTY mode
- [x] T006 Add `verbose()` function to `cli/src/utils/ui.ts` — create module-level `verboseEnabled` flag, `setVerbose(enabled: boolean)` setter, and `verbose(message: string)` that outputs `[verbose] {message}` only when enabled
- [x] T007 Add global `--verbose` option to root program in `cli/src/index.ts` — add `.option('-v, --verbose', 'Enable verbose output')`, call `setVerbose(true)` from `ui.ts` when flag is set, wire into command pre-action hook
- [x] T008 Remove `FALLBACK_VERSION` constant and hardcoded fallback from `cli/src/utils/manifest.ts` — change `resolveLatestVersion()` to throw an error with message suggesting retry or `--plugin-version` flag on network failure instead of returning fallback

**Checkpoint**: Foundation ready — TTY detection, verbose flag, and network error handling in place

---

## Phase 3: User Story 1 - Install Unity Editor (Priority: P1) MVP

**Goal**: A developer can install a specific Unity Editor version via a single CLI command

**Independent Test**: Run `unity-mcp-cli install-unity 2022.3.10f1` and verify the editor appears in installed editors list

### Tests for User Story 1

- [x] T009 [P] [US1] Write unit tests for `isValidUnityVersion()` edge cases in `cli/tests/unity-hub.test.ts` — test alpha/beta versions, malformed strings, empty input
- [x] T010 [P] [US1] Write integration test for `install-unity --help` output in `cli/tests/cli.test.ts` — verify help shows version argument and `--path` option

### Implementation for User Story 1

- [x] T011 [US1] Add verbose logging to `cli/src/commands/install-unity.ts` — log resolved version, Unity Hub path, installation progress via `verbose()`
- [x] T012 [US1] Add verbose logging to `cli/src/utils/unity-hub.ts` — log Hub discovery path, installed editors list, download URLs via `verbose()`

**Checkpoint**: install-unity command fully functional with verbose support and tests

---

## Phase 4: User Story 2 - Create Unity Project (Priority: P1)

**Goal**: A developer can create a new Unity project at a specified path using a specified editor version

**Independent Test**: Run `unity-mcp-cli create-project ./test-project --unity 2022.3.10f1` and verify project directory contains `Assets/`, `ProjectSettings/`

### Tests for User Story 2

- [x] T013 [P] [US2] Write integration test for `create-project --help` output in `cli/tests/cli.test.ts` — verify help shows path argument and `--unity` option
- [x] T014 [P] [US2] Write unit test for project path validation in `cli/tests/unity-editor.test.ts` — verify existing project detection, invalid path handling

### Implementation for User Story 2

- [x] T015 [US2] Add verbose logging to `cli/src/commands/create-project.ts` — log resolved editor path, target project directory, Unity version used via `verbose()`

**Checkpoint**: create-project command fully functional with verbose support and tests

---

## Phase 5: User Story 3 - Install Plugin (Priority: P1)

**Goal**: A developer can inject the Unity-MCP plugin into an existing Unity project's package manifest

**Independent Test**: Run `unity-mcp-cli install-plugin ./my-project` and verify plugin appears in `Packages/manifest.json`

### Tests for User Story 3

- [x] T016 [P] [US3] Write unit test for network failure handling in `cli/tests/manifest.test.ts` — verify `resolveLatestVersion()` throws with actionable error message when network unavailable (not silent fallback)
- [x] T017 [P] [US3] Write unit test for `--plugin-version` manual override in `cli/tests/manifest.test.ts` — verify manual version bypasses network lookup

### Implementation for User Story 3

- [x] T018 [US3] Update error handling in `cli/src/commands/install-plugin.ts` — catch `resolveLatestVersion()` error, display via `ui.error()` with suggestion to use `--plugin-version`
- [x] T019 [US3] Add verbose logging to `cli/src/commands/install-plugin.ts` — log OpenUPM request URL, resolved version, manifest changes via `verbose()`

**Checkpoint**: install-plugin command fails explicitly on network errors, supports verbose, has tests

---

## Phase 6: User Story 4 - Remove Plugin (Priority: P2)

**Goal**: A developer can cleanly remove the Unity-MCP plugin from a project without breaking other packages

**Independent Test**: Run `unity-mcp-cli remove-plugin ./my-project` on a project with plugin installed and verify plugin removed from `manifest.json` while other packages remain

### Tests for User Story 4

- [x] T020 [P] [US4] Write unit test for scoped registry preservation in `cli/tests/manifest.test.ts` — verify removing plugin preserves registries used by other packages
- [x] T021 [P] [US4] Write unit test for removing non-existent plugin in `cli/tests/manifest.test.ts` — verify informative message when plugin not installed

### Implementation for User Story 4

- [x] T022 [US4] Add verbose logging to `cli/src/commands/remove-plugin.ts` — log manifest path, current plugin version, registry preservation decisions via `verbose()`

**Checkpoint**: remove-plugin command fully functional with verbose support and tests

---

## Phase 7: User Story 5 - Open Unity Project (Priority: P2)

**Goal**: A developer can open a Unity project with a single `open` command that sets MCP env vars by default, with `--no-connect` to skip MCP connection

**Independent Test**: Run `unity-mcp-cli open ./my-project --url http://localhost:5000` and verify Unity Editor launches with `UNITY_MCP_URL` set; run with `--no-connect` and verify no MCP env vars

### Tests for User Story 5

- [x] T023 [P] [US5] Write unit tests for merged open command in `cli/tests/open.test.ts` — test option parsing: `--no-connect`, `--url`, `--tools`, `--token`, `--auth`, `--keep-connected`, `--transport`, `--start-server`
- [x] T024 [P] [US5] Write unit tests for environment variable mapping in `cli/tests/open.test.ts` — verify each option maps to correct `UNITY_MCP_*` env var, verify `--no-connect` suppresses all env vars
- [x] T025 [P] [US5] Write integration test for `open --help` output in `cli/tests/cli.test.ts` — verify help shows all options including `--no-connect`

### Implementation for User Story 5

- [x] T026 [US5] Merge `connect.ts` options into `cli/src/commands/open.ts` — add all connection options (`--url`, `--tools`, `--token`, `--auth`, `--keep-connected`, `--transport`, `--start-server`), add `--no-connect` flag, implement env var setup logic from `connect.ts`
- [x] T027 [US5] Remove `cli/src/commands/connect.ts` — delete file, remove import and registration from `cli/src/index.ts`
- [x] T028 [US5] Update integration tests in `cli/tests/cli.test.ts` — replace `connect` command tests with merged `open` command tests, add `--no-connect` test
- [x] T029 [US5] Add verbose logging to `cli/src/commands/open.ts` — log resolved editor path, project version, environment variables being set (mask token values) via `verbose()`

**Checkpoint**: Single `open` command replaces `open` + `connect`, with `--no-connect` flag, verbose support, and tests

---

## Phase 8: User Story 6 - Configure MCP Tools (Priority: P2)

**Goal**: A developer can enable/disable MCP tools, prompts, and resources via CLI without editing JSON manually

**Independent Test**: Run `unity-mcp-cli configure ./my-project --enable-tools tool1 --list` and verify configuration reflects changes

### Tests for User Story 6

- [x] T030 [P] [US6] Write unit tests for bulk enable/disable operations in `cli/tests/config.test.ts` — test `--enable-all-tools`, `--disable-all-tools`, `--enable-all-prompts`, `--disable-all-prompts`, `--enable-all-resources`, `--disable-all-resources`

### Implementation for User Story 6

- [x] T031 [US6] Add verbose logging to `cli/src/commands/configure.ts` — log config file path, changes being applied, final configuration state via `verbose()`

**Checkpoint**: configure command fully functional with verbose support and tests

---

## Phase 9: User Story 7 - Help Command (Priority: P3)

**Goal**: All commands listed alphabetically with descriptions when running `--help`

**Independent Test**: Run `unity-mcp-cli --help` and verify commands appear in alphabetical order

### Tests for User Story 7

- [x] T032 [US7] Write integration test for alphabetical command ordering in `cli/tests/cli.test.ts` — capture `--help` output, verify command names appear in sorted order

### Implementation for User Story 7

- [x] T033 [US7] Ensure alphabetical command registration in `cli/src/index.ts` — sort the `subcommands` array alphabetically before registering with `program.addCommand()`, verify `configureStyledHelp()` preserves sort order

**Checkpoint**: Help output displays all commands in alphabetical order

---

## Phase 10: User Story 8 - Fancy Terminal UI (Priority: P3)

**Goal**: Visual feedback during long-running operations with TTY-aware styled output

**Independent Test**: Run any long-running command in TTY and verify spinner/progress appears; run in non-TTY (piped) and verify plain text output

### Tests for User Story 8

- [x] T034 [US8] Write unit tests for non-TTY output format in `cli/tests/ui.test.ts` — verify success/error/info/warn messages use plain text prefixes (no Unicode symbols) when TTY is unavailable

### Implementation for User Story 8

- [x] T035 [US8] Update `success()`, `error()`, `info()`, `warn()` in `cli/src/utils/ui.ts` — use plain text prefixes (`SUCCESS:`, `ERROR:`, `INFO:`, `WARN:`) when `!process.stdout.isTTY`, keep Unicode symbols for TTY mode

**Checkpoint**: All UI output is CI/pipeline-safe in non-TTY mode while remaining styled in interactive terminals

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T036 Run full test suite via `npm test` in `cli/` and verify all tests pass
- [x] T037 Run `npm audit` in `cli/` and verify zero vulnerabilities
- [x] T038 [P] Update `cli/README.md` — document merged `open` command (replaces `open` + `connect`), document `--verbose` flag, document `--no-connect` flag
- [x] T039 [P] Update CLI translations in `cli/docs/README.es.md`, `cli/docs/README.ja.md`, `cli/docs/README.zh-CN.md` — sync with updated `cli/README.md`
- [x] T040 Verify test coverage meets 80% threshold via `npx vitest run --coverage` in `cli/`
- [x] T041 Run quickstart.md validation — follow steps in `specs/468-cli-tool-spec/quickstart.md` and verify all commands work

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Stories (Phase 3–10)**: All depend on Foundational phase completion
  - US1, US2, US3 (P1 stories) can proceed in parallel
  - US4, US5, US6 (P2 stories) can proceed in parallel after P1 or concurrently
  - US7, US8 (P3 stories) can proceed in parallel after P2 or concurrently
- **Polish (Phase 11)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (Install Unity)**: Independent — after Foundational
- **US2 (Create Project)**: Independent — after Foundational
- **US3 (Install Plugin)**: Independent — after Foundational (depends on T008 for network error handling)
- **US4 (Remove Plugin)**: Independent — after Foundational
- **US5 (Open Project)**: Independent — after Foundational (largest change: merge open+connect)
- **US6 (Configure)**: Independent — after Foundational
- **US7 (Help)**: Independent — after Foundational (should run after US5 since command list changes)
- **US8 (Fancy UI)**: Independent — after Foundational (depends on T005 for TTY detection)

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Implementation tasks in order listed
- Verbose logging last (after core logic)

### Parallel Opportunities

- T003, T004 (foundational tests) can run in parallel
- T005, T006 (foundational implementation) can run in parallel
- All P1 user stories (US1, US2, US3) can run in parallel after foundational
- All P2 user stories (US4, US5, US6) can run in parallel
- All P3 user stories (US7, US8) can run in parallel
- Within each story: test tasks marked [P] can run in parallel
- T038, T039 (docs) can run in parallel with T040 (coverage)

---

## Parallel Example: Foundational Phase

```bash
# Launch foundational tests in parallel:
Task: "Write TTY detection tests in cli/tests/ui.test.ts"
Task: "Write verbose output tests in cli/tests/ui.test.ts"

# After tests fail, launch implementations in parallel:
Task: "Add TTY auto-detection to cli/src/utils/ui.ts"
Task: "Add verbose() function to cli/src/utils/ui.ts"
```

## Parallel Example: P1 User Stories (after Foundational)

```bash
# Launch all P1 stories concurrently:
Task: "US1 — Install Unity Editor tests and implementation"
Task: "US2 — Create Unity Project tests and implementation"
Task: "US3 — Install Plugin tests and implementation"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003-T008) — CRITICAL
3. Complete Phase 3: User Story 1 (T009-T012)
4. **STOP and VALIDATE**: Run `npm test`, verify install-unity works with verbose
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 + US2 + US3 (P1) → Core workflows functional (MVP!)
3. Add US4 + US5 + US6 (P2) → Full lifecycle + merged open command
4. Add US7 + US8 (P3) → Polish and UX enhancements
5. Polish phase → Documentation, coverage, audit

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: US1 (Install Unity) + US4 (Remove Plugin)
   - Developer B: US2 (Create Project) + US5 (Open Project — largest change)
   - Developer C: US3 (Install Plugin) + US6 (Configure)
3. After P1+P2 complete: Anyone picks up US7 + US8

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- This is a refactor — most commands already exist; tasks focus on changes needed
- The largest single change is US5 (merging open + connect into one command)
- Constitution Principle V requires TDD — tests are included for all stories
- Run `npm test` after each phase checkpoint
