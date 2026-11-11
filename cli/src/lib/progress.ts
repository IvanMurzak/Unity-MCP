// Forward a progress event to the caller's optional callback, swallowing anything it throws: cli-core's.
export { emitProgress } from '@baizor/gamedev-cli-core';

/** ` (PID: n)` when the pid is known, else nothing: a log line never says "PID: unknown". */
export function pidSuffix(pid: number | undefined): string {
  return pid !== undefined ? ` (PID: ${pid})` : '';
}
