import { afterEach, beforeEach, describe, it, expect, vi } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { createProject } from '../src/lib.js';
import { queryInstalledEditors, runEditorCreateProject } from '../src/utils/unity-hub.js';
import type {
  CreateProjectEditorInfo,
  CreateProjectOptions,
  ProgressEvent,
} from '../src/lib.js';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const EDITORS: CreateProjectEditorInfo[] = [
  { version: '2022.3.62f3', path: '/editors/2022.3.62f3' },
  { version: '6000.3.11f1', path: '/editors/6000.3.11f1' },
];

const FAKE_HUB = '/hub/Unity Hub.exe';

/**
 * Options with every side-effecting dependency mocked out so tests
 * never touch Unity Hub or the filesystem. Individual tests override
 * the pieces they exercise.
 */
function makeOptions(overrides: Partial<CreateProjectOptions> = {}): CreateProjectOptions {
  return {
    projectPath: 'some/new-project',
    // No editor in the Unity Hub editor folders unless a test says so, so the
    // Unity Hub CLI path below is exercised and the machine's real installs
    // never leak in.
    listLocalEditorsImpl: () => [],
    findHubImpl: () => FAKE_HUB,
    listEditorsImpl: () => EDITORS,
    resolveEditorExecutableImpl: (installPath) => `${installPath}/Editor/Unity.exe`,
    runEditorImpl: vi.fn(async () => {}),
    ...overrides,
  };
}

// ---------------------------------------------------------------------------
// Argument validation
// ---------------------------------------------------------------------------

describe('createProject — argument validation', () => {
  it('fails on an empty projectPath without touching the hub', async () => {
    const findHub = vi.fn(() => FAKE_HUB);
    const runEditor = vi.fn(async () => {});
    const result = await createProject(
      makeOptions({ projectPath: '', findHubImpl: findHub, runEditorImpl: runEditor }),
    );

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.success).toBe(false);
    expect(result.errorMessage).toContain('projectPath');
    expect(findHub).not.toHaveBeenCalled();
    expect(runEditor).not.toHaveBeenCalled();
  });

  it('fails on a whitespace-only projectPath', async () => {
    const result = await createProject(makeOptions({ projectPath: '   ' }));
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.errorMessage).toContain('projectPath');
  });

  it('fails on a whitespace-only editorVersion', async () => {
    const result = await createProject(makeOptions({ editorVersion: '   ' }));
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.errorMessage).toContain('editorVersion');
  });
});

// ---------------------------------------------------------------------------
// Unity Hub resolution
// ---------------------------------------------------------------------------

describe('createProject — Unity Hub resolution', () => {
  it('returns a structured failure when Unity Hub is missing', async () => {
    const result = await createProject(makeOptions({ findHubImpl: () => null }));
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.errorMessage).toContain('Unity Hub not found');
    expect(result.errorMessage).toContain('install-unity');
  });

  it('skips hub discovery when an explicit hubPath is provided', async () => {
    const findHub = vi.fn(() => null);
    const listEditors = vi.fn(() => EDITORS);
    const result = await createProject(
      makeOptions({ hubPath: FAKE_HUB, findHubImpl: findHub, listEditorsImpl: listEditors }),
    );

    expect(result.kind).toBe('success');
    expect(findHub).not.toHaveBeenCalled();
    expect(listEditors).toHaveBeenCalledWith(FAKE_HUB);
  });
});

// ---------------------------------------------------------------------------
// Editor folders first, Unity Hub CLI only on a miss
// ---------------------------------------------------------------------------

describe('createProject — editor folders before the Unity Hub CLI', () => {
  const LOCAL: CreateProjectEditorInfo[] = [
    { version: '6000.3.1f1', path: '/local/6000.3.1f1/Editor/Unity.exe' },
  ];

  it('does not start the Unity Hub CLI when the requested version is in the editor folders', async () => {
    const findHub = vi.fn(() => FAKE_HUB);
    const listEditors = vi.fn(() => EDITORS);
    const runEditor = vi.fn(async () => {});
    const result = await createProject(
      makeOptions({
        editorVersion: '6000.3.1f1',
        listLocalEditorsImpl: () => LOCAL,
        findHubImpl: findHub,
        listEditorsImpl: listEditors,
        resolveEditorExecutableImpl: (p) => p,
        runEditorImpl: runEditor,
      }),
    );

    expect(result.kind).toBe('success');
    expect(findHub).not.toHaveBeenCalled();
    expect(listEditors).not.toHaveBeenCalled();
    expect(runEditor).toHaveBeenCalledWith('/local/6000.3.1f1/Editor/Unity.exe', expect.any(String), expect.any(Number));
  });

  it('does not need Unity Hub at all when no version is requested and an editor is found locally', async () => {
    const listEditors = vi.fn(() => EDITORS);
    const result = await createProject(
      makeOptions({ listLocalEditorsImpl: () => LOCAL, findHubImpl: () => null, listEditorsImpl: listEditors }),
    );

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.editorVersion).toBe('6000.3.1f1');
    expect(listEditors).not.toHaveBeenCalled();
  });

  it('asks the Unity Hub CLI when the requested version is not in the editor folders', async () => {
    const listEditors = vi.fn(() => EDITORS);
    const result = await createProject(
      makeOptions({ editorVersion: '2022.3.62f3', listLocalEditorsImpl: () => LOCAL, listEditorsImpl: listEditors }),
    );

    expect(result.kind).toBe('success');
    expect(listEditors).toHaveBeenCalledWith(FAKE_HUB);
  });
});

