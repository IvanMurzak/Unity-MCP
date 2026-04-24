import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { spawn } from 'child_process';
import type {
  TeamRuntimeAdapter,
  TeamRuntimeCapabilities,
  TeamRuntimeLaunchRequest,
  TeamRuntimeLaunchResult,
  TeamRuntimeRoleInspection,
  TeamRuntimeSelection,
  TeamRuntimeSessionInspection,
} from './team-runtime.js';

interface ProcessRuntimeRoleRecord {
  roleName: string;
  runtimeHandle: string;
  displayName: string;
  workingDirectory: string;
  currentCommand: string;
}

interface ProcessRuntimeSessionRecord {
  runtimeKind: 'process';
  sessionHandle: string;
  projectPath: string;
  createdAt: string;
  roles: ProcessRuntimeRoleRecord[];
}

interface ProcessRuntimeDependencies {
  metadataRoot?: string;
  launchRoleProcess?: (role: { roleName: string; workingDirectory: string; currentCommand: string }) => { pid: number };
  isProcessAlive?: (pid: number) => boolean;
  terminateProcess?: (pid: number) => void;
}

function getDefaultMetadataRoot(): string {
  return path.join(os.tmpdir(), 'unity-mcp-team-process-runtime');
}

function ensureMetadataRoot(metadataRoot: string): void {
  fs.mkdirSync(metadataRoot, { recursive: true });
}

function getMetadataPath(metadataRoot: string, sessionHandle: string): string {
  return path.join(metadataRoot, `${sessionHandle}.json`);
}

function readSessionRecord(metadataRoot: string, sessionHandle: string): ProcessRuntimeSessionRecord | null {
  const metadataPath = getMetadataPath(metadataRoot, sessionHandle);
  if (!fs.existsSync(metadataPath)) {
    return null;
  }

  return JSON.parse(fs.readFileSync(metadataPath, 'utf-8')) as ProcessRuntimeSessionRecord;
}

function writeSessionRecord(metadataRoot: string, record: ProcessRuntimeSessionRecord): void {
  ensureMetadataRoot(metadataRoot);
  fs.writeFileSync(getMetadataPath(metadataRoot, record.sessionHandle), `${JSON.stringify(record, null, 2)}\n`);
}

function removeSessionRecord(metadataRoot: string, sessionHandle: string): void {
  fs.rmSync(getMetadataPath(metadataRoot, sessionHandle), { force: true });
}

function defaultLaunchRoleProcess(
  _role: { roleName: string; workingDirectory: string; currentCommand: string },
): { pid: number } {
  const child = spawn(
    process.execPath,
    ['-e', 'setInterval(() => {}, 1 << 30)'],
    {
      detached: true,
      stdio: 'ignore',
      windowsHide: true,
    },
  );
  child.unref();

  if (!child.pid) {
    throw new Error('Unable to start standalone process runtime role.');
  }

  return { pid: child.pid };
}

function defaultIsProcessAlive(pid: number): boolean {
  try {
    process.kill(pid, 0);
    return true;
  } catch (err) {
    const code = (err as NodeJS.ErrnoException).code;
    return code === 'EPERM';
  }
}

function defaultTerminateProcess(pid: number): void {
  try {
    process.kill(pid);
  } catch (err) {
    const code = (err as NodeJS.ErrnoException).code;
    if (code !== 'ESRCH') {
      throw err;
    }
  }
}

function toInspection(role: ProcessRuntimeRoleRecord, isAlive: boolean): TeamRuntimeRoleInspection {
  return {
    roleName: role.roleName,
    runtimeHandle: role.runtimeHandle,
    displayName: role.displayName,
    status: isAlive ? 'ready' : 'missing',
    workingDirectory: role.workingDirectory,
    currentCommand: role.currentCommand,
  };
}

export function createProcessTeamRuntime(
  selection: TeamRuntimeSelection,
  deps: ProcessRuntimeDependencies = {},
): TeamRuntimeAdapter {
  const metadataRoot = deps.metadataRoot ?? getDefaultMetadataRoot();
  const launchRoleProcess = deps.launchRoleProcess ?? defaultLaunchRoleProcess;
  const isProcessAlive = deps.isProcessAlive ?? defaultIsProcessAlive;
  const terminateProcess = deps.terminateProcess ?? defaultTerminateProcess;

  const capabilities: TeamRuntimeCapabilities = {
    runtimeKind: 'process',
    paneTitles: false,
    roleHandles: true,
    splitLayout: false,
    sessionListing: false,
  };

  return {
    kind: 'process',
    displayName: 'process',
    selection,

    ensureAvailable(): void {
      ensureMetadataRoot(metadataRoot);
    },

    capabilities(): TeamRuntimeCapabilities {
      return capabilities;
    },

    hasSession(sessionHandle: string): boolean {
      const record = readSessionRecord(metadataRoot, sessionHandle);
      if (!record) {
        return false;
      }

      return record.roles.some(role => isProcessAlive(Number(role.runtimeHandle)));
    },

    launchSession(request: TeamRuntimeLaunchRequest): TeamRuntimeLaunchResult {
      const existing = readSessionRecord(metadataRoot, request.sessionId);
      if (existing && existing.roles.some(role => isProcessAlive(Number(role.runtimeHandle)))) {
        throw new Error(`A process session named "${request.sessionId}" already exists.`);
      }

      const roles = request.roles.map(role => {
        const launched = launchRoleProcess({
          roleName: role.roleName,
          workingDirectory: role.workingDirectory,
          currentCommand: role.command,
        });

        return {
          roleName: role.roleName,
          runtimeHandle: String(launched.pid),
          displayName: role.roleName,
          workingDirectory: role.workingDirectory,
          currentCommand: role.command,
        };
      });

      writeSessionRecord(metadataRoot, {
        runtimeKind: 'process',
        sessionHandle: request.sessionId,
        projectPath: request.projectPath,
        createdAt: new Date().toISOString(),
        roles,
      });

      return {
        sessionHandle: request.sessionId,
        roles: roles.map(role => ({
          roleName: role.roleName,
          runtimeHandle: role.runtimeHandle,
          displayName: role.displayName,
        })),
        notes: selection.fallbackReason ? [selection.fallbackReason] : [],
      };
    },

    inspectSession(sessionHandle: string): TeamRuntimeSessionInspection {
      const record = readSessionRecord(metadataRoot, sessionHandle);
      if (!record) {
        return {
          sessionHandle,
          available: false,
          roles: [],
          issues: [`process session ${sessionHandle} is not available`],
        };
      }

      const roles = record.roles.map(role => toInspection(role, isProcessAlive(Number(role.runtimeHandle))));
      const issues = roles
        .filter(role => role.status !== 'ready')
        .map(role => `process runtime missing role ${role.roleName} (${role.runtimeHandle || 'unassigned'})`);
      const available = roles.some(role => role.status === 'ready');

      return {
        sessionHandle,
        available,
        roles,
        issues: available ? issues : [`process session ${sessionHandle} is not available`, ...issues],
      };
    },

    stopSession(sessionHandle: string): void {
      const record = readSessionRecord(metadataRoot, sessionHandle);
      if (!record) {
        return;
      }

      for (const role of record.roles) {
        const pid = Number(role.runtimeHandle);
        if (Number.isFinite(pid) && isProcessAlive(pid)) {
          terminateProcess(pid);
        }
      }

      removeSessionRecord(metadataRoot, sessionHandle);
    },
  };
}
