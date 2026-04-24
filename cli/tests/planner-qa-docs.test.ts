import { describe, expect, it } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');

function readRepoFile(relativePath: string): string {
  return fs.readFileSync(path.join(repoRoot, relativePath), 'utf-8');
}

describe('planner + QA first-slice docs', () => {
  it('documents planner and QA as bounded roles using relatedHandoffRecordVersion', () => {
    const doc = readRepoFile('cli/docs/planner-qa-first-slice.md');

    expect(doc).toContain('`planner`');
    expect(doc).toContain('`qa`');
    expect(doc).toContain('relatedHandoffRecordVersion');
    expect(doc).toContain('Planner and QA are not new canonical lanes.');
    expect(doc).toContain('GitHub Issues remain ops tracking only.');
  });

  it('keeps QA gate behavior tied to existing leader-owned gates only', () => {
    const handoffDoc = readRepoFile('cli/docs/dev-env-cicd-handoff-v1.md');
    const contractDoc = readRepoFile('docs/approval-handoff-contract.md');

    expect(handoffDoc).toContain('Planner + QA bounded role types');
    expect(handoffDoc).toContain('QA outputs feed existing leader-owned readiness decisions');
    expect(contractDoc).toContain('Planner + QA review placement');
    expect(contractDoc).toContain('QA does not become a separate approval plane');
    expect(contractDoc).toContain('Discord still handles only notify + approve/reject');
  });

  it('keeps artist and map-designer deferred in the first slice', () => {
    const doc = readRepoFile('cli/docs/planner-qa-first-slice.md');

    expect(doc).toContain('Deferred for later:');
    expect(doc).toContain('`artist`');
    expect(doc).toContain('`map-designer`');
  });
});
