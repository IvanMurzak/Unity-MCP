# Feature Specification: Unity-MCP CLI Tool

**Feature Branch**: `468-cli-tool-spec`
**Created**: 2026-03-14
**Status**: Draft
**Input**: User description: "CLI tool for maintaining Unity projects and Unity-MCP plugin. Commands: install Unity Editor, create Unity project, inject/remove plugin, open editor with environment variables, configure tools, connect MCP, help with alphabetical listing. Fancy terminal UI with progress indicators. No vulnerable dependencies. Published to NPMJS."

## Clarifications

### Session 2026-03-14

- Q: Should the CLI auto-detect TTY or require an explicit flag to disable styled output? → A: Auto-detect TTY; disable spinners/colors when no interactive terminal is detected.
- Q: What should happen when network operations fail (OpenUPM lookup, Unity Hub download)? → A: Fail with a clear error message and suggest the user retry or provide the version manually. No silent fallbacks to hardcoded versions.
- Q: Should `open` and `connect` be separate commands or merged? → A: Merge into a single `open` command that connects with MCP env vars by default; add an optional flag (e.g., `--no-connect`) to open the project without MCP connection.
- Q: Should the CLI support verbose/debug logging? → A: Add a `--verbose` flag for detailed diagnostic output during execution.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Install Unity Editor (Priority: P1)

A developer wants to install a specific version of Unity Editor on their machine without manually navigating the Unity Hub UI. They run a single command specifying the desired version, and the tool handles downloading and installing through Unity Hub.

**Why this priority**: Installing Unity Editor is the prerequisite for all other operations. Without an editor installed, no project can be created or opened.

**Independent Test**: Can be fully tested by running the install command with a specific Unity version and verifying that the editor appears in the list of installed editors afterward.

**Acceptance Scenarios**:

1. **Given** a machine with Unity Hub installed, **When** the user runs the install command with a valid version (e.g., `2022.3.10f1`), **Then** the specified Unity Editor version is installed and available for use.
2. **Given** a machine without Unity Hub, **When** the user runs the install command, **Then** Unity Hub is automatically downloaded, installed, and then the requested editor version is installed.
3. **Given** a version that is already installed, **When** the user runs the install command, **Then** the tool informs the user the version is already present and exits gracefully.
4. **Given** no version is specified, **When** the user runs the install command, **Then** the latest stable Unity version is installed by default.

---

### User Story 2 - Create Unity Project (Priority: P1)

A developer wants to create a new Unity project using a specific Unity Editor version at a specified file system path, without opening the Unity Hub GUI.

**Why this priority**: Creating a project is the foundational action before plugin injection or configuration. Equally critical as editor installation.

**Independent Test**: Can be fully tested by running the create command with a path and version, then verifying a valid Unity project exists at the specified location.

**Acceptance Scenarios**:

1. **Given** a valid path and an installed Unity version, **When** the user runs the create-project command, **Then** a new Unity project is created at the specified path using the specified editor version.
2. **Given** a path that already contains a Unity project, **When** the user runs the create-project command, **Then** the tool warns the user and does not overwrite the existing project.
3. **Given** a Unity version that is not installed, **When** the user runs the create-project command without specifying a version, **Then** the tool uses the latest available installed editor version.

---

### User Story 3 - Install Plugin into Unity Project (Priority: P1)

A developer wants to inject the Unity-MCP plugin into an existing Unity project so the project gains MCP integration capabilities.

**Why this priority**: Plugin installation is the core value proposition of the CLI — connecting Unity projects to MCP infrastructure.

**Independent Test**: Can be fully tested by running the install-plugin command on a Unity project and verifying the plugin appears in the project's package manifest.

**Acceptance Scenarios**:

1. **Given** a valid Unity project path, **When** the user runs the install-plugin command, **Then** the Unity-MCP plugin is added to the project's `Packages/manifest.json` with the latest version from OpenUPM.
2. **Given** a project that already has the plugin installed at an older version, **When** the user runs the install-plugin command, **Then** the plugin version is upgraded to the latest available version.
3. **Given** a project that already has the plugin at the latest version, **When** the user runs the install-plugin command, **Then** the tool informs the user no update is needed.
4. **Given** a project with a newer plugin version than the one specified, **When** the user runs the install-plugin command with a specific older version, **Then** the tool refuses to downgrade and informs the user.

---

### User Story 4 - Remove Plugin from Unity Project (Priority: P2)

A developer wants to cleanly remove the Unity-MCP plugin from a Unity project, reverting the project to its pre-plugin state without breaking other packages.

**Why this priority**: Ability to uninstall is essential for a complete lifecycle, but less frequently used than installation.

