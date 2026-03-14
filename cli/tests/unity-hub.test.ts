import { describe, it, expect } from 'vitest';
import {
  findLatestStableRelease,
  parseHubProgress,
  isValidUnityVersion,
  type AvailableRelease,
} from '../src/utils/unity-hub.js';

describe('isValidUnityVersion', () => {
  it('accepts standard version formats', () => {
    expect(isValidUnityVersion('2022.3.62f3')).toBe(true);
    expect(isValidUnityVersion('6000.3.1f1')).toBe(true);
    expect(isValidUnityVersion('2023.2.22f1')).toBe(true);
    expect(isValidUnityVersion('2019.4.40f1')).toBe(true);
  });

  it('accepts alpha/beta versions', () => {
    expect(isValidUnityVersion('2023.1.0a1')).toBe(true);
    expect(isValidUnityVersion('6000.1.0b2')).toBe(true);
  });

  it('rejects invalid formats', () => {
    expect(isValidUnityVersion('')).toBe(false);
    expect(isValidUnityVersion('latest')).toBe(false);
    expect(isValidUnityVersion('../../etc/passwd')).toBe(false);
    expect(isValidUnityVersion('6000.3.1')).toBe(false);
    expect(isValidUnityVersion('6000.3.1f')).toBe(false);
    expect(isValidUnityVersion('foo.bar.bazf1')).toBe(false);
    expect(isValidUnityVersion('6000.3.1f1; rm -rf /')).toBe(false);
    expect(isValidUnityVersion('6000.3.1f1\n')).toBe(false);
  });
});

describe('findLatestStableRelease', () => {
  it('returns null for empty array', () => {
    expect(findLatestStableRelease([])).toBeNull();
  });

  it('returns null when no stable releases exist', () => {
    const releases: AvailableRelease[] = [
      { version: '2023.1.0a1', isStable: false },
      { version: '6000.1.0b2', isStable: false },
    ];
    expect(findLatestStableRelease(releases)).toBeNull();
  });

  it('returns the highest stable version', () => {
    const releases: AvailableRelease[] = [
      { version: '2022.3.10f1', isStable: true },
      { version: '6000.3.1f1', isStable: true },
      { version: '2023.2.22f1', isStable: true },
      { version: '6000.4.0a1', isStable: false },
    ];
    const result = findLatestStableRelease(releases);
    expect(result).not.toBeNull();
    expect(result!.version).toBe('6000.3.1f1');
  });

  it('handles single stable release', () => {
    const releases: AvailableRelease[] = [
      { version: '2022.3.10f1', isStable: true },
    ];
    const result = findLatestStableRelease(releases);
    expect(result!.version).toBe('2022.3.10f1');
  });
});

describe('parseHubProgress', () => {
  it('returns null for empty/noise lines', () => {
    expect(parseHubProgress('')).toBeNull();
    expect(parseHubProgress('   ')).toBeNull();
    expect(parseHubProgress('Progress:')).toBeNull();
    expect(parseHubProgress('All Tasks Completed Successfully.')).toBeNull();
  });

  it('parses download percentage', () => {
    const result = parseHubProgress('[Unity (6000.3.11f1)] downloading 50.54%');
    expect(result).toEqual({ percent: 50.54, status: 'Downloading' });
  });

  it('parses 0% download', () => {
    const result = parseHubProgress('[Unity (6000.3.11f1)] downloading 0.00%');
    expect(result).toEqual({ percent: 0, status: 'Downloading' });
  });

  it('parses 100% download', () => {
    const result = parseHubProgress('[Unity (6000.3.11f1)] downloading 100.00%');
    expect(result).toEqual({ percent: 100, status: 'Downloading' });
  });

  it('parses validating download phase', () => {
    const result = parseHubProgress('[Unity (6000.3.11f1)] validating download...');
    expect(result).toEqual({ percent: 0, status: 'Validating download' });
  });

  it('parses installing phase', () => {
    const result = parseHubProgress('[Unity (6000.3.11f1)] installing...');
    expect(result).toEqual({ percent: 100, status: 'Installing' });
  });

  it('parses installed successfully', () => {
    const result = parseHubProgress('[Unity (6000.3.11f1)] installed successfully.');
    expect(result).toEqual({ percent: 100, status: 'Installed' });
  });

  it('parses finished downloading', () => {
    const result = parseHubProgress('[Unity (6000.3.11f1)] finished downloading');
    expect(result).toEqual({ percent: 100, status: 'Download complete' });
  });

  it('parses queued for install', () => {
    const result = parseHubProgress('[Unity (6000.3.11f1)] queued for install');
    expect(result).toEqual({ percent: 100, status: 'Queued for install' });
  });

  it('strips ANSI escape codes', () => {
    const result = parseHubProgress('\x1b[36m[Unity (6000.3.11f1)] downloading 25.00%\x1b[0m');
    expect(result).toEqual({ percent: 25, status: 'Downloading' });
  });

  it('returns generic status for unknown phases', () => {
    const result = parseHubProgress('[Unity (6000.3.11f1)] some unknown phase');
    expect(result).toEqual({ percent: 0, status: 'some unknown phase' });
  });

  it('returns null for lines without bracket prefix', () => {
    expect(parseHubProgress('just a random log line')).toBeNull();
  });
});
