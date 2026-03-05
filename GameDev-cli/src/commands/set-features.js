import path from 'path';
import { readConfig, writeConfig } from '../utils/config.js';

/**
 * Parses a comma-separated string into a trimmed, non-empty string array.
 * @param {string} value
 * @returns {string[]}
 */
function parseCommaSeparated(value) {
  return value.split(',').map(s => s.trim()).filter(Boolean);
}

/**
 * Builds a feature list for the config.
 *
 * If `enabledNames` is provided and non-empty, the resulting list contains
 * exactly those entries as enabled. Any name also present in `disabledNames`
 * is set to disabled.
 *
 * If no `enabledNames` are given but `disabledNames` are, the existing config
 * list is updated: existing entries are preserved, and the disabled names are
 * forced to disabled (new entries are created if they did not exist).
 *
 * An empty `existing` list combined with `--disable` will only add the
 * disabled entries (Unity interprets an absent entry as enabled by default).
 *
 * @param {Array<{name:string,enabled:boolean}>} existing - Current list from config
 * @param {string[]} enabledNames - Positional names to enable (replaces list)
 * @param {string[]} disabledNames - Names to explicitly disable
 * @returns {Array<{name:string,enabled:boolean}>}
 */
function buildFeatureList(existing, enabledNames, disabledNames) {
  const disabledSet = new Set(disabledNames);

  if (enabledNames.length > 0) {
    // Replace the whole list: enable the specified names, disable the rest if
    // they were also passed via --disable.
    const enabledSet = new Set(enabledNames);
    const allNames = new Set([...enabledSet, ...disabledSet]);
    return Array.from(allNames).map(name => ({
      name,
      enabled: enabledSet.has(name) && !disabledSet.has(name),
    }));
  }

  if (disabledNames.length > 0) {
    // Patch the existing list: update/add disabled entries, leave others intact.
    const result = existing.map(f => ({
      name: f.name,
      enabled: disabledSet.has(f.name) ? false : f.enabled,
    }));
    for (const name of disabledSet) {
      if (!result.some(f => f.name === name)) {
        result.push({ name, enabled: false });
      }
    }
    return result;
  }

  // Should not reach here given the caller validates input.
  return existing;
}

/**
 * Registers a set-<featureType>s command for the given feature type.
 *
 * @param {import('commander').Command} program
 * @param {'tool'|'prompt'|'resource'} featureType
 */
function registerSetFeaturesCommand(program, featureType) {
  const plural = `${featureType}s`;
  const configKey = plural; // config.tools / config.prompts / config.resources

  program
    .command(`set-${plural} <project-path> [names...]`)
    .description(
      `Set the enabled/disabled list of MCP ${plural} in AI-Game-Developer-Config.json.\n\n` +
      `Examples:\n` +
      `  gamedev set-${plural} ./MyGame assets-find gameobject-create   # enable only these\n` +
      `  gamedev set-${plural} ./MyGame --disable script-execute        # disable one, keep rest\n` +
      `  gamedev set-${plural} ./MyGame --all                           # enable all (clear list)`
    )
    .option('--all', `Enable all ${plural} (writes an empty list — Unity enables everything by default)`)
    .option(
      '--disable <names>',
      `Comma-separated list of ${plural} to disable (can be combined with positional names)`,
      parseCommaSeparated
    )
    .action((projectPath, names, options) => {
      const absPath = path.resolve(projectPath);
      const config = readConfig(absPath);
      const existing = Array.isArray(config[configKey]) ? config[configKey] : [];

      if (options.all) {
        config[configKey] = [];
        console.log(`All MCP ${plural} enabled (list cleared).`);
      } else if (names.length > 0 || (options.disable && options.disable.length > 0)) {
        config[configKey] = buildFeatureList(existing, names, options.disable ?? []);
        if (names.length > 0) {
          console.log(`MCP ${plural} set to enabled: [${names.join(', ')}]`);
        }
        if (options.disable?.length > 0) {
          console.log(`MCP ${plural} set to disabled: [${options.disable.join(', ')}]`);
        }
      } else {
        console.error(
          `No ${plural} specified.\n` +
          `Use --all to enable all, provide names as arguments to enable specific ones,\n` +
          `or use --disable <names> to disable specific ones.`
        );
        process.exit(1);
      }

      writeConfig(absPath, config);
    });
}

/**
 * Registers set-tools, set-prompts and set-resources commands.
 * @param {import('commander').Command} program
 */
export function registerSetFeaturesCommands(program) {
  registerSetFeaturesCommand(program, 'tool');
  registerSetFeaturesCommand(program, 'prompt');
  registerSetFeaturesCommand(program, 'resource');
}
