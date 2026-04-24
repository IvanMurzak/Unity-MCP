import { describe, expect, it } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');

function readRepoFile(relativePath: string): string {
  return fs.readFileSync(path.join(repoRoot, relativePath), 'utf-8');
}

describe('dev environment CI/CD handoff v1 policy', () => {
  it('documents only the two default human approval gates for v1', () => {
    const runbook = readRepoFile('cli/docs/dev-env-cicd-handoff-runbook.md');

    expect(runbook).toContain('Only the leader mutates lifecycle state.');
    expect(runbook).toContain('`plan -> execution` | **Required**');
    expect(runbook).toContain('`verification -> CI/CD` | **Required**');
    expect(runbook).toContain('`execution -> verification` | Not required by default');
    expect(runbook).toContain('`CI/CD result -> release/recovery` | Not required by default');
    expect(runbook).toContain('do not add new mandatory human approval gates in v1');
  });

  it('keeps chat approval scoped to known handoff approvals, not runtime control', () => {
    const runbook = readRepoFile('cli/docs/dev-env-cicd-handoff-runbook.md');

    expect(runbook).toContain('Offer only approve/reject controls for that exact handoff ID and record version.');
    expect(runbook).toContain('Free-form command execution or direct Unity runtime control');
    expect(runbook).toContain('Provider differences may affect transport and bootstrap only.');
  });

  it('maps the approved verification gate to the manual PR test workflow relay', () => {
    const runbook = readRepoFile('cli/docs/dev-env-cicd-handoff-runbook.md');
    const workflow = readRepoFile('.github/workflows/test_pull_request_manual.yml');

    expect(runbook).toContain('`.github/workflows/test_pull_request_manual.yml`');
    expect(workflow).toContain('repository_dispatch:');
    expect(workflow).toContain('types: [unity-mcp-approved-verification]');
    expect(workflow).toContain('APPROVAL_GATE: ${{ github.event.client_payload.approval_gate }}');
    expect(workflow).toContain('test "$APPROVAL_GATE" = "verification_to_cicd"');
    expect(workflow).toContain('needs: [approval-relay-context]');
    expect(workflow).toContain('workflow_dispatch is an operator fallback');
  });
});
