import * as vscode from 'vscode';

export type LogLevel = 'error' | 'warn' | 'info' | 'debug';

const logLevelWeight: Record<LogLevel, number> = {
  error: 0,
  warn: 1,
  info: 2,
  debug: 3,
};

export class ExtensionLogger implements vscode.Disposable {
  private readonly channel = vscode.window.createOutputChannel('Unity MCP');

  public dispose(): void {
    this.channel.dispose();
  }

  public show(preserveFocus = false): void {
    this.channel.show(preserveFocus);
  }

  public error(event: string, details?: Record<string, unknown>): void {
    this.write('error', event, details);
  }

  public warn(event: string, details?: Record<string, unknown>): void {
    this.write('warn', event, details);
  }

  public info(event: string, details?: Record<string, unknown>): void {
    this.write('info', event, details);
  }

  public debug(event: string, details?: Record<string, unknown>): void {
    this.write('debug', event, details);
  }

  public appendReport(title: string, body: string): void {
    this.channel.appendLine('');
    this.channel.appendLine(`=== ${title} ===`);
    for (const line of body.split('\n')) {
      this.channel.appendLine(line);
    }
  }

  private write(level: LogLevel, event: string, details?: Record<string, unknown>): void {
    if (logLevelWeight[level] > logLevelWeight[this.currentLogLevel()]) {
      return;
    }

    const timestamp = new Date().toISOString();
    const suffix = details && Object.keys(details).length > 0
      ? ` ${serializeDetails(details)}`
      : '';

    this.channel.appendLine(`[${timestamp}] [${level}] ${event}${suffix}`);
  }

  private currentLogLevel(): LogLevel {
    const configured = vscode.workspace
      .getConfiguration('unityMcp')
      .get<string>('logLevel', 'info');

    if (configured === 'error' || configured === 'warn' || configured === 'info' || configured === 'debug') {
      return configured;
    }

    return 'info';
  }
}

function serializeDetails(details: Record<string, unknown>): string {
  return Object.entries(details)
    .map(([key, value]) => `${key}=${stringifyValue(value)}`)
    .join(' ');
}

function stringifyValue(value: unknown): string {
  if (value === undefined) {
    return 'undefined';
  }

  if (value === null) {
    return 'null';
  }

  if (typeof value === 'string') {
    return JSON.stringify(value);
  }

  if (typeof value === 'number' || typeof value === 'boolean') {
    return String(value);
  }

  if (Array.isArray(value)) {
    return `[${value.map((item) => stringifyValue(item)).join(',')}]`;
  }

  return JSON.stringify(value);
}
