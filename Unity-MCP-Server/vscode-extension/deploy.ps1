#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Builds and deploys Unity MCP Server VS Code extension to the marketplace.

.DESCRIPTION
    This script automates the complete deployment process:
    1. Builds the .NET MCP server for all platforms
    2. Copies binaries to the extension folder
    3. Installs npm dependencies
    4. Compiles TypeScript code
    5. Packages the extension (.vsix)
    6. Publishes to VS Code marketplace

.PARAMETER SkipBuild
    Skip building the .NET server binaries (use existing binaries)

.PARAMETER SkipPublish
    Only create the .vsix package without publishing

.PARAMETER Token
    Personal Access Token for VS Code marketplace (if not set, will prompt)

.EXAMPLE
    .\deploy.ps1
    Full build and publish

.EXAMPLE
    .\deploy.ps1 -SkipBuild
    Use existing binaries and publish

.EXAMPLE
    .\deploy.ps1 -SkipPublish
    Only create .vsix package without publishing
#>

param(
    [switch]$SkipBuild,
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
$ServerDir = Split-Path -Parent $ScriptDir
$ExtensionDir = Join-Path $ScriptDir ""
$PublishDir = Join-Path $ServerDir "publish"
$ServerBinariesDir = Join-Path $ExtensionDir "server"

Write-Host @"
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║     Unity MCP VS Code Extension Deployment Script          ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Magenta

# Step 1: Build .NET Server (unless skipped)
if (-not $SkipBuild) {
    Write-Step "Building .NET MCP Server for all platforms..."

    Push-Location $ServerDir
    try {
        $buildScript = Join-Path $ServerDir "build-all.ps1"
        if (Test-Path $buildScript) {
            & $buildScript Release
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed with exit code $LASTEXITCODE"
            }
            Write-Success "Server binaries built successfully"
        }
        else {
            Write-Error-Custom "Build script not found: $buildScript"
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Step "Skipping .NET build (using existing binaries)"
}

# Step 2: Copy binaries to extension folder
Write-Step "Copying server binaries to extension folder..."

if (-not (Test-Path $PublishDir)) {
    Write-Error-Custom "Publish directory not found: $PublishDir"
    exit 1
}

# Remove old binaries
if (Test-Path $ServerBinariesDir) {
    Remove-Item -Path $ServerBinariesDir -Recurse -Force
}

# Create server directory
New-Item -ItemType Directory -Path $ServerBinariesDir -Force | Out-Null

# Copy all platform binaries
$platforms = @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")

foreach ($platform in $platforms) {
    $sourcePath = Join-Path $PublishDir $platform
    $destPath = Join-Path $ServerBinariesDir $platform

    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        Write-Success "Copied $platform binaries"
    }
    else {
        Write-Warning "Platform binaries not found: $platform (skipping)"
    }
}

# Step 3: Install npm dependencies
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

# Step 4: Compile TypeScript
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

# Step 5: Package extension
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

# Step 6: Publish to marketplace (unless skipped)
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
