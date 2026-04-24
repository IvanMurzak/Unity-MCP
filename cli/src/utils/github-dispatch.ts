import {
  createLeaderWriter,
  readHandoffRecord,
  transitionHandoffState,
  writeHandoffRecord,
} from './handoff-ledger.js';
import type { HandoffBridgeConfig } from './discord-approval.js';

const DEFAULT_EVENT_TYPE = 'unity-mcp-approved-verification';

export interface GitHubDispatchConfig {
  token: string;
  repository: string;
  eventType: string;
  apiBaseUrl: string;
}

export interface GitHubDispatchResult {
  eventType: string;
  dispatchId: string;
  target: string;
  createdAt: string;
}

export class GitHubDispatchError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'GitHubDispatchError';
  }
}

export function loadGitHubDispatchConfig(options: {
  env?: NodeJS.ProcessEnv;
  bridgeConfig: HandoffBridgeConfig;
}): GitHubDispatchConfig {
  const env = options.env ?? process.env;
  const token = env['UNITY_MCP_HANDOFF_GITHUB_TOKEN'];
  const repository = env['UNITY_MCP_HANDOFF_GITHUB_REPOSITORY'] ?? env['GITHUB_REPOSITORY'];
  const eventType = env['UNITY_MCP_HANDOFF_GITHUB_EVENT_TYPE'] ?? DEFAULT_EVENT_TYPE;
  const apiBaseUrl = env['UNITY_MCP_HANDOFF_GITHUB_API_BASE_URL'] ?? 'https://api.github.com';

  if (!token?.trim()) {
    throw new GitHubDispatchError('Missing UNITY_MCP_HANDOFF_GITHUB_TOKEN for repository_dispatch.');
  }
  if (!repository?.trim() || !repository.includes('/')) {
    throw new GitHubDispatchError('Missing UNITY_MCP_HANDOFF_GITHUB_REPOSITORY for repository_dispatch.');
  }

  return {
    token,
    repository,
    eventType,
    apiBaseUrl: apiBaseUrl.replace(/\/$/u, ''),
  };
}

export async function dispatchApprovedVerificationHandoff(input: {
  projectPath: string;
  handoffId: string;
  bridgeConfig: HandoffBridgeConfig;
  env?: NodeJS.ProcessEnv;
  fetchImpl?: typeof fetch;
}): Promise<GitHubDispatchResult> {
  const config = loadGitHubDispatchConfig({ env: input.env, bridgeConfig: input.bridgeConfig });
  const writer = createLeaderWriter(input.bridgeConfig.handoffLeaderActor);
  const record = readHandoffRecord(input.projectPath, input.handoffId);

  if (record.state !== 'approved_not_dispatched') {
    throw new GitHubDispatchError(`Handoff ${record.handoffId} must be approved_not_dispatched before dispatch; current state is ${record.state}.`);
  }
  if (record.requestedAction !== 'verification_to_cicd') {
    throw new GitHubDispatchError(`Handoff ${record.handoffId} is not a verification_to_cicd gate.`);
  }

  const payload = {
    event_type: config.eventType,
    client_payload: {
      handoff_id: record.handoffId,
      record_version: record.recordVersion,
      approval_gate: record.requestedAction,
      dispatch_provenance: `github:${config.repository}:${config.eventType}`,
    },
  };

  const response = await (input.fetchImpl ?? fetch)(`${config.apiBaseUrl}/repos/${config.repository}/dispatches`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${config.token}`,
      'Accept': 'application/vnd.github+json',
      'Content-Type': 'application/json',
      'User-Agent': 'unity-mcp-cli',
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const responseText = await response.text();
    throw new GitHubDispatchError(`GitHub repository_dispatch failed (${response.status}): ${responseText}`);
  }

  const createdAt = new Date().toISOString();
  const result: GitHubDispatchResult = {
    eventType: config.eventType,
    dispatchId: `github:${record.handoffId}:${record.recordVersion}:${createdAt}`,
    target: config.repository,
    createdAt,
  };

  const nextRecord = transitionHandoffState(record, writer, 'dispatched', {
    updatedAt: createdAt,
    notes: `repository_dispatch ${config.eventType} -> ${config.repository}`,
    dispatchProvenance: {
      provider: 'github',
      target: `${config.repository}:${config.eventType}`,
      dispatchId: result.dispatchId,
      createdAt,
    },
  });
  writeHandoffRecord(input.projectPath, writer, nextRecord);

  return result;
}