**Independent Test**: Can be fully tested by running the remove-plugin command on a project with the plugin installed, then verifying the plugin no longer appears in the package manifest while other packages remain intact.

**Acceptance Scenarios**:

1. **Given** a Unity project with the Unity-MCP plugin installed, **When** the user runs the remove-plugin command, **Then** the plugin entry is removed from `Packages/manifest.json` and scoped registries remain intact for other packages.
2. **Given** a Unity project without the plugin, **When** the user runs the remove-plugin command, **Then** the tool informs the user the plugin is not installed.

---

### User Story 5 - Open Unity Project (Priority: P2)

A developer wants to open a Unity project in the editor. By default, the `open` command launches with MCP connection environment variables set. An optional flag (e.g., `--no-connect`) allows opening the project without MCP connection for simple editing.

**Why this priority**: Enables launching Unity with dynamic MCP configuration without modifying project files, critical for different environments (dev/staging/prod). Also serves as the simple project-open command.

**Independent Test**: Can be fully tested by running the open command with and without connection parameters and verifying the Unity Editor launches with the correct environment variables (or none when `--no-connect` is used).

**Acceptance Scenarios**:

1. **Given** a valid Unity project path, **When** the user runs the `open` command with connection parameters (URL, token, transport), **Then** the Unity Editor opens with the corresponding `UNITY_MCP_*` environment variables set.
2. **Given** a valid Unity project path, **When** the user runs the `open` command with the `--no-connect` flag, **Then** the Unity Editor opens without any MCP environment variables.
3. **Given** no Unity version is specified, **When** the user runs the open command, **Then** the tool discovers the correct Unity version from the project's `ProjectSettings/ProjectVersion.txt`.

---

### User Story 6 - Configure MCP Tools, Prompts, and Resources (Priority: P2)

A developer wants to enable or disable specific MCP tools, prompts, or resources in a Unity project's configuration without manually editing JSON files.

**Why this priority**: Enables fine-grained control over MCP features, important for security and customization, but not needed for initial setup.

**Independent Test**: Can be fully tested by running the configure command with enable/disable flags and verifying the configuration file reflects the changes.

**Acceptance Scenarios**:

1. **Given** a valid Unity project path, **When** the user runs the configure command with `--enable-tools tool1,tool2`, **Then** the specified tools are enabled in the project's AI configuration file.
2. **Given** a valid Unity project path, **When** the user runs the configure command with `--list`, **Then** the current configuration of all tools, prompts, and resources is displayed.
3. **Given** a valid Unity project path, **When** the user runs the configure command with `--disable-all-tools`, **Then** all tools are disabled in the configuration.

---

### User Story 7 - Help Command with Alphabetical Listing (Priority: P3)

A developer wants to see all available commands listed alphabetically with descriptions when they run the help command or pass no arguments.

**Why this priority**: Help is essential for discoverability but is a supporting feature, not a primary workflow.

**Independent Test**: Can be fully tested by running the tool with `--help` and verifying all commands appear in alphabetical order with descriptions.

**Acceptance Scenarios**:

1. **Given** the CLI is installed, **When** the user runs the tool with `--help` or no arguments, **Then** all available commands are listed in alphabetical order with brief descriptions.
2. **Given** the CLI is installed, **When** the user runs a specific command with `--help`, **Then** detailed usage information for that command is displayed including all options.

---

### User Story 8 - Fancy Terminal UI with Progress Indicators (Priority: P3)

A developer wants visual feedback during long-running operations (installing Unity, creating projects, downloading plugins) through styled terminal output with spinners, progress bars, and colored status messages.

**Why this priority**: Enhances user experience and provides confidence that operations are progressing, but the tool functions without fancy UI.

**Independent Test**: Can be fully tested by running any long-running command and verifying that animated progress indicators and styled output appear in the terminal.

**Acceptance Scenarios**:

1. **Given** a long-running operation (e.g., Unity Editor installation), **When** the operation is in progress, **Then** the terminal displays an animated spinner or progress indicator showing the operation is active.
2. **Given** any command execution, **When** the command completes successfully, **Then** a styled success message with a visual indicator (e.g., checkmark symbol) is displayed.
3. **Given** any command execution, **When** the command fails, **Then** a styled error message with clear formatting and suggestions for resolution is displayed.

---

### Edge Cases

