import chalk from 'chalk';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

// ---------------------------------------------------------------------------
// Agent Definition
// ---------------------------------------------------------------------------

export interface AgentDefinition {
  id: string;
  name: string;
  skillsPath: string | null;
  configPathDisplay: string;
  configFormat: 'json' | 'toml';
  bodyPath: string;
  getConfigPath(projectPath: string): string;
  getStdioProps(
    serverPath: string,
    port: number,
    timeout: number,
    auth: string,
    token: string,
  ): Record<string, unknown>;
  getHttpProps(
    url: string,
    token: string,
    authRequired: boolean,
  ): Record<string, unknown>;
  stdioRemoveKeys: string[];
  httpRemoveKeys: string[];
}

// ---------------------------------------------------------------------------
// Platform helpers
// ---------------------------------------------------------------------------

function appData(): string {
  return process.env['APPDATA'] ?? path.join(os.homedir(), 'AppData', 'Roaming');
}

function home(): string {
  return os.homedir();
}

function isWindows(): boolean {
  return process.platform === 'win32';
}

function isMac(): boolean {
  return process.platform === 'darwin';
}

// ---------------------------------------------------------------------------
// Shared stdio / http arg builders
// ---------------------------------------------------------------------------

function stdioArgs(
  port: number,
  timeout: number,
  auth: string,
  token: string,
): string[] {
  return [
    `port=${port}`,
    `plugin-timeout=${timeout}`,
    `client-transport=stdio`,
    `authorization=${auth}`,
    `token=${token}`,
  ];
}

function authHeaders(
  token: string,
  authRequired: boolean,
): Record<string, string> | undefined {
  if (authRequired && token) {
    return { Authorization: `Bearer ${token}` };
  }
  return undefined;
}

// ---------------------------------------------------------------------------
// Agent Registry
// ---------------------------------------------------------------------------

const MCP_SERVER_NAME = 'ai-game-developer';

