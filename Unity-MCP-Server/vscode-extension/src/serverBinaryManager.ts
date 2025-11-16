/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

import * as vscode from 'vscode';
import * as path from 'path';
import * as os from 'os';
import * as fs from 'fs';
import * as https from 'https';
import AdmZip = require('adm-zip');

const EXECUTABLE_NAME = 'unity-mcp-server';

/**
 * Gets the operating system name for the download URL
 */
function getOperatingSystem(): string {
    const platform = os.platform();
    switch (platform) {
        case 'win32': return 'win';
        case 'darwin': return 'osx';
        case 'linux': return 'linux';
        default: throw new Error(`Unsupported platform: ${platform}`);
    }
}

/**
 * Gets the CPU architecture name for the download URL
 */
function getCpuArchitecture(): string {
    const arch = os.arch();
    switch (arch) {
        case 'x64': return 'x64';
        case 'ia32': return 'x86';
        case 'arm': return 'arm';
        case 'arm64': return 'arm64';
        default: throw new Error(`Unsupported architecture: ${arch}`);
    }
}

/**
 * Gets the platform name (e.g., "win-x64", "osx-arm64")
 */
export function getPlatformName(): string {
    return `${getOperatingSystem()}-${getCpuArchitecture()}`;
}

/**
 * Gets the executable file name with extension
 */
export function getExecutableFileName(): string {
    return os.platform() === 'win32'
        ? `${EXECUTABLE_NAME}.exe`
        : EXECUTABLE_NAME;
}

/**
 * Gets the folder path where server binaries are stored
 * Uses VS Code's global storage which is outside the extension folder
 * and won't be tracked by git
 */
export function getServerBinaryFolder(context: vscode.ExtensionContext): string {
    return path.join(
        context.globalStorageUri.fsPath,
        'server',
        getPlatformName()
    );
}

/**
 * Gets the full path to the server executable
 */
export function getServerExecutablePath(context: vscode.ExtensionContext): string {
    return path.join(
        getServerBinaryFolder(context),
        getExecutableFileName()
    );
}

/**
 * Gets the full path to the version file
 */
export function getVersionFilePath(context: vscode.ExtensionContext): string {
    return path.join(
        getServerBinaryFolder(context),
        'version'
    );
}

/**
 * Gets the download URL for the server binary
 */
export function getDownloadUrl(version: string): string {
    const platformName = getPlatformName();
    return `https://github.com/IvanMurzak/Unity-MCP/releases/download/${version}/${EXECUTABLE_NAME}-${platformName}.zip`;
}

/**
 * Checks if the server binary exists
 */
export function isBinaryExists(context: vscode.ExtensionContext): boolean {
    const executablePath = getServerExecutablePath(context);
    return fs.existsSync(executablePath);
}

/**
 * Checks if the version file matches the current extension version
 */
export function isVersionMatches(context: vscode.ExtensionContext, currentVersion: string): boolean {
    const versionFilePath = getVersionFilePath(context);

    if (!fs.existsSync(versionFilePath)) {
        return false;
    }

    try {
        const existingVersion = fs.readFileSync(versionFilePath, 'utf-8').trim();
        return existingVersion === currentVersion;
    } catch (error) {
        return false;
    }
}

/**
 * Deletes the server binary folder if it exists
 */
export function deleteServerBinaryFolder(context: vscode.ExtensionContext, outputChannel: vscode.OutputChannel): void {
    const binaryFolder = path.join(context.globalStorageUri.fsPath, 'server');

    if (fs.existsSync(binaryFolder)) {
        fs.rmSync(binaryFolder, { recursive: true, force: true });
        outputChannel.appendLine(`Deleted existing server folder: ${binaryFolder}`);
    }
}

/**
 * Downloads a file from a URL to a destination path
 */