- What happens when the specified Unity project path does not exist or is inaccessible?
- How does the tool handle network failures during Unity Hub download, editor installation, or plugin version lookup? → Fails with a clear error and suggests retry or manual input; no silent fallbacks.
- What happens when Unity Hub CLI is not found in the expected system locations?
- How does the tool behave on unsupported operating systems or architectures?
- What happens when the user lacks file system permissions to write to the project directory?
- How does the tool handle concurrent operations on the same project?
- What happens when the `manifest.json` file is malformed or missing?
- How does the tool handle version strings that don't match Unity's version format?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a command to install a specific Unity Editor version via Unity Hub CLI, defaulting to the latest stable version when none is specified.
- **FR-002**: System MUST automatically download and install Unity Hub if it is not found on the machine (supporting Windows, macOS, and Linux).
- **FR-003**: System MUST provide a command to create a new Unity project at a specified path using a specified or default Unity Editor version.
- **FR-004**: System MUST provide a command to install the Unity-MCP plugin into a Unity project's package manifest, adding required scoped registries and resolving the latest plugin version from OpenUPM.
- **FR-005**: System MUST never downgrade an already-installed plugin to an older version.
- **FR-006**: System MUST provide a command to remove the Unity-MCP plugin from a Unity project's package manifest while preserving other packages and scoped registries.
- **FR-007**: System MUST provide a single `open` command that launches a Unity project in the editor with MCP connection environment variables (`UNITY_MCP_*`) by default, and an optional flag (e.g., `--no-connect`) to open without MCP connection.
- **FR-008**: System MUST provide a command to configure (enable/disable) individual or all MCP tools, prompts, and resources in a Unity project's AI configuration file.
- **FR-009**: System MUST display a help command listing all available commands sorted alphabetically with descriptions.
- **FR-010**: System MUST display animated progress indicators (spinners, progress bars) for all long-running operations when an interactive terminal (TTY) is detected.
- **FR-011**: System MUST use styled terminal output (colors, symbols, formatted sections) for all user-facing messages when an interactive terminal (TTY) is detected. When no TTY is detected (e.g., CI pipelines), spinners and colors MUST be automatically disabled.
- **FR-012**: System MUST be published to NPMJS as a publicly available package, installable via `npm install -g`.
- **FR-013**: System MUST not use any dependencies with known security vulnerabilities at the time of release.
- **FR-014**: System MUST determine the Unity Editor version from `ProjectSettings/ProjectVersion.txt` when a version is not explicitly provided.
- **FR-015**: System MUST generate a deterministic server port from the project path using SHA256 hashing, mapped to the 50000-59999 range.
- **FR-016**: System MUST provide clear, actionable error messages when operations fail, including suggestions for resolution.
- **FR-017**: System MUST support cross-platform operation on Windows, macOS, and Linux.
- **FR-018**: System MUST fail with a clear error message and actionable suggestions (retry or provide value manually) when network operations fail (e.g., OpenUPM version lookup, Unity Hub download). Silent fallbacks to hardcoded values are not permitted.
- **FR-019**: System MUST provide a `--verbose` flag on all commands that outputs detailed diagnostic information during execution for troubleshooting.

### Key Entities

- **Unity Project**: A directory containing Unity project structure (`Assets/`, `Packages/manifest.json`, `ProjectSettings/`). Identified by path. Has an associated Unity Editor version.
- **Unity Editor**: An installed version of the Unity Editor. Identified by version string (e.g., `2022.3.10f1`). Managed through Unity Hub.
- **Unity Hub**: The Unity Hub application used to install and manage Unity Editor versions. Has a CLI interface for headless operation.
- **Plugin (Unity-MCP Plugin)**: A Unity package installable via OpenUPM scoped registry. Defined by package name and version in `manifest.json`.
- **MCP Configuration**: A project-level JSON file (`UserSettings/AI-Game-Developer-Config.json`) containing enabled/disabled states for tools, prompts, and resources.
- **Connection Settings**: Environment variables (`UNITY_MCP_*`) that configure the plugin's runtime behavior including server URL, transport, authentication, and port.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new user can install the CLI and run their first command (help) within 2 minutes of reading the documentation.
- **SC-002**: A user can go from zero (no Unity) to a fully configured Unity project with the MCP plugin installed in a single terminal session using sequential CLI commands.
- **SC-003**: All commands provide visual feedback within 1 second of invocation (spinner starts, progress appears, or result is displayed).
- **SC-004**: The tool correctly handles all edge cases (missing paths, network errors, permission issues) with user-friendly error messages that include actionable next steps.
- **SC-005**: The tool passes security audit with zero known vulnerabilities in its dependency tree.
- **SC-006**: The tool operates correctly on all three major platforms (Windows, macOS, Linux) without platform-specific workarounds required by the user.
- **SC-007**: Plugin installation and removal operations preserve all other packages and project configuration without data loss.
- **SC-008**: 90% of users can complete any supported operation on their first attempt without consulting external documentation beyond the built-in help.
