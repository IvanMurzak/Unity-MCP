import fs from 'fs';
import path from 'path';

export const CONFIG_RELATIVE_PATH = path.join('Assets', 'Resources', 'AI-Game-Developer-Config.json');

export const DEFAULT_CONFIG = {
  logLevel: 3,
  tools: [],
  prompts: [],
  resources: [],
  host: 'http://localhost:8080',
  timeoutMs: 10000,
  keepConnected: true,
};

/**
 * Returns the absolute path to the AI-Game-Developer-Config.json file
 * for the given Unity project path.
 * @param {string} projectPath - Absolute path to the Unity project root
 * @returns {string} Absolute path to the config file
 */
export function getConfigPath(projectPath) {
  return path.join(projectPath, CONFIG_RELATIVE_PATH);
}

/**
 * Reads the AI-Game-Developer-Config.json from the Unity project.
 * Creates a default config if the file does not exist or is invalid.
 * @param {string} projectPath - Absolute path to the Unity project root
 * @returns {object} Parsed config object
 */
export function readConfig(projectPath) {
  const configPath = getConfigPath(projectPath);
  if (fs.existsSync(configPath)) {
    try {
      const json = fs.readFileSync(configPath, 'utf-8');
      const parsed = JSON.parse(json);
      return { ...DEFAULT_CONFIG, ...parsed };
    } catch (err) {
      console.warn(`Warning: Could not parse config at ${configPath}: ${err.message}`);
      console.warn('Using default configuration.');
    }
  }
  return { ...DEFAULT_CONFIG };
}

/**
 * Writes the config object to the AI-Game-Developer-Config.json file.
 * Creates the Assets/Resources directory if it does not exist.
 * @param {string} projectPath - Absolute path to the Unity project root
 * @param {object} config - Config object to serialize
 */
export function writeConfig(projectPath, config) {
  const configPath = getConfigPath(projectPath);
  const dir = path.dirname(configPath);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
  fs.writeFileSync(configPath, JSON.stringify(config, null, 2) + '\n', 'utf-8');
  console.log(`Config saved: ${configPath}`);
}
