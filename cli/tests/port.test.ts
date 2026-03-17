import { describe, it, expect } from 'vitest';
import { generatePortFromDirectory } from '../src/utils/port.js';

describe('generatePortFromDirectory', () => {
  it('returns a port within the valid range (20000-29999)', () => {
    const port = generatePortFromDirectory('/some/test/path');
    expect(port).toBeGreaterThanOrEqual(20000);
    expect(port).toBeLessThanOrEqual(29999);
  });

  it('is deterministic — same input always produces same output', () => {
    const path = '/home/user/MyUnityProject';
    const port1 = generatePortFromDirectory(path);
    const port2 = generatePortFromDirectory(path);
    expect(port1).toBe(port2);
  });

  it('is case-insensitive — different cases produce same port', () => {
    const port1 = generatePortFromDirectory('/Home/User/Project');
    const port2 = generatePortFromDirectory('/home/user/project');
    expect(port1).toBe(port2);
  });

  it('produces different ports for different paths', () => {
    const port1 = generatePortFromDirectory('/path/to/project-a');
    const port2 = generatePortFromDirectory('/path/to/project-b');
    expect(port1).not.toBe(port2);
  });

  it('handles empty string', () => {
    const port = generatePortFromDirectory('');
    expect(port).toBeGreaterThanOrEqual(20000);
    expect(port).toBeLessThanOrEqual(29999);
  });

  it('handles Windows-style paths', () => {
    const port = generatePortFromDirectory('C:\\Users\\dev\\MyProject');
    expect(port).toBeGreaterThanOrEqual(20000);
    expect(port).toBeLessThanOrEqual(29999);
  });

  it('handles paths with spaces', () => {
    const port = generatePortFromDirectory('/home/user/My Unity Project');
    expect(port).toBeGreaterThanOrEqual(20000);
    expect(port).toBeLessThanOrEqual(29999);
  });

  it('handles very long paths', () => {
    const longPath = '/a'.repeat(1000);
    const port = generatePortFromDirectory(longPath);
    expect(port).toBeGreaterThanOrEqual(20000);
    expect(port).toBeLessThanOrEqual(29999);
  });

  it('produces an integer', () => {
    const port = generatePortFromDirectory('/test');
    expect(Number.isInteger(port)).toBe(true);
  });
});
