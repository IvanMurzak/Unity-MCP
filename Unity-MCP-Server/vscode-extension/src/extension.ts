import * as vscode from 'vscode';
import { ChildProcess, spawn } from 'child_process';
import * as serverBinaryManager from './serverBinaryManager';

let mcpServerProcess: ChildProcess | null = null;
let outputChannel: vscode.OutputChannel;
let statusBarItem: vscode.StatusBarItem;

export async function activate(context: vscode.ExtensionContext) {
    console.log('Unity MCP extension is now active');

    // Create output channel
    outputChannel = vscode.window.createOutputChannel('Unity MCP');

    // Create status bar item
    statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    statusBarItem.command = 'unityMcp.showStatus';
    context.subscriptions.push(statusBarItem);

    // Register commands
    context.subscriptions.push(
        vscode.commands.registerCommand('unityMcp.start', () => startServer(context)),
        vscode.commands.registerCommand('unityMcp.stop', stopServer),
        vscode.commands.registerCommand('unityMcp.restart', () => restartServer(context)),
        vscode.commands.registerCommand('unityMcp.showStatus', showStatus)
    );

    // Watch for configuration changes
    context.subscriptions.push(
        vscode.workspace.onDidChangeConfiguration(async (e) => {
            if (e.affectsConfiguration('unityMcp.port') ||
                e.affectsConfiguration('unityMcp.pluginTimeout') ||
                e.affectsConfiguration('unityMcp.clientTransport')) {

                if (mcpServerProcess) {
                    outputChannel.appendLine('Configuration changed, restarting server...');
                    vscode.window.showInformationMessage('Unity MCP configuration changed. Restarting server...');
                    await restartServer(context);
                }
            }
        })
    );

    // Ensure server binary exists and is up to date
    const extensionVersion = context.extension.packageJSON.version;
    outputChannel.appendLine(`Extension version: ${extensionVersion}`);

    updateStatusBar('stopped');
    statusBarItem.text = '$(sync~spin) Unity MCP (Initializing...)';
    statusBarItem.show();

    const binaryReady = await serverBinaryManager.ensureServerBinaryExists(
        context,
        extensionVersion,
        outputChannel
    );

    if (!binaryReady) {
        vscode.window.showErrorMessage(
            'Failed to download Unity MCP Server binary. Please check the output channel for details.',
            'Show Output'
        ).then(selection => {
            if (selection === 'Show Output') {
                outputChannel.show();
            }
        });
        updateStatusBar('error');
        return;
    }

    // Auto-start if enabled
    const config = vscode.workspace.getConfiguration('unityMcp');
    if (config.get<boolean>('autoStart', true)) {
        startServer(context);
    } else {
        updateStatusBar('stopped');
    }
}

export function deactivate() {
    stopServer();
    if (outputChannel) {
        outputChannel.dispose();
    }
    if (statusBarItem) {
        statusBarItem.dispose();
    }
}

function getServerExecutablePath(context: vscode.ExtensionContext): string {
    return serverBinaryManager.getServerExecutablePath(context);
}

async function startServer(context: vscode.ExtensionContext) {
    if (mcpServerProcess) {
        vscode.window.showWarningMessage('Unity MCP Server is already running');
        return;
    }

    try {
        const config = vscode.workspace.getConfiguration('unityMcp');
        const port = config.get<number>('port', 8080);
        const pluginTimeout = config.get<number>('pluginTimeout', 10000);
        const clientTransport = config.get<string>('clientTransport', 'stdio');

        const executablePath = getServerExecutablePath(context);

        outputChannel.appendLine(`Starting Unity MCP Server...`);
        outputChannel.appendLine(`Executable: ${executablePath}`);
        outputChannel.appendLine(`Port: ${port}`);
        outputChannel.appendLine(`Transport: ${clientTransport}`);

        const args = [
            `--port=${port}`,
            `--plugin-timeout=${pluginTimeout}`,
            `--client-transport=${clientTransport}`
        ];

        mcpServerProcess = spawn(executablePath, args);

        mcpServerProcess.stdout?.on('data', (data) => {
            outputChannel.appendLine(`[STDOUT] ${data.toString()}`);
        });

        mcpServerProcess.stderr?.on('data', (data) => {
            outputChannel.appendLine(`[STDERR] ${data.toString()}`);
        });

        mcpServerProcess.on('error', (error) => {
            outputChannel.appendLine(`[ERROR] ${error.message}`);
            vscode.window.showErrorMessage(`Unity MCP Server error: ${error.message}`);
            updateStatusBar('error');
        });

        mcpServerProcess.on('exit', (code, signal) => {
            outputChannel.appendLine(`[EXIT] Server process exited with code ${code} and signal ${signal}`);
            mcpServerProcess = null;
            updateStatusBar('stopped');

            if (code !== 0 && code !== null) {
                vscode.window.showErrorMessage(`Unity MCP Server stopped unexpectedly with code ${code}`);
            }
        });

        updateStatusBar('running');
        vscode.window.showInformationMessage('Unity MCP Server started successfully');
        outputChannel.show();

    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        outputChannel.appendLine(`[ERROR] Failed to start server: ${errorMessage}`);
        vscode.window.showErrorMessage(`Failed to start Unity MCP Server: ${errorMessage}`);
        updateStatusBar('error');
    }
}

function stopServer() {
    if (!mcpServerProcess) {
        vscode.window.showWarningMessage('Unity MCP Server is not running');
        return;
    }

    outputChannel.appendLine('Stopping Unity MCP Server...');
    mcpServerProcess.kill();
    mcpServerProcess = null;
    updateStatusBar('stopped');
    vscode.window.showInformationMessage('Unity MCP Server stopped');
}

async function restartServer(context: vscode.ExtensionContext) {
    outputChannel.appendLine('Restarting Unity MCP Server...');
    stopServer();

    // Wait a bit before restarting
    await new Promise(resolve => setTimeout(resolve, 1000));

    await startServer(context);
}

function showStatus() {
    const isRunning = mcpServerProcess !== null;
    const config = vscode.workspace.getConfiguration('unityMcp');
    const port = config.get<number>('port', 8080);
    const transport = config.get<string>('clientTransport', 'stdio');

    const status = `Unity MCP Server Status:

Status: ${isRunning ? '✅ Running' : '❌ Stopped'}
Port: ${port}
Transport: ${transport}
PID: ${mcpServerProcess?.pid || 'N/A'}`;

    vscode.window.showInformationMessage(status, { modal: true });
}

function updateStatusBar(status: 'running' | 'stopped' | 'error') {
    switch (status) {
        case 'running':
            statusBarItem.text = '$(play) Unity MCP';
            statusBarItem.backgroundColor = undefined;
            statusBarItem.tooltip = 'Unity MCP Server is running';
            break;
        case 'stopped':
            statusBarItem.text = '$(debug-stop) Unity MCP';
            statusBarItem.backgroundColor = undefined;
            statusBarItem.tooltip = 'Unity MCP Server is stopped';
            break;
        case 'error':
            statusBarItem.text = '$(error) Unity MCP';
            statusBarItem.backgroundColor = new vscode.ThemeColor('statusBarItem.errorBackground');
            statusBarItem.tooltip = 'Unity MCP Server error';
            break;
    }
    statusBarItem.show();
}