export const agentRegistry: readonly AgentDefinition[] = [
  // ── Claude Code ──────────────────────────────────────────────
  {
    id: 'claude-code',
    name: 'Claude Code',
    skillsPath: '.claude/skills',
    configPathDisplay: '.mcp.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: (p) => path.join(p, '.mcp.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'http',
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['type', 'url'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── Claude Desktop ───────────────────────────────────────────
  {
    id: 'claude-desktop',
    name: 'Claude Desktop',
    skillsPath: null,
    configPathDisplay: '~/Claude/claude_desktop_config.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: () =>
      isWindows()
        ? path.join(appData(), 'Claude', 'claude_desktop_config.json')
        : path.join(
            home(),
            'Library',
            'Application Support',
            'Claude',
            'claude_desktop_config.json',
          ),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      type: 'stdio',
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'http',
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── Cursor ───────────────────────────────────────────────────
  {
    id: 'cursor',
    name: 'Cursor',
    skillsPath: '.cursor/skills',
    configPathDisplay: '.cursor/mcp.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: (p) => path.join(p, '.cursor', 'mcp.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      type: 'stdio',
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'http',
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── VS Code (Copilot) ───────────────────────────────────────
  {
    id: 'vscode-copilot',
    name: 'Visual Studio Code (Copilot)',
    skillsPath: '.github/skills',
    configPathDisplay: '.vscode/mcp.json',
    configFormat: 'json',
    bodyPath: 'servers',
    getConfigPath: (p) => path.join(p, '.vscode', 'mcp.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      type: 'stdio',
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'http',
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── Visual Studio (Copilot) ──────────────────────────────────
  {
    id: 'vs-copilot',
    name: 'Visual Studio (Copilot)',
    skillsPath: '.github/skills',
    configPathDisplay: '.vs/mcp.json',
    configFormat: 'json',
    bodyPath: 'servers',
    getConfigPath: (p) => path.join(p, '.vs', 'mcp.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      type: 'stdio',
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'http',
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── Rider (Junie) ───────────────────────────────────────────
  {
    id: 'rider-junie',
    name: 'Rider (Junie)',
    skillsPath: '.junie/skills',
    configPathDisplay: '.junie/mcp/mcp.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: (p) => path.join(p, '.junie', 'mcp', 'mcp.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      enabled: true,
      type: 'stdio',
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      enabled: true,
      type: 'http',
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['disabled', 'url'],
    httpRemoveKeys: ['disabled', 'command', 'args'],
  },

  // ── GitHub Copilot CLI ──────────────────────────────────────
  {
    id: 'github-copilot-cli',
    name: 'GitHub Copilot CLI',
    skillsPath: '.github/skills',
    configPathDisplay: '~/.copilot/mcp-config.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: () => path.join(home(), '.copilot', 'mcp-config.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
      tools: ['*'],
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'http',
      url,
      tools: ['*'],
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url', 'type'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── Gemini ──────────────────────────────────────────────────
  {
    id: 'gemini',
    name: 'Gemini',
    skillsPath: '.gemini/skills',
    configPathDisplay: '.gemini/settings.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: (p) => path.join(p, '.gemini', 'settings.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      type: 'stdio',
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'http',
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── Antigravity ─────────────────────────────────────────────
  {
    id: 'antigravity',
    name: 'Antigravity',
    skillsPath: '.agent/skills',
    configPathDisplay: '~/.gemini/antigravity/mcp_config.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: () =>
      path.join(home(), '.gemini', 'antigravity', 'mcp_config.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      disabled: false,
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, _token, _authRequired) => ({
      disabled: false,
      serverUrl: url,
    }),
    stdioRemoveKeys: ['url', 'serverUrl', 'type'],
    httpRemoveKeys: ['command', 'args', 'url', 'type'],
  },

  // ── Cline ───────────────────────────────────────────────────
  {
    id: 'cline',
    name: 'Cline',
    skillsPath: '.cline/skills',
    configPathDisplay: '~/Code/globalStorage/.../cline_mcp_settings.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: () => {
      if (isWindows()) {
        return path.join(
          appData(),
          'Code',
          'User',
          'globalStorage',
          'saoudrizwan.claude-dev',
          'settings',
          'cline_mcp_settings.json',
        );
      }
      const base = isMac()
        ? path.join(
            home(),
            'Library',
            'Application Support',
            'Code',
            'User',
            'globalStorage',
          )
        : path.join(home(), '.config', 'Code', 'User', 'globalStorage');
      return path.join(
        base,
        'saoudrizwan.claude-dev',
        'settings',
        'cline_mcp_settings.json',
      );
    },
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      type: 'stdio',
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'streamableHttp',
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── Open Code ───────────────────────────────────────────────
  {
    id: 'open-code',
    name: 'Open Code',
    skillsPath: '.opencode/skills',
    configPathDisplay: 'opencode.json',
    configFormat: 'json',
    bodyPath: 'mcp',
    getConfigPath: (p) => path.join(p, 'opencode.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      type: 'local',
      enabled: true,
      command: [
        serverPath,
        ...stdioArgs(port, timeout, auth, token),
      ],
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'remote',
      enabled: true,
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url', 'args'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── Codex ───────────────────────────────────────────────────
  {
    id: 'codex',
    name: 'Codex',
    skillsPath: '.agents/skills',
    configPathDisplay: '~/.codex/config.toml',
    configFormat: 'toml',
    bodyPath: 'mcp_servers',
    getConfigPath: () => path.join(home(), '.codex', 'config.toml'),
    getStdioProps: (serverPath, port, timeout, auth, _token) => ({
      enabled: true,
      command: serverPath,
      args: [
        `port=${port}`,
        `plugin-timeout=${timeout}`,
        `client-transport=stdio`,
        `authorization=${auth}`,
      ],
      tool_timeout_sec: 300,
    }),
    getHttpProps: (url, _token, _authRequired) => ({
      enabled: true,
      url,
      tool_timeout_sec: 300,
      startup_timeout_sec: 30,
    }),
    stdioRemoveKeys: ['url', 'type', 'startup_timeout_sec'],
    httpRemoveKeys: ['command', 'args', 'type'],
  },

  // ── Kilo Code ───────────────────────────────────────────────
  {
    id: 'kilo-code',
    name: 'Kilo Code',
    skillsPath: '.kilocode/skills',
    configPathDisplay: '.kilocode/mcp.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: (p) => path.join(p, '.kilocode', 'mcp.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      type: 'stdio',
      disabled: false,
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'streamable-http',
      disabled: false,
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url'],
    httpRemoveKeys: ['command', 'args'],
  },

  // ── Unity AI ────────────────────────────────────────────────
  {
    id: 'unity-ai',
    name: 'Unity AI',
    skillsPath: null,
    configPathDisplay: 'UserSettings/mcp.json',
    configFormat: 'json',
    bodyPath: 'mcpServers',
    getConfigPath: (p) => path.join(p, 'UserSettings', 'mcp.json'),
    getStdioProps: (serverPath, port, timeout, auth, token) => ({
      type: 'stdio',
      command: serverPath,
      args: stdioArgs(port, timeout, auth, token),
    }),
    getHttpProps: (url, token, authRequired) => ({
      type: 'http',
      url,
      ...(authHeaders(token, authRequired)
        ? { headers: authHeaders(token, authRequired) }
        : {}),
    }),
    stdioRemoveKeys: ['url'],
    httpRemoveKeys: ['command', 'args'],
  },
] as const;

// ---------------------------------------------------------------------------
// Lookup helpers
// ---------------------------------------------------------------------------

export function getAgentById(id: string): AgentDefinition | undefined {
  return agentRegistry.find((a) => a.id === id);
}

export function getAgentIds(): string[] {
  return agentRegistry.map((a) => a.id);
}

export function listAgentTable(
  heading: string,
  locationLabel: string,
  locationFn: (agent: AgentDefinition) => string,
): void {
  const sorted = [...agentRegistry].sort((a, b) => a.id.localeCompare(b.id));

  const colId = 'ID';
  const colLoc = locationLabel;

  const wId = Math.max(colId.length, ...sorted.map((a) => a.id.length));
  const wLoc = Math.max(colLoc.length, ...sorted.map((a) => locationFn(a).length));

  const sep = chalk.dim;
  const hBar = (w: number) => '\u2500'.repeat(w);

  console.log(`\n${chalk.bold.cyan(heading)}\n`);

  // Header
  console.log(
    sep('  \u250C\u2500') + sep(hBar(wId)) + sep('\u2500\u252C\u2500') + sep(hBar(wLoc)) + sep('\u2500\u2510'),
  );
  console.log(
    sep('  \u2502 ') + chalk.bold.white(colId.padEnd(wId)) + sep(' \u2502 ') + chalk.bold.white(colLoc.padEnd(wLoc)) + sep(' \u2502'),
  );
  console.log(
    sep('  \u251C\u2500') + sep(hBar(wId)) + sep('\u2500\u253C\u2500') + sep(hBar(wLoc)) + sep('\u2500\u2524'),
  );

  // Rows
  for (const agent of sorted) {
    const loc = locationFn(agent);
    console.log(
      sep('  \u2502 ') + chalk.yellow(agent.id.padEnd(wId)) + sep(' \u2502 ') + chalk.green(loc.padEnd(wLoc)) + sep(' \u2502'),
    );
  }

  // Footer
  console.log(
    sep('  \u2514\u2500') + sep(hBar(wId)) + sep('\u2500\u2534\u2500') + sep(hBar(wLoc)) + sep('\u2500\u2518'),
  );
  console.log('');
}

// ---------------------------------------------------------------------------
// Server binary path resolution
// ---------------------------------------------------------------------------

export function resolveServerBinaryPath(projectPath: string): string {
  const platform = resolveServerPlatform();
  const ext = isWindows() ? '.exe' : '';
  return path.join(
    projectPath,
    'Library',
    'mcp-server',
    platform,
    `unity-mcp-server${ext}`,
  );
}

function resolveServerPlatform(): string {
  const p = process.platform;
  const a = process.arch;
  if (p === 'win32') return 'win-x64';
  if (p === 'darwin') return a === 'arm64' ? 'osx-arm64' : 'osx-x64';
  return 'linux-x64';
}

// ---------------------------------------------------------------------------
// Config file writing — JSON
// ---------------------------------------------------------------------------

export function writeJsonAgentConfig(
  configPath: string,
  bodyPath: string,
  serverName: string,
  props: Record<string, unknown>,
  removeKeys: string[],
): void {
  const dir = path.dirname(configPath);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }

  let root: Record<string, unknown> = {};
  if (fs.existsSync(configPath)) {
    try {
      root = JSON.parse(fs.readFileSync(configPath, 'utf-8')) as Record<
        string,
        unknown
      >;
    } catch {
      // If the file is malformed, start fresh
      root = {};
    }
    if (!root || typeof root !== 'object' || Array.isArray(root)) {
      root = {};
    }
  }

  // Navigate/create bodyPath
  let body = root[bodyPath] as Record<string, unknown> | undefined;
  if (!body || typeof body !== 'object' || Array.isArray(body)) {
    body = {};
    root[bodyPath] = body;
  }

  // Remove deprecated "Unity-MCP" entries
  delete body['Unity-MCP'];

  // Get or create the server entry
  let entry = body[serverName] as Record<string, unknown> | undefined;
  if (!entry || typeof entry !== 'object' || Array.isArray(entry)) {
    entry = {};
  }

  // Remove stale keys
  for (const key of removeKeys) {
    delete entry[key];
  }

  // Merge new properties
  for (const [key, value] of Object.entries(props)) {
    entry[key] = value;
  }

  body[serverName] = entry;
  root[bodyPath] = body;

  fs.writeFileSync(configPath, JSON.stringify(root, null, 2) + '\n');
}

// ---------------------------------------------------------------------------
// Config file writing — TOML (Codex only)
// ---------------------------------------------------------------------------

export function writeTomlAgentConfig(
  configPath: string,
  bodyPath: string,
  serverName: string,
  props: Record<string, unknown>,
  removeKeys: string[],
): void {
  const dir = path.dirname(configPath);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }

  // Read existing content or start fresh
  let lines: string[] = [];
  if (fs.existsSync(configPath)) {
    lines = fs.readFileSync(configPath, 'utf-8').split('\n');
  }

  const sectionHeader = `[${bodyPath}.${serverName}]`;

  // Find existing section boundaries
  const sectionIdx = lines.findIndex(
    (l) => l.trim() === sectionHeader,
  );

  // Build TOML key-value pairs for the section
  const tomlLines = [sectionHeader];
  for (const [key, value] of Object.entries(props)) {
    if (removeKeys.includes(key)) continue;
    tomlLines.push(`${key} = ${tomlValue(value)}`);
  }

  if (sectionIdx >= 0) {
    // Find end of section (next [...] header or EOF)
    let endIdx = sectionIdx + 1;
    while (endIdx < lines.length && !lines[endIdx].trim().startsWith('[')) {
      endIdx++;
    }
    // Replace section
    lines.splice(sectionIdx, endIdx - sectionIdx, ...tomlLines);
  } else {
    // Append section
    if (lines.length > 0 && lines[lines.length - 1].trim() !== '') {
      lines.push('');
    }
    lines.push(...tomlLines);
  }

  fs.writeFileSync(configPath, lines.join('\n') + '\n');
}

function tomlValue(v: unknown): string {
  if (typeof v === 'string') return `"${v.replace(/\\/g, '\\\\').replace(/"/g, '\\"')}"`;
  if (typeof v === 'number' || typeof v === 'boolean') return String(v);
  if (Array.isArray(v)) {
    return `[${v.map(tomlValue).join(', ')}]`;
  }
  return String(v);
}

export { MCP_SERVER_NAME };
