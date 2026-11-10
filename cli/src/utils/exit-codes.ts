// Process exit codes, shared by unity-mcp-cli, unreal-mcp-cli and godot-cli (same numbers in all three), so a script or
// an agent can tell "fix the command line" from "the editor failed" from "the tests failed" without reading messages.

export const ExitCode = {
  Success: 0,
  /** Unclassified error (commands that do not use the taxonomy yet). */
  Error: 1,
  /** Bad arguments: a misspelled flag, a wrong value. Nothing ran; fix the command line. */
  Usage: 2,
  /** The command ran and failed: the tool reported an error, compilation failed, a test run produced no verdict. */
  CommandFailed: 6,
  /** The editor or its MCP server could not be started or reached; whether the command would succeed is unknown. */
  Unreachable: 7,
  /** The test run completed and reported failing tests. */
  TestsFailed: 8,
  /** The command hit its timeout before a result. */
  Timeout: 14,
  /** The editor is busy (the call was not executed); retry later. */
  Busy: 17,
  /** The editor is blocked by a modal dialog that waits for a human. */
  AwaitingInput: 18,
} as const;

export type ExitCodeValue = (typeof ExitCode)[keyof typeof ExitCode];

/** One line per code, for `commands` and the README. */
export const EXIT_CODE_DESCRIPTIONS: ReadonlyArray<{ code: ExitCodeValue; name: string; description: string }> = [
  { code: ExitCode.Success, name: 'SUCCESS', description: 'The command succeeded.' },
  { code: ExitCode.Error, name: 'ERROR', description: 'Unclassified error.' },
  { code: ExitCode.Usage, name: 'USAGE', description: 'Bad arguments; nothing ran. Fix the command line, do not retry it unchanged.' },
  { code: ExitCode.CommandFailed, name: 'COMMAND_FAILED', description: 'The command ran and failed (tool error, compile errors, test run without a verdict).' },
  { code: ExitCode.Unreachable, name: 'UNREACHABLE', description: 'The editor or MCP server could not be started or reached. Retry once the environment is fixed.' },
  { code: ExitCode.TestsFailed, name: 'TESTS_FAILED', description: 'The tests ran and some failed.' },
  { code: ExitCode.Timeout, name: 'TIMEOUT', description: 'The command timed out before a result.' },
  { code: ExitCode.Busy, name: 'BUSY', description: 'The editor is busy and did not run the call; retry later.' },
  { code: ExitCode.AwaitingInput, name: 'AWAITING_INPUT', description: 'A modal dialog blocks the editor; a human must answer it (see `dialog`).' },
];
