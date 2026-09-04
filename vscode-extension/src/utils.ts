import { promises as fs } from 'node:fs';

export async function pathExists(targetPath: string): Promise<boolean> {
  try {
    await fs.access(targetPath);
    return true;
  } catch {
    return false;
  }
}

export function toErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

export function normalizeFsPath(
  targetPath: string,
  platform: NodeJS.Platform = process.platform,
): string {
  const normalized = targetPath.replaceAll('\\', '/');
  return platform === 'win32' ? normalized.toLowerCase() : normalized;
}
