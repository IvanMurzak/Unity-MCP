#!/usr/bin/env pwsh
# Universal PowerShell script to build self-contained executables for all platforms
# Works on Windows, macOS, and Linux with PowerShell Core

param(
    [string]$Configuration = "Release",
    [string]$ProjectFile = "com.IvanMurzak.Unity.MCP.Server.csproj",
    [string[]]$Platforms = @()
)

Write-Host "Building self-contained executables..." -ForegroundColor Green

# Root output directory (relative to this script location)
$PublishRoot = Join-Path $PSScriptRoot "publish"
if (Test-Path $PublishRoot) {
    Write-Host "Cleaning existing publish folder..." -ForegroundColor Cyan
    try {
        Remove-Item $PublishRoot -Recurse -Force -ErrorAction Stop
    }
    catch {
        Write-Host "Failed to clean publish folder: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}
New-Item -ItemType Directory -Path $PublishRoot | Out-Null

$allRuntimes = @(
    "win-x64",
    "win-x86",
    "win-arm64",
    "linux-x64",
    "linux-arm64",
    "osx-x64",
    "osx-arm64"
)

$runtimes = if ($Platforms.Count -gt 0) {
    $allRuntimes | Where-Object { $_ -in $Platforms }
} else {
    $allRuntimes
}

if ($runtimes.Count -eq 0 -and $Platforms.Count -gt 0) {
    Write-Host "No valid runtimes found matching: $($Platforms -join ', ')" -ForegroundColor Red
    Write-Host "Available runtimes: $($allRuntimes -join ', ')" -ForegroundColor Cyan
    exit 1
}

Write-Host "Target runtimes: $($runtimes -join ', ')" -ForegroundColor Cyan

$success = 0
$failed = 0

foreach ($runtime in $runtimes) {
    Write-Host "Building for $runtime..." -ForegroundColor Yellow

    $outputPath = Join-Path $PublishRoot $runtime
    if (-not (Test-Path $outputPath)) { New-Item -ItemType Directory -Path $outputPath | Out-Null }

    $publishArgs = @(
        "publish",
        $ProjectFile,
        "-c", $Configuration,
        "-r", $runtime,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-o", $outputPath
    )

    Write-Host "Executing: dotnet $($publishArgs -join ' ')" -ForegroundColor DarkGray

    try {
        dotnet @publishArgs
        $exitCode = $LASTEXITCODE
    }
    catch {
        Write-Host "Error executing dotnet publish: $($_.Exception.Message)" -ForegroundColor Red
        $exitCode = 1
    }

    if ($exitCode -eq 0) {
        Write-Host "✅ Successfully built $runtime" -ForegroundColor Green
        $success++
    }
    else {
        Write-Host "❌ Failed to build $runtime (exit code: $exitCode)" -ForegroundColor Red
        $failed++
    }
}

Write-Host "`nBuild Summary:" -ForegroundColor Cyan
Write-Host "Success: $success" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor Red

if ($failed -gt 0) {
    Write-Host "`n⚠️ Some builds failed. Check the output above." -ForegroundColor Yellow
    exit 1
}

Write-Host "`n🎉 All builds completed successfully!" -ForegroundColor Green
Write-Host "Executables are located in: $PublishRoot" -ForegroundColor Yellow
Write-Host "Per-platform folders: ./publish/{runtime}/" -ForegroundColor Yellow

Write-Host "`n📦 Creating zip archives for each runtime..." -ForegroundColor Cyan

$zipSuccess = 0
$zipFailed = 0

foreach ($runtime in $runtimes) {
    $runtimePath = Join-Path $PublishRoot $runtime

    if (Test-Path $runtimePath) {
        Write-Host "🗜️  Creating zip for $runtime..." -ForegroundColor Yellow

        $zipName = "unity-mcp-server-$runtime.zip"
        $zipPath = Join-Path $PublishRoot $zipName

        try {
            if (Test-Path $zipPath) {
                Remove-Item $zipPath -Force
            }

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($runtimePath, $zipPath)

            Write-Host "✅ Successfully created $zipName" -ForegroundColor Green
            $zipSuccess++
        }
        catch {
            Write-Host "❌ Failed to create $zipName : $($_.Exception.Message)" -ForegroundColor Red
            $zipFailed++
        }
    }
    else {
        Write-Host "⚠️  Skipping $runtime - directory not found" -ForegroundColor Yellow
        $zipFailed++
    }
}

Write-Host "`n📊 Zip Creation Summary:" -ForegroundColor Cyan
Write-Host "Success: $zipSuccess" -ForegroundColor Green
Write-Host "Failed: $zipFailed" -ForegroundColor Red

if ($zipFailed -eq 0) {
    Write-Host "`n🎉 All zip archives created successfully!" -ForegroundColor Green
    Write-Host "📁 Zip files are located in: $PublishRoot" -ForegroundColor Yellow
    Write-Host "📋 Created files:" -ForegroundColor Cyan

    $zipFiles = Get-ChildItem -Path $PublishRoot -Filter "*.zip" -ErrorAction SilentlyContinue
    if ($zipFiles) {
        foreach ($zipFile in $zipFiles) {
            $sizeKB = [math]::Round($zipFile.Length / 1KB, 2)
            Write-Host "  $($zipFile.Name) ($sizeKB KB)" -ForegroundColor White
        }
    }
} else {
    Write-Host "`n❌ $zipFailed zip archive(s) failed to create. See errors above." -ForegroundColor Red
    exit 1
}
