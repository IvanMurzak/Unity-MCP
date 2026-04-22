import * as path from 'path';
import {
  getOrCreateConfig,
  writeConfig,
  updateFeatures,
  type McpFeature,
  type UnityConnectionConfig,
} from '../utils/config.js';
import { emitProgress } from './progress.js';
import { requireExistingPath } from './validation.js';
import type {
  ConfigureOptions,
  ConfigureResult,
  FeatureAction,
  McpFeatureSnapshot,
} from './types.js';

const CONFIG_RELATIVE_PATH = 'UserSettings/AI-Game-Developer-Config.json';

function hasAnyAction(action: FeatureAction | undefined): boolean {
  if (!action) return false;
  return (
    (Array.isArray(action.enableNames) && action.enableNames.length > 0) ||
    (Array.isArray(action.disableNames) && action.disableNames.length > 0) ||
    action.enableAll === true ||
    action.disableAll === true
  );
}

function snapshotFeatures(config: UnityConnectionConfig, key: 'tools' | 'prompts' | 'resources'): McpFeatureSnapshot[] {
  const raw = config[key];
  if (!Array.isArray(raw)) return [];
  return raw
    .filter(
      (f): f is McpFeature =>
        typeof f === 'object' && f !== null && typeof f.name === 'string' && typeof f.enabled === 'boolean',
    )
    .map((f) => ({ name: f.name, enabled: f.enabled }));
}

/**
 * Configure Unity-MCP features (tools / prompts / resources) for a
 * Unity project. Library-safe: no stdout noise, no process.exit.
 *
 * If none of `tools`/`prompts`/`resources` are supplied, the call is a
 * read-only "create-if-missing and return snapshot" — mirrors the CLI's
 * `--list` behaviour minus the rendering.
 */
export async function configure(opts: ConfigureOptions): Promise<ConfigureResult> {
  const warnings: string[] = [];

  try {
    const validated = requireExistingPath(opts?.unityProjectPath);
    if (!validated.ok) {
      return { success: false, warnings, error: validated.error };
    }
    const { projectPath } = validated;

    const configPath = path.join(projectPath, CONFIG_RELATIVE_PATH);

    emitProgress(opts.onProgress, { phase: 'start', message: `Configuring Unity-MCP features for ${projectPath}` });

    const config = getOrCreateConfig(projectPath);

    const hasToolsAction = hasAnyAction(opts.tools);
    const hasPromptsAction = hasAnyAction(opts.prompts);
    const hasResourcesAction = hasAnyAction(opts.resources);

    if (hasToolsAction) {
      updateFeatures(config, 'tools', opts.tools!);
    }
    if (hasPromptsAction) {
      updateFeatures(config, 'prompts', opts.prompts!);
    }
    if (hasResourcesAction) {
      updateFeatures(config, 'resources', opts.resources!);
    }

    if (hasToolsAction || hasPromptsAction || hasResourcesAction) {
      writeConfig(projectPath, config);
      emitProgress(opts.onProgress, {
        phase: 'manifest-patched',
        message: `Wrote ${configPath}`,
        manifestPath: configPath,
      });
    }

    emitProgress(opts.onProgress, { phase: 'done', message: 'Configure complete.' });

    return {
      success: true,
      configPath,
      snapshot: {
        host: typeof config.host === 'string' ? config.host : undefined,
        keepConnected: typeof config.keepConnected === 'boolean' ? config.keepConnected : undefined,
        transportMethod: typeof config.transportMethod === 'string' ? config.transportMethod : undefined,
        authOption: typeof config.authOption === 'string' ? config.authOption : undefined,
        tools: snapshotFeatures(config, 'tools'),
        prompts: snapshotFeatures(config, 'prompts'),
        resources: snapshotFeatures(config, 'resources'),
      },
      warnings,
    };
  } catch (err: unknown) {
    return {
      success: false,
      warnings,
      error: err instanceof Error ? err : new Error(String(err)),
    };
  }
}
