import type { ProgressCallback, ProgressEvent } from './types.js';

/**
 * Forward a single progress event to the caller's optional callback,
 * swallowing any exception the callback throws. A broken onProgress
 * handler must never abort the underlying library operation.
 */
export function emitProgress(onProgress: ProgressCallback | undefined, event: ProgressEvent): void {
  if (!onProgress) return;
  try {
    onProgress(event);
  } catch {
    // Intentionally ignored — see doc comment.
  }
}
