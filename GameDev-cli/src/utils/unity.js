import { spawn, execSync } from 'child_process';
import fs from 'fs';
import path from 'path';
import os from 'os';

// Unity Hub executable paths per platform
const UNITY_HUB_PATHS = {
  win32: [
    path.join('C:', 'Program Files', 'Unity Hub', 'Unity Hub.exe'),
    path.join(os.homedir(), 'AppData', 'Local', 'Programs', 'Unity Hub', 'Unity Hub.exe'),
  ],
  darwin: [
    '/Applications/Unity Hub.app/Contents/MacOS/Unity Hub',
  ],
  linux: [
    '/opt/unityhub/unityhub',
    '/usr/bin/unityhub',
    path.join(os.homedir(), 'Applications', 'Unity Hub', 'unityhub'),
  ],
};

// Unity Editor installation base directories per platform
const UNITY_EDITOR_BASE_DIRS = {
  win32: [
    path.join('C:', 'Program Files', 'Unity', 'Hub', 'Editor'),
    path.join(os.homedir(), 'AppData', 'Roaming', 'Unity', 'Editors'),
  ],
  darwin: [
    '/Applications/Unity/Hub/Editor',
  ],
  linux: [
    path.join(os.homedir(), '.local', 'share', 'unity-hub', 'editors'),
    '/opt/Unity/Hub/Editor',
  ],
};

// Platform-specific Unity executable name within a versioned Editor directory
const UNITY_EXECUTABLE_SUBPATH = {
  win32: 'Editor/Unity.exe',
  darwin: 'Unity.app/Contents/MacOS/Unity',
  linux: 'Editor/Unity',
};

/**
 * Attempts to find the Unity Hub executable on this machine.
 * Tries PATH first, then falls back to known install locations.
 * @returns {string|null} Path to the Unity Hub executable, or null if not found
 */
function findUnityHub() {
  const platform = process.platform;

  // Try the command in PATH first
  try {
    execSync('unityhub --version', { stdio: 'ignore' });
    return 'unityhub';
  } catch {
    // not in PATH
  }

  const candidates = UNITY_HUB_PATHS[platform] ?? [];
  for (const candidate of candidates) {
    if (fs.existsSync(candidate)) return candidate;
  }
  return null;
}

/**
 * Finds the Unity Editor executable for a specific version, or the latest
 * installed version if no version is specified.
 * @param {string|undefined} version - Unity version string (e.g. "2022.3.62f1")
 * @returns {string|null} Absolute path to the Unity executable, or null if not found
 */
function findUnityExecutable(version) {
  const platform = process.platform;
  const execSubpath = UNITY_EXECUTABLE_SUBPATH[platform] ?? 'Editor/Unity';
  const baseDirs = UNITY_EDITOR_BASE_DIRS[platform] ?? [];

  for (const baseDir of baseDirs) {
    if (!fs.existsSync(baseDir)) continue;

    if (version) {
      const candidate = path.join(baseDir, version, execSubpath);
      if (fs.existsSync(candidate)) return candidate;
    } else {
      // Find all installed versions and return the most recently installed one
      let entries;
      try {
        entries = fs.readdirSync(baseDir).filter(e =>
          fs.statSync(path.join(baseDir, e)).isDirectory()
        );
      } catch {
        continue;
      }
      // Sort descending so newest version is first
      entries.sort((a, b) => b.localeCompare(a, undefined, { numeric: true }));
      for (const entry of entries) {
        const candidate = path.join(baseDir, entry, execSubpath);
        if (fs.existsSync(candidate)) return candidate;
      }
    }
  }
  return null;
}

/**
 * Opens a Unity project in the Unity Editor.
 *
 * Resolution order:
 *  1. If `options.unityPath` is provided, use it directly.
 *  2. Otherwise, look for Unity Hub and use its `launch-project` headless command.
 *  3. Otherwise, scan known install directories for a Unity executable.
 *
 * The editor process is spawned detached so the CLI exits immediately.
 *
 * @param {string} projectPath - Absolute path to the Unity project root
 * @param {object} [options]
 * @param {string} [options.unityPath] - Explicit path to the Unity executable
 * @param {string} [options.unityVersion] - Unity version to use (e.g. "2022.3.62f1")
 * @param {string[]} [options.extraArgs] - Extra arguments forwarded to the Unity process
 */
export async function openUnityProject(projectPath, options = {}) {
  const { unityPath, unityVersion, extraArgs = [] } = options;

  let executable;
  let args;

  if (unityPath) {
    // Explicit path provided — use it directly
    executable = unityPath;
    args = ['-projectPath', projectPath, ...extraArgs];
  } else {
    const unityHub = findUnityHub();
    if (unityHub) {
      // Unity Hub headless launch opens the editor interactively
      executable = unityHub;
      args = ['--', '--headless', 'launch-project', '--path', projectPath, ...extraArgs];
    } else {
      // Fall back to a direct Unity executable
      const unityExe = findUnityExecutable(unityVersion);
      if (!unityExe) {
        throw new Error(
          'Could not find a Unity installation.\n' +
          'Please install Unity Hub or specify --unity-path <path> to the Unity executable.'
        );
      }
      executable = unityExe;
      args = ['-projectPath', projectPath, ...extraArgs];
    }
  }

  console.log(`Launching: ${executable} ${args.join(' ')}`);

  const child = spawn(executable, args, {
    detached: true,
    stdio: 'ignore',
  });

  child.unref();
  console.log(`Unity launched (PID: ${child.pid})`);
}
