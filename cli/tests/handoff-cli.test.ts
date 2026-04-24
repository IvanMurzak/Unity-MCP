import { describe, expect, it } from 'vitest';
import { runCliAsync } from './helpers/cli.js';

describe('handoff command surface', () => {
  it('shows the handoff subcommands in help output', async () => {
    const result = await runCliAsync(['handoff', '--help']);

    expect(result.exitCode).toBe(0);
    expect(result.stdout).toContain('serve');
    expect(result.stdout).toContain('notify-discord');
    expect(result.stdout).toContain('dispatch-approved');
  });
});
