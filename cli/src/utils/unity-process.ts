import { execFileSync, execSync } from 'child_process';
import * as fs from 'fs';
import { platform } from 'os';
import * as path from 'path';
import { verbose } from './ui.js';

export interface UnityProcess {
  pid: number;
  projectPath: string;
  commandLine: string;
}

/**
 * Check if a Unity Editor process is running with the given project path.
 * Returns process info if found, null otherwise.
 */
export function findUnityProcess(projectPath: string): UnityProcess | null {
  const isWindows = platform() === 'win32';
  const normalize = (p: string): string => {
    const real = canonicalPath(p);
    return isWindows ? real.toLowerCase() : real;
  };
  const normalizedTarget = normalize(projectPath);
  const processes = listUnityProcesses();

  for (const proc of processes) {
    const normalizedProc = normalize(proc.projectPath);
    if (normalizedProc === normalizedTarget) {
      verbose(`Found Unity process PID ${proc.pid} with project: ${proc.projectPath}`);
      return proc;
    }
  }

  verbose(`No Unity process found for project: ${projectPath}`);
  return null;
}

/**
 * The absolute path with symlinks resolved, so `/tmp/x` and `/private/tmp/x` (macOS) or a junction and its target name
 * the same project. A path that does not exist (or cannot be read) stays as `path.resolve` gives it.
 */
export function canonicalPath(p: string): string {
  const resolved = path.resolve(p);
  try {
    return fs.realpathSync.native(resolved);
  } catch {
    return resolved;
  }
}

/**
 * Unity's background asset-import workers run the same Unity binary with the same `-projectPath`
 * (`-batchMode -name AssetImportWorker0 ... -parentPid <editor>`); they are not the Editor.
 */
export function isAssetImportWorker(commandLine: string): boolean {
  return /-name\s+"?AssetImportWorker/i.test(commandLine);
}

/**
 * List all running Unity Editor processes with their project paths (asset-import workers excluded).
 */
function listUnityProcesses(): UnityProcess[] {
  const os = platform();
  const results: UnityProcess[] = [];

  try {
    let lines: string[];

    if (os === 'win32') {
      const psCommand = "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | Select-Object ProcessId,CommandLine | ForEach-Object { $_.ProcessId.ToString() + '|||' + $_.CommandLine }";
      const output = execFileSync(
        'powershell.exe',
        ['-NoProfile', '-Command', psCommand],
        { encoding: 'utf-8', timeout: 10000, stdio: ['pipe', 'pipe', 'pipe'] }
      );
      lines = output.split('\n').filter(l => l.trim().length > 0);

      for (const line of lines) {
        const sepIdx = line.indexOf('|||');
        if (sepIdx === -1) continue;

        const pid = parseInt(line.substring(0, sepIdx).trim(), 10);
        const commandLine = line.substring(sepIdx + 3).trim();

        if (!Number.isFinite(pid) || pid === 0) continue;
        if (isAssetImportWorker(commandLine)) continue;

        const projectPathMatch = commandLine.match(/-projectPath\s+"([^"]+)"/i)
          ?? commandLine.match(/-projectPath\s+(\S+)/i);

        if (projectPathMatch) {
          results.push({
            pid,
            projectPath: path.resolve(projectPathMatch[1].trim()),
            commandLine,
          });
        }
      }
    } else {
      // macOS / Linux
      const output = execSync(
        "ps -eo pid,args | grep -i '[U]nity' || true",
        { encoding: 'utf-8', timeout: 5000, stdio: ['pipe', 'pipe', 'pipe'] }
      );
      lines = output.split('\n').filter(l => l.trim().length > 0);

      for (const line of lines) {
        const match = line.trim().match(/^(\d+)\s+(.*)$/);
        if (!match) continue;

        const pid = parseInt(match[1], 10);
        const commandLine = match[2];

        if (!commandLine.includes('-projectPath')) continue;
        if (isAssetImportWorker(commandLine)) continue;

        const projectPathMatch = commandLine.match(/-projectPath\s+"([^"]+)"/)
          ?? commandLine.match(/-projectPath\s+(\S+)/);

        if (projectPathMatch) {
          results.push({
            pid,
            projectPath: path.resolve(projectPathMatch[1].trim()),
            commandLine,
          });
        }
      }
    }
  } catch (err) {
    verbose(`Failed to list Unity processes: ${err instanceof Error ? err.message : String(err)}`);
  }

  verbose(`Found ${results.length} Unity process(es)`);
  return results;
}
