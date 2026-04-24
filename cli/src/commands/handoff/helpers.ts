import { resolveAndValidateProjectPath } from '../../utils/connection.js';

export interface HandoffProjectOptions {
  path?: string;
}

export function resolveHandoffProjectPath(positionalPath: string | undefined, options: HandoffProjectOptions): string {
  return resolveAndValidateProjectPath(positionalPath, { path: options.path });
}
