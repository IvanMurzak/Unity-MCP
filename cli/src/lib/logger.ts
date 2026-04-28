// Minimal logger interface used by shared utility functions that are
// exercised from BOTH the CLI (which wants chalk-styled console output)
// and the library API (which must stay silent).
//
// The CLI passes an adapter backed by the existing `ui` module; the
// library API passes `silentLogger` which swallows every call.
//
// This file intentionally has no top-level side effects and no
// dependency on `chalk`, `commander`, or any other UI layer — the
// library entry (lib.ts) imports it, and the library entry is
// required to stay side-effect-free.

export interface LibLogger {
  info(message: string): void;
  success(message: string): void;
  warn(message: string): void;
  error(message: string): void;
}

export const silentLogger: LibLogger = {
  info: () => { /* intentionally silent */ },
  success: () => { /* intentionally silent */ },
  warn: () => { /* intentionally silent */ },
  error: () => { /* intentionally silent */ },
};
