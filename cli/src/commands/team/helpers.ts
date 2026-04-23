import * as fs from 'fs';
import * as path from 'path';
import { resolveAndValidateProjectPath } from '../../utils/connection.js';

export interface TeamProjectOptions {
  path?: string;
}

export interface TeamTargetResolution {
  projectPath: string;
  sessionRef?: string;
}

export function resolveTeamProjectPath(positionalPath: string | undefined, options: TeamProjectOptions): string {
  return resolveAndValidateProjectPath(positionalPath, { path: options.path });
}

export function resolveTeamProjectAndSession(target: string | undefined, options: TeamProjectOptions): TeamTargetResolution {
  if (options.path) {
    return {
      projectPath: resolveAndValidateProjectPath(undefined, { path: options.path }),
      sessionRef: target,
    };
  }

  if (target) {
    const asPath = path.resolve(target);
    if (fs.existsSync(asPath)) {
      return {
        projectPath: resolveAndValidateProjectPath(target, {}),
      };
    }
  }

  return {
    projectPath: resolveAndValidateProjectPath(undefined, {}),
    sessionRef: target,
  };
}
