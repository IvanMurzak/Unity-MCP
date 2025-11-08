#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Run GitHub Actions workflow locally using act with secrets from .env file
.DESCRIPTION
    This script reads Unity secrets from a .env file and runs the specified
    GitHub Actions workflow job using the act command with enhanced debugging capabilities.
.PARAMETER JobName
    The name of the job to run (default: test-unity-2022-3-61f1-standalone)
.PARAMETER WorkflowFile
    The workflow file to run (default: ./.github/workflows/test_pull_request_manual.yml)
.PARAMETER Verbose
    Enable verbose output from act
.PARAMETER DryRun
    Show what would be executed without actually running it
.PARAMETER LogFile
    Path to save log output (optional)
.PARAMETER StepDebug
    Enable GitHub Actions step debugging (ACTIONS_STEP_DEBUG=true)
.EXAMPLE
    .\run-act-test.ps1
    .\run-act-test.ps1 -JobName test-unity-2023-2-20f1-editmode
    .\run-act-test.ps1 -Verbose -StepDebug
    .\run-act-test.ps1 -DryRun
    .\run-act-test.ps1 -LogFile "./act-logs.txt"
#>

param(
    [string]$JobName = "test-unity-2022-3-61f1-standalone",
    [string]$WorkflowFile = "./.github/workflows/test_pull_request_manual.yml",
    [switch]$Verbose,
    [switch]$DryRun,
    [string]$LogFile,
    [switch]$StepDebug
)

# Set location to repository root (parent of commands folder)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
Push-Location $repoRoot

# ============================================================================
# Pre-flight checks
# ============================================================================

Write-Host "=== Pre-flight Checks ===" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is installed
try {
    $dockerVersion = docker --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Docker installed: $dockerVersion" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Docker is not responding properly" -ForegroundColor Red
        Pop-Location
        exit 1
    }
} catch {
    Write-Host "[ERROR] Docker is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install Docker Desktop: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    Pop-Location
    exit 1
}

# Check if Docker daemon is running
try {
    $dockerPs = docker ps 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Docker daemon is running" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Docker daemon is not running" -ForegroundColor Red
        Write-Host "Please start Docker Desktop" -ForegroundColor Yellow
        Pop-Location
        exit 1
    }
} catch {
    Write-Host "[ERROR] Cannot connect to Docker daemon" -ForegroundColor Red
    Write-Host "Please start Docker Desktop" -ForegroundColor Yellow
    Pop-Location
    exit 1
}

# Check if act is installed
try {
    $actVersion = act --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Act installed: $actVersion" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Act is not responding properly" -ForegroundColor Red
        Pop-Location
        exit 1
    }
} catch {
    Write-Host "[ERROR] Act is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install act: https://github.com/nektos/act" -ForegroundColor Yellow
    Pop-Location
    exit 1
}

Write-Host ""

# ============================================================================
# Load secrets from .env file
# ============================================================================

# Path to .env file (in commands folder)
$envFile = "./commands/.env"

# Check if .env file exists
if (-not (Test-Path $envFile)) {
    Write-Host "Error: .env file not found at $envFile" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please create a .env file in the commands folder with the following format:" -ForegroundColor Yellow
    Write-Host "UNITY_LICENSE=<your-unity-license-content>"
    Write-Host "UNITY_EMAIL=<your-unity-email>"
    Write-Host "UNITY_PASSWORD=<your-unity-password>"
    Write-Host ""
    Write-Host "You can copy commands/.env.example to commands/.env and fill in your values" -ForegroundColor Yellow
    Pop-Location
    exit 1
}

# Read .env file and parse key-value pairs
$secrets = @{}
Get-Content $envFile | ForEach-Object {
    $line = $_.Trim()
    # Skip empty lines and comments
    if ($line -and -not $line.StartsWith("#")) {
        # Split on first = only
        $parts = $line -split "=", 2
        if ($parts.Count -eq 2) {
            $key = $parts[0].Trim()
            $value = $parts[1].Trim()
            # Remove quotes if present
            $value = $value.Trim('"').Trim("'")
            $secrets[$key] = $value
        }
    }
}

# Check required secrets
$requiredSecrets = @("UNITY_LICENSE", "UNITY_EMAIL", "UNITY_PASSWORD")
$missingSecrets = @()

foreach ($secret in $requiredSecrets) {
    if (-not $secrets.ContainsKey($secret) -or [string]::IsNullOrWhiteSpace($secrets[$secret])) {
        $missingSecrets += $secret
    }
}