function downloadFile(url: string, destPath: string): Promise<void> {
    return new Promise((resolve, reject) => {
        const file = fs.createWriteStream(destPath);

        https.get(url, (response) => {
            // Handle redirects
            if (response.statusCode === 301 || response.statusCode === 302) {
                const redirectUrl = response.headers.location;
                if (redirectUrl) {
                    file.close();
                    fs.unlinkSync(destPath);
                    downloadFile(redirectUrl, destPath).then(resolve).catch(reject);
                    return;
                }
            }

            if (response.statusCode !== 200) {
                file.close();
                fs.unlinkSync(destPath);
                reject(new Error(`Failed to download: HTTP ${response.statusCode}`));
                return;
            }

            response.pipe(file);

            file.on('finish', () => {
                file.close();
                resolve();
            });
        }).on('error', (err) => {
            file.close();
            fs.unlinkSync(destPath);
            reject(err);
        });

        file.on('error', (err) => {
            file.close();
            fs.unlinkSync(destPath);
            reject(err);
        });
    });
}

/**
 * Sets executable permissions on Unix systems (chmod 0755)
 */
function setExecutablePermissions(filePath: string): void {
    if (os.platform() !== 'win32') {
        fs.chmodSync(filePath, 0o755);
    }
}

/**
 * Downloads and unpacks the server binary
 */
export async function downloadAndUnpackBinary(
    context: vscode.ExtensionContext,
    version: string,
    outputChannel: vscode.OutputChannel
): Promise<boolean> {
    const downloadUrl = getDownloadUrl(version);
    const platformName = getPlatformName();

    outputChannel.appendLine(`Downloading Unity-MCP-Server binary from: ${downloadUrl}`);

    try {
        // Clear existing server folder
        deleteServerBinaryFolder(context, outputChannel);

        // Create binary folder if needed
        const binaryFolder = getServerBinaryFolder(context);
        if (!fs.existsSync(binaryFolder)) {
            fs.mkdirSync(binaryFolder, { recursive: true });
        }

        // Download to temp location
        const tempDir = os.tmpdir();
        const archiveFilePath = path.join(tempDir, `${EXECUTABLE_NAME}-${platformName}-${version}.zip`);

        outputChannel.appendLine(`Temporary archive file path: ${archiveFilePath}`);

        // Download the zip file
        await downloadFile(downloadUrl, archiveFilePath);
        outputChannel.appendLine('Download completed');

        // Extract zip archive
        outputChannel.appendLine(`Unpacking Unity-MCP-Server binary to: ${binaryFolder}`);
        const zip = new AdmZip(archiveFilePath);

        // Extract to the parent folder (context.globalStorageUri.fsPath/server)
        const extractPath = path.join(context.globalStorageUri.fsPath, 'server');
        zip.extractAllTo(extractPath, true);

        // Clean up temp file
        fs.unlinkSync(archiveFilePath);

        // Verify extraction
        const executablePath = getServerExecutablePath(context);
        if (!fs.existsSync(executablePath)) {
            outputChannel.appendLine(`ERROR: Binary file not found at: ${executablePath}`);
            return false;
        }

        outputChannel.appendLine(`Downloaded and unpacked Unity-MCP-Server binary to: ${executablePath}`);

        // Set executable permissions on Unix systems
        if (os.platform() !== 'win32') {
            outputChannel.appendLine(`Setting executable permission for: ${executablePath}`);
            setExecutablePermissions(executablePath);
        }

        // Write version file
        const versionFilePath = getVersionFilePath(context);
        fs.writeFileSync(versionFilePath, version, 'utf-8');
        outputChannel.appendLine('MCP server version file created - COMPLETED');

        return isBinaryExists(context) && isVersionMatches(context, version);

    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        outputChannel.appendLine(`ERROR: Failed to download and unpack server binary: ${errorMessage}`);
        if (error instanceof Error && error.stack) {
            outputChannel.appendLine(error.stack);
        }
        return false;
    }
}

/**
 * Ensures the server binary is downloaded if needed
 * Returns true if binary is ready, false otherwise
 */
export async function ensureServerBinaryExists(
    context: vscode.ExtensionContext,
    version: string,
    outputChannel: vscode.OutputChannel
): Promise<boolean> {
    // Check if binary exists and version matches
    if (isBinaryExists(context) && isVersionMatches(context, version)) {
        outputChannel.appendLine('Server binary exists and version matches');
        return true;
    }

    // Download and unpack
    outputChannel.appendLine('Server binary needs to be downloaded or updated');
    return await downloadAndUnpackBinary(context, version, outputChannel);
}