// ---------------------------------------------------------------------------
// Editor resolution
// ---------------------------------------------------------------------------

describe('createProject — editor resolution', () => {
  it('fails with a structured error when no editors are installed', async () => {
    const result = await createProject(makeOptions({ listEditorsImpl: () => [] }));
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.errorMessage).toContain('No Unity editors installed');
  });

  it('never throws when the editor query itself fails', async () => {
    const result = await createProject(
      makeOptions({
        listEditorsImpl: () => {
          throw new Error('Failed to list installed editors: hub exploded');
        },
      }),
    );
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.errorMessage).toContain('hub exploded');
  });

  it('uses the explicitly requested version when installed', async () => {
    const runEditor = vi.fn(async () => {});
    const result = await createProject(
      makeOptions({ editorVersion: '2022.3.62f3', runEditorImpl: runEditor }),
    );

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.editorVersion).toBe('2022.3.62f3');
    expect(result.editorPath).toBe('/editors/2022.3.62f3/Editor/Unity.exe');
    expect(runEditor).toHaveBeenCalledWith(
      '/editors/2022.3.62f3/Editor/Unity.exe',
      path.resolve('some/new-project'),
      120000,
    );
    // Explicit version → no "using highest installed" warning.
    expect(result.warnings).toEqual([]);
  });

  it('fails with installed versions listed when the requested version is missing', async () => {
    const runEditor = vi.fn(async () => {});
    const result = await createProject(
      makeOptions({ editorVersion: '2021.1.1f1', runEditorImpl: runEditor }),
    );

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.errorMessage).toContain('Unity Editor 2021.1.1f1 not found');
    expect(result.errorMessage).toContain('2022.3.62f3');
    expect(result.errorMessage).toContain('6000.3.11f1');
    expect(runEditor).not.toHaveBeenCalled();
  });

  it('falls back to the highest installed editor when no version is requested', async () => {
    const result = await createProject(makeOptions());

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.editorVersion).toBe('6000.3.11f1');
    expect(result.warnings).toEqual([
      'No Unity version specified — using highest installed: 6000.3.11f1',
    ]);
  });

  it('fails when the editor executable cannot be resolved', async () => {
    const result = await createProject(
      makeOptions({ resolveEditorExecutableImpl: () => null }),
    );
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.errorMessage).toContain('executable not found');
    expect(result.editorVersion).toBe('6000.3.11f1');
  });
});

// ---------------------------------------------------------------------------
// Success path
// ---------------------------------------------------------------------------

describe('createProject — success path', () => {
  it('returns the full structured success result and emits the progress sequence', async () => {
    const runEditor = vi.fn(async () => {});
    const phases: ProgressEvent['phase'][] = [];

    const result = await createProject(
      makeOptions({
        runEditorImpl: runEditor,
        onProgress: (event) => phases.push(event.phase),
      }),
    );

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.success).toBe(true);
    expect(result.projectPath).toBe(path.resolve('some/new-project'));
    expect(result.editorVersion).toBe('6000.3.11f1');
    expect(result.editorPath).toBe('/editors/6000.3.11f1/Editor/Unity.exe');
    expect(phases).toEqual([
      'start',
      'editors-located',
      'editor-resolved',
      'creating-project',
      'done',
    ]);
    expect(runEditor).toHaveBeenCalledTimes(1);
  });

  it('forwards a custom timeout and defaults non-positive values', async () => {
    const runEditor = vi.fn(async () => {});

    await createProject(makeOptions({ runEditorImpl: runEditor, timeoutMs: 5000 }));
    expect(runEditor).toHaveBeenLastCalledWith(expect.any(String), expect.any(String), 5000);

    await createProject(makeOptions({ runEditorImpl: runEditor, timeoutMs: 0 }));
    expect(runEditor).toHaveBeenLastCalledWith(expect.any(String), expect.any(String), 120000);

    // The upper boundary (Node's max timer delay, 2^31-1) is honoured as-is.
    await createProject(makeOptions({ runEditorImpl: runEditor, timeoutMs: 2147483647 }));
    expect(runEditor).toHaveBeenLastCalledWith(expect.any(String), expect.any(String), 2147483647);

    // Non-integer values (negative, fractional, NaN, Infinity) and values
    // above Node's max timer delay (2^31-1, which would emit a
    // TimeoutOverflowWarning and clamp to 1ms) are not valid execFile
    // timeouts and fall back to the default rather than reaching
    // child_process with an out-of-range value.
    for (const bad of [-1, 1500.5, Number.NaN, Number.POSITIVE_INFINITY, 2147483648]) {
      await createProject(makeOptions({ runEditorImpl: runEditor, timeoutMs: bad }));
      expect(runEditor).toHaveBeenLastCalledWith(expect.any(String), expect.any(String), 120000);
    }
  });

  it('a throwing onProgress callback does not break the operation', async () => {
    const result = await createProject(
      makeOptions({
        onProgress: () => {
          throw new Error('broken callback');
        },
      }),
    );
    expect(result.kind).toBe('success');
  });
});

