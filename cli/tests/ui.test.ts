import { describe, it, expect, vi, afterEach } from 'vitest';

describe('TTY detection', () => {
  const originalIsTTY = process.stdout.isTTY;

  afterEach(() => {
    Object.defineProperty(process.stdout, 'isTTY', { value: originalIsTTY, writable: true });
  });

  it('startSpinner returns no-op spinner when stdout is not a TTY', async () => {
    Object.defineProperty(process.stdout, 'isTTY', { value: false, writable: true });
    // Re-import to pick up the TTY state
    const ui = await import('../src/utils/ui.js');
    const spinner = ui.startSpinner('test');
    // No-op spinner should have success/error/warning/info methods that don't throw
    expect(() => spinner.success('done')).not.toThrow();
    expect(() => spinner.error('fail')).not.toThrow();
  });

  it('createProgressBar outputs plain text in non-TTY mode', async () => {
    Object.defineProperty(process.stdout, 'isTTY', { value: false, writable: true });
    const ui = await import('../src/utils/ui.js');
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
    const stderrSpy = vi.spyOn(process.stderr, 'write').mockImplementation(() => true);

    const bar = ui.createProgressBar();
    bar.update(50, 'Installing');
    bar.complete('Done');

    // In non-TTY mode, should use console.log for plain text (no ANSI cursor control)
    const allOutput = [
      ...consoleSpy.mock.calls.map(c => String(c[0])),
      ...stderrSpy.mock.calls.map(c => String(c[0])),
    ].join('\n');

    // Should contain percentage or status text
    expect(allOutput).toContain('Done');

    consoleSpy.mockRestore();
    stderrSpy.mockRestore();
  });
});

describe('verbose output', () => {
  it('verbose() outputs nothing when verbose mode is disabled', async () => {
    const ui = await import('../src/utils/ui.js');
    ui.setVerbose(false);
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

    ui.verbose('test message');

    const verboseCalls = consoleSpy.mock.calls.filter(
      call => String(call[0]).includes('[verbose]')
    );
    expect(verboseCalls.length).toBe(0);

    consoleSpy.mockRestore();
  });

  it('verbose() outputs with [verbose] prefix when enabled', async () => {
    const ui = await import('../src/utils/ui.js');
    ui.setVerbose(true);
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

    ui.verbose('test message');

    const verboseCalls = consoleSpy.mock.calls.filter(
      call => String(call[0]).includes('[verbose]')
    );
    expect(verboseCalls.length).toBe(1);
    expect(String(verboseCalls[0][0])).toContain('test message');

    consoleSpy.mockRestore();
    ui.setVerbose(false);
  });
});
