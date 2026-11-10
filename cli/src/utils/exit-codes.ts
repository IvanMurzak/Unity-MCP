// Process exit codes, shared by unity-mcp-cli, unreal-mcp-cli and godot-cli: the contract lives in
// @baizor/gamedev-cli-core, so a script or an agent can tell "fix the command line" from "the editor failed" from
// "the tests failed" without reading messages.

export { ExitCode, EXIT_CODES as EXIT_CODE_DESCRIPTIONS } from '@baizor/gamedev-cli-core';
export type { ExitCodeValue } from '@baizor/gamedev-cli-core';
