#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Deploys Unity MCP Server VS Code extension to the marketplace.

.DESCRIPTION
    This script automates the deployment process:
    1. Installs npm dependencies
    2. Compiles TypeScript code
    3. Packages the extension (.vsix)
    4. Publishes to VS Code marketplace

    NOTE: Server binaries are NOT included in the extension package.
    The extension downloads them dynamically from GitHub releases at runtime.
    Make sure the server binaries are already uploaded to GitHub releases
    before deploying a new extension version.

.PARAMETER SkipPublish
    Only create the .vsix package without publishing

.PARAMETER Token
    Personal Access Token for VS Code marketplace (if not set, will prompt)

.EXAMPLE
    .\deploy.ps1
    Package and publish to marketplace

.EXAMPLE
    .\deploy.ps1 -SkipPublish
    Only create .vsix package without publishing
#>

param(
    [switch]$SkipPublish,
    [string]$Token = $env:VSCE_PAT
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Step {
    param([string]$Message)
    Write-Host "`n===> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ExtensionDir = Join-Path $ScriptDir ""

Write-Host @"
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║     Unity MCP VS Code Extension Deployment Script          ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Magenta

Write-Host "`nNOTE: Server binaries are downloaded from GitHub releases at runtime." -ForegroundColor Yellow
Write-Host "Make sure binaries are uploaded to GitHub releases before publishing!`n" -ForegroundColor Yellow

# Step 1: Install npm dependencies
Write-Step "Installing npm dependencies..."

Push-Location $ExtensionDir
try {
    if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
        Write-Error-Custom "npm is not installed or not in PATH"
        exit 1
    }

    npm install
    if ($LASTEXITCODE -ne 0) {
        throw "npm install failed with exit code $LASTEXITCODE"
    }
    Write-Success "Dependencies installed"
}
finally {
    Pop-Location
}

# Step 2: Compile TypeScript
Write-Step "Compiling TypeScript code..."

Push-Location $ExtensionDir
try {
    npm run compile
    if ($LASTEXITCODE -ne 0) {
        throw "TypeScript compilation failed with exit code $LASTEXITCODE"
    }
    Write-Success "TypeScript compiled"
}
finally {
    Pop-Location
}

# Step 3: Package extension
Write-Step "Packaging VS Code extension..."

Push-Location $ExtensionDir
try {
    # Check if vsce is installed
    if (-not (Get-Command vsce -ErrorAction SilentlyContinue)) {
        Write-Host "Installing vsce (VS Code Extension Manager)..." -ForegroundColor Yellow
        npm install -g @vscode/vsce
    }

    # Package
    vsce package
    if ($LASTEXITCODE -ne 0) {
        throw "Packaging failed with exit code $LASTEXITCODE"
    }

    $vsixFile = Get-ChildItem -Path $ExtensionDir -Filter "*.vsix" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($vsixFile) {
        Write-Success "Extension packaged: $($vsixFile.Name)"
    }
    else {
        throw "No .vsix file found after packaging"
    }
}
finally {
    Pop-Location
}

# Step 4: Publish to marketplace (unless skipped)
if (-not $SkipPublish) {
    Write-Step "Publishing to VS Code Marketplace..."

    if ([string]::IsNullOrWhiteSpace($Token)) {
        Write-Host "No token provided. Please enter your Personal Access Token:" -ForegroundColor Yellow
        $Token = Read-Host -AsSecureString "PAT"
        $Token = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Token)
        )
    }

    Push-Location $ExtensionDir
    try {
        vsce publish -p $Token
        if ($LASTEXITCODE -ne 0) {
            throw "Publishing failed with exit code $LASTEXITCODE"
        }
        Write-Success "Extension published successfully!"
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Step "Skipping publish (package created only)"
}

Write-Host @"

╔════════════════════════════════════════════════════════════╗
║                                                            ║
║              Deployment Completed Successfully!            ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Green

if (-not $SkipPublish) {
    Write-Host "`nYour extension is now available on the VS Code Marketplace!" -ForegroundColor Cyan
    Write-Host "Visit: https://marketplace.visualstudio.com/publishers/IvanMurzak" -ForegroundColor Yellow
}
else {
    $vsixFile = Get-ChildItem -Path $ExtensionDir -Filter "*.vsix" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "`nYour extension package is ready: $($vsixFile.FullName)" -ForegroundColor Cyan
    Write-Host "You can install it locally with: code --install-extension $($vsixFile.Name)" -ForegroundColor Yellow
}
