import { describe, it, expect } from 'vitest';
import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';
import { fileURLToPath } from 'node:url';
import { addPackageToManifest } from '../src/utils/manifest.js';
import { UNITY_EXTENSION_COMPATIBILITY, unityExtensionDependencies } from '../src/utils/extension-compatibility.js';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const policy = JSON.parse(fs.readFileSync(path.join(root,
  'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.compatibility.json'), 'utf8'));

describe('extension package compatibility', () => {
  it('the shipped policy and CLI mirror match exactly', () => {
    expect(policy.minimumUnityVersion).toBe('6000.6');
    expect(UNITY_EXTENSION_COMPATIBILITY).toEqual(Object.fromEntries(
      policy.extensions.map((entry: { packageId: string; dependencies: Record<string, string> }) => [entry.packageId, entry.dependencies])));
  });

  for (const [extension, dependencies] of Object.entries(UNITY_EXTENSION_COMPATIBILITY)) {
    it(`${extension} upgrades only required old pins on supported editors and stays idempotent`, () => {
      const project = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-compatibility-'));
      try {
        fs.mkdirSync(path.join(project, 'Packages'));
        fs.mkdirSync(path.join(project, 'ProjectSettings'));
        fs.writeFileSync(path.join(project, 'ProjectSettings/ProjectVersion.txt'), 'm_EditorVersion: 6000.6.3f1\n');
        const manifest = path.join(project, 'Packages/manifest.json');
        fs.writeFileSync(manifest, JSON.stringify({ dependencies: Object.fromEntries(Object.keys(dependencies).map(key => [key, '0.0.1'])) }));
        addPackageToManifest(project, extension, '2.0.1');
        expect(JSON.parse(fs.readFileSync(manifest, 'utf8')).dependencies).toMatchObject(dependencies);
        const first = fs.readFileSync(manifest, 'utf8');
        expect(addPackageToManifest(project, extension, '2.0.1').modified).toBe(false);
        expect(fs.readFileSync(manifest, 'utf8')).toBe(first);
        for (const value of ['99.0.0', 'https://example.invalid/custom.git#pinned']) {
          fs.writeFileSync(manifest, JSON.stringify({ dependencies: Object.fromEntries(Object.keys(dependencies).map(key => [key, value])) }));
          addPackageToManifest(project, extension, '2.0.1');
          expect(JSON.parse(fs.readFileSync(manifest, 'utf8')).dependencies).toMatchObject(
            Object.fromEntries(Object.keys(dependencies).map(key => [key, value])));
        }
        for (const version of ['2022.3.62f3', '2023.2.22f1', '6000.3.1f1', '6000.5.0f1', 'unknown']) {
          expect(unityExtensionDependencies(extension, version)).toEqual({});
        }
        expect(unityExtensionDependencies(extension, '6000.10.0f1')).toEqual(dependencies);
      } finally { fs.rmSync(project, { recursive: true, force: true }); }
    });
  }
});