// ---------------------------------------------------------------------------
// Editor invocation failure
// ---------------------------------------------------------------------------

describe('createProject — editor invocation failure', () => {
  it('captures a rejected editor run as a structured failure', async () => {
    const result = await createProject(
      makeOptions({
        runEditorImpl: async () => {
          throw new Error('Failed to create project: exit code 1\nsome editor stderr');
        },
      }),
    );

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.success).toBe(false);
    expect(result.errorMessage).toContain('Failed to create project');
    expect(result.errorMessage).toContain('some editor stderr');
    expect(result.error).toBeInstanceOf(Error);
    // Context resolved before the failure is preserved on the result.
    expect(result.projectPath).toBe(path.resolve('some/new-project'));
    expect(result.editorVersion).toBe('6000.3.11f1');
    expect(result.editorPath).toBe('/editors/6000.3.11f1/Editor/Unity.exe');
  });
});

// ---------------------------------------------------------------------------
// Unity-hub building blocks (exercised directly — the createProject tests
// above inject past these, so their real error-shaping contracts are only
// covered here).
// ---------------------------------------------------------------------------

describe('queryInstalledEditors — direct contract', () => {
  it('rejects an unrunnable hub with the formatted "Failed to list installed editors" error', async () => {
    // A path that cannot be executed makes the underlying execFile call back
    // with an error; the silent core must reject with the message its
    // callers (listInstalledEditors, the lib) rely on.
    await expect(queryInstalledEditors('/no/such/unity-hub-binary')).rejects.toThrow(
      /Failed to list installed editors/,
    );
  });

  describe.skipIf(process.platform === 'win32')('with a fake Hub executable', () => {
    let dir: string;
    beforeEach(() => {
      dir = fs.mkdtempSync(path.join(os.tmpdir(), 'fake-hub-'));
    });
    afterEach(() => {
      fs.rmSync(dir, { recursive: true, force: true });
    });

    function fakeHub(script: string): string {
      const file = path.join(dir, 'hub.sh');
      fs.writeFileSync(file, `#!/bin/sh\n${script}\n`, { mode: 0o755 });
      return file;
    }

    it('parses the installed editors the Hub prints, without blocking the event loop', async () => {
      const hub = fakeHub([
        'sleep 0.2',
        'echo "6000.6.4f1 (Apple silicon) installed at /Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app"',
        'echo "2022.3.62f3 , installed at /opt/Unity/2022.3.62f3"',
      ].join('\n'));
      let ticks = 0;
      const timer = setInterval(() => ticks++, 20);
      try {
        expect(await queryInstalledEditors(hub)).toEqual([
          { version: '6000.6.4f1', path: '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app' },
          { version: '2022.3.62f3', path: '/opt/Unity/2022.3.62f3' },
        ]);
      } finally {
        clearInterval(timer);
      }
      expect(ticks).toBeGreaterThan(2);
    });

    it('keeps the list a Hub printed before it hung', async () => {
      const hub = fakeHub('echo "6000.6.4f1 , installed at /opt/Unity/6000.6.4f1"\nexec sleep 30');
      expect(await queryInstalledEditors(hub, 500)).toEqual([{ version: '6000.6.4f1', path: '/opt/Unity/6000.6.4f1' }]);
    });

    it('gives up on a Hub that does not answer within the timeout', async () => {
      const hub = fakeHub('exec sleep 30');
      const startedAt = Date.now();
      await expect(queryInstalledEditors(hub, 300)).rejects.toThrow(/did not answer within/);
      expect(Date.now() - startedAt).toBeLessThan(5_000);
    });
  });
});

describe('runEditorCreateProject — direct contract', () => {
  it('rejects (never throws synchronously) with a wrapped error when the editor binary is unrunnable', async () => {
    // Drives the real sync->async refactor: a non-existent editor exe makes
    // execFile call back with an error, which must surface as a rejected
    // promise carrying the "Failed to create project" prefix.
    await expect(
      runEditorCreateProject('/no/such/unity-editor-binary', 'some/throwaway/project'),
    ).rejects.toThrow(/Failed to create project/);
  });
});