if ($missingSecrets.Count -gt 0) {
    Write-Host "Error: Missing required secrets in .env file:" -ForegroundColor Red
    $missingSecrets | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Pop-Location
    exit 1
}

# ============================================================================
# Build act command
# ============================================================================

# Base command
$actCommand = "act workflow_dispatch -j $JobName -W $WorkflowFile"

# Add secrets
foreach ($key in $secrets.Keys) {
    $actCommand += " -s $key=`"$($secrets[$key])`""
}

# Add debugging flags
if ($Verbose) {
    $actCommand += " --verbose"
}

if ($DryRun) {
    $actCommand += " --dryrun"
}

if ($StepDebug) {
    $actCommand += " --env ACTIONS_STEP_DEBUG=true"
}

# Add artifact support
$artifactPath = "./.act-artifacts"
$actCommand += " --artifact-server-path `"$artifactPath`""

# Add container architecture for consistency
$actCommand += " --container-architecture linux/amd64"

# ============================================================================
# Display execution info
# ============================================================================

Write-Host "=== Execution Info ===" -ForegroundColor Cyan
Write-Host "Job:      $JobName" -ForegroundColor White
Write-Host "Workflow: $WorkflowFile" -ForegroundColor White
Write-Host "Secrets:  $($secrets.Keys -join ', ')" -ForegroundColor Green
Write-Host "Verbose:  $Verbose" -ForegroundColor White
Write-Host "DryRun:   $DryRun" -ForegroundColor White
Write-Host "StepDebug: $StepDebug" -ForegroundColor White
if ($LogFile) {
    Write-Host "LogFile:  $LogFile" -ForegroundColor White
}
Write-Host "Artifacts: $artifactPath" -ForegroundColor White
Write-Host ""

# ============================================================================
# Execute the command
# ============================================================================

Write-Host "=== Running Act ===" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date

if ($LogFile) {
    # Execute with log file
    Invoke-Expression "$actCommand 2>&1 | Tee-Object -FilePath `"$LogFile`""
    $exitCode = $LASTEXITCODE
} else {
    # Execute normally
    Invoke-Expression $actCommand
    $exitCode = $LASTEXITCODE
}

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
Write-Host "=== Execution Complete ===" -ForegroundColor Cyan
Write-Host "Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor White
Write-Host "Exit Code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })

# ============================================================================
# Post-execution diagnostics
# ============================================================================

if ($exitCode -ne 0) {
    Write-Host ""
    Write-Host "=== Troubleshooting Tips ===" -ForegroundColor Yellow
    Write-Host ""

    # Explain exit code
    if ($exitCode -eq 127) {
        Write-Host "Exit code 127 typically means 'Command not found'" -ForegroundColor Yellow
        Write-Host "This often indicates:" -ForegroundColor Yellow
        Write-Host "  - Docker is not properly installed or not in PATH" -ForegroundColor Yellow
        Write-Host "  - A command within the container doesn't exist" -ForegroundColor Yellow
        Write-Host "  - Docker-in-Docker (DinD) issues with nested containers" -ForegroundColor Yellow
    } elseif ($exitCode -eq 1) {
        Write-Host "Exit code 1 indicates a general failure" -ForegroundColor Yellow
        Write-Host "Check the output above for specific error messages" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Debugging commands:" -ForegroundColor Cyan
    Write-Host "  # List all Docker containers (including stopped):" -ForegroundColor White
    Write-Host "  docker ps -a" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  # View logs from the most recent container:" -ForegroundColor White
    Write-Host "  docker logs `$(docker ps -aq | Select-Object -First 1)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  # Re-run with verbose output:" -ForegroundColor White
    Write-Host "  .\commands\run-act-test.ps1 -JobName $JobName -Verbose -StepDebug" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  # Re-run with log file:" -ForegroundColor White
    Write-Host "  .\commands\run-act-test.ps1 -JobName $JobName -LogFile ./act-debug.log" -ForegroundColor Gray
    Write-Host ""

    if ($LogFile) {
        Write-Host "Log file saved to: $LogFile" -ForegroundColor Cyan
    }
} else {
    Write-Host ""
    Write-Host "Workflow completed successfully!" -ForegroundColor Green

    if (Test-Path $artifactPath) {
        Write-Host "Artifacts saved to: $artifactPath" -ForegroundColor Cyan
    }
}

Write-Host ""
Pop-Location
exit $exitCode
