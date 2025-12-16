#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Run Unity tests locally without Docker
.DESCRIPTION
    This script executes Unity tests directly using your local Unity installation,
    replicating what the GitHub Actions workflow does but without Docker or act.
.PARAMETER TestMode
    Which test mode to run: editmode, playmode, standalone, or all (default: all)
.PARAMETER UnityPath
    Path to Unity.exe (required)
.PARAMETER ProjectPath
    Path to Unity project (default: ./Unity-MCP-Plugin)
.PARAMETER OutputDir
    Directory for test results and logs (default: ./TestResults)
.EXAMPLE
    .\run-unity-tests.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\2022.3.69f1\Editor\Unity.exe"
    .\run-unity-tests.ps1 -UnityPath "C:\Unity\Editor\Unity.exe" -TestMode editmode
    .\run-unity-tests.ps1 -UnityPath "C:\Unity\Editor\Unity.exe" -TestMode all -Verbose
#>

[CmdletBinding()]
param(
    [ValidateSet('editmode', 'playmode', 'standalone', 'all')]
    [string]$TestMode = "all",
    [Parameter(Mandatory = $true)]
    [string]$UnityPath,
    [string]$ProjectPath = "./Unity-MCP-Plugin",
    [string]$OutputDir = "./commands/TestResults"
)

# ============================================================================
# Helper Functions
# ============================================================================

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Error-Message {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor White
}


function Invoke-UnityTest {
    param(
        [string]$UnityExe,
        [string]$ProjectPath,
        [string]$TestPlatform,
        [string]$ResultsFile,
        [string]$LogFile
    )

    $testModeName = $TestPlatform
    Write-Section "Running $testModeName Tests"

    # Build Unity command
    $unityArgs = @(
        "-runTests",
        "-batchmode",
        "-projectPath", "`"$ProjectPath`"",
        "-testResults", "`"$ResultsFile`"",
        "-testPlatform", $TestPlatform,
        "-logFile", "`"$LogFile`"",
        "-CI", "true",
        "-GITHUB_ACTIONS", "true"
    )

    Write-Info "Test Platform: $TestPlatform"
    Write-Info "Results: $ResultsFile"
    Write-Info "Log: $LogFile"
    Write-Host ""

    # Execute Unity
    $startTime = Get-Date

    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $UnityExe
    $processInfo.Arguments = $unityArgs -join " "
    $processInfo.UseShellExecute = $false
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo

    # Event handlers for output
    $outputBuilder = New-Object System.Text.StringBuilder
    $errorBuilder = New-Object System.Text.StringBuilder

    $outputHandler = {
        if ($EventArgs.Data) {
            $line = $EventArgs.Data
            [void]$Event.MessageData.AppendLine($line)
            if ($script:VerboseOutput -eq 'Continue') {
                Write-Host $line -ForegroundColor Gray
            }
        }
    }

    $errorHandler = {
        if ($EventArgs.Data) {
            $line = $EventArgs.Data
            [void]$Event.MessageData.AppendLine($line)
            Write-Host $line -ForegroundColor Red
        }
    }

    $script:VerboseOutput = $VerbosePreference

    Register-ObjectEvent -InputObject $process -EventName OutputDataReceived -Action $outputHandler -MessageData $outputBuilder | Out-Null
    Register-ObjectEvent -InputObject $process -EventName ErrorDataReceived -Action $errorHandler -MessageData $errorBuilder | Out-Null

    [void]$process.Start()
    $process.BeginOutputReadLine()
    $process.BeginErrorReadLine()

    Write-Host "Unity is running tests..." -ForegroundColor Cyan
    if ($VerbosePreference -ne 'Continue') {
        Write-Host "(Use -Verbose flag to see real-time output)" -ForegroundColor Gray
    }

    $process.WaitForExit()

    # Cleanup event handlers
    Get-EventSubscriber | Where-Object { $_.SourceObject -eq $process } | Unregister-Event

    $endTime = Get-Date
    $duration = $endTime - $startTime
    $exitCode = $process.ExitCode

    Write-Host ""
    Write-Info "Duration: $($duration.ToString('mm\:ss'))"
    Write-Info "Exit Code: $exitCode"

    return @{
        ExitCode     = $exitCode
        Duration     = $duration
        TestPlatform = $TestPlatform
        ResultsFile  = $ResultsFile
        LogFile      = $LogFile
    }
}

function Parse-TestResults {
    param([string]$XmlPath)

    if (-not (Test-Path $XmlPath)) {
        return @{
            Success = $false
            Total   = 0
            Passed  = 0
            Failed  = 0
            Skipped = 0
            Error   = "Results file not found"
        }
    }

    try {
        [xml]$xml = Get-Content $XmlPath

        # NUnit 3 format
        $testRun = $xml.'test-run'
        if ($testRun) {
            return @{
                Success      = ($testRun.failed -eq "0" -and $testRun.result -eq "Passed")
                Total        = [int]$testRun.total
                Passed       = [int]$testRun.passed
                Failed       = [int]$testRun.failed
                Skipped      = [int]$testRun.skipped
                Inconclusive = [int]$testRun.inconclusive
                Duration     = $testRun.duration
            }
        }

        # Fallback NUnit 2 format
        $testResults = $xml.'test-results'
        if ($testResults) {
            $total = [int]$testResults.total
            $failures = [int]$testResults.failures
            $errors = [int]$testResults.errors
            $notRun = [int]$testResults.'not-run'

            return @{
                Success = ($failures -eq 0 -and $errors -eq 0)
                Total   = $total
                Passed  = $total - $failures - $errors - $notRun
                Failed  = $failures + $errors
                Skipped = $notRun
            }
        }

        return @{
            Success = $false
            Error   = "Unknown XML format"
        }

    }
    catch {
        return @{
            Success = $false
            Error   = $_.Exception.Message
        }
    }
}

# ============================================================================
# Main Script
# ============================================================================

# Set location to repository root (parent of commands folder)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
Push-Location $repoRoot

Write-Section "Unity Local Test Runner"

Write-Info "Repository Root: $repoRoot"

# Convert to absolute paths
$ProjectPath = Resolve-Path $ProjectPath -ErrorAction SilentlyContinue
if (-not $ProjectPath) {
    Write-Error-Message "Project path not found: $ProjectPath"
    Pop-Location
    exit 1
}

Write-Info "Project: $ProjectPath"

# ============================================================================
# Step 1: Verify Unity Path
# ============================================================================

if (-not (Test-Path $UnityPath)) {
    Write-Error-Message "Unity.exe not found at: $UnityPath"
    Write-Host ""
    Write-Host "Please provide a valid path to Unity.exe:" -ForegroundColor Yellow
    Write-Host "  Example: -UnityPath `"C:\Program Files\Unity\Hub\Editor\2022.3.69f1\Editor\Unity.exe`"" -ForegroundColor Gray
    Pop-Location
    exit 1
}

Write-Success "Unity Executable: $UnityPath"

# ============================================================================
# Step 2: Prepare Output Directories
# ============================================================================

# Create directories first, then resolve to absolute path
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$OutputDir = (Resolve-Path $OutputDir).Path

$logsDir = Join-Path $OutputDir "Logs"
New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

Write-Success "Output Directory: $OutputDir"

# ============================================================================
# Step 3: Determine Test Modes
# ============================================================================

$testModes = @()

if ($TestMode -eq "all") {
    $testModes = @("EditMode", "PlayMode", "StandaloneWindows64")
}
else {
    $testModes = switch ($TestMode) {
        "editmode" { @("EditMode") }
        "playmode" { @("PlayMode") }
        "standalone" { @("StandaloneWindows64") }
    }
}

Write-Info "Test Modes: $($testModes -join ', ')"

# ============================================================================
# Step 4: Run Tests
# ============================================================================

$results = @()

foreach ($mode in $testModes) {
    $resultsFile = Join-Path $OutputDir "TestResults-$mode.xml"
    $logFile = Join-Path $logsDir "Unity-$mode.log"

    # Remove old results
    if (Test-Path $resultsFile) { Remove-Item $resultsFile -Force }
    if (Test-Path $logFile) { Remove-Item $logFile -Force }

    $result = Invoke-UnityTest `
        -UnityExe $UnityPath `
        -ProjectPath $ProjectPath `
        -TestPlatform $mode `
        -ResultsFile $resultsFile `
        -LogFile $logFile

    $results += $result
}

# ============================================================================
# Step 5: Parse and Display Results
# ============================================================================

Write-Section "Test Results Summary"

$overallSuccess = $true
$totalTests = 0
$totalPassed = 0
$totalFailed = 0
$totalSkipped = 0

foreach ($result in $results) {
    $mode = $result.TestPlatform
    $exitCode = $result.ExitCode

    Write-Host ""
    Write-Host "--- $mode ---" -ForegroundColor Cyan

    if ($exitCode -ne 0) {
        Write-Host "Status: FAILED (Exit Code: $exitCode)" -ForegroundColor Red
        $overallSuccess = $false

        Write-Host "Log file: $($result.LogFile)" -ForegroundColor Yellow
    }
    else {
        $parsed = Parse-TestResults -XmlPath $result.ResultsFile

        if ($parsed.Error) {
            Write-Host "Status: ERROR" -ForegroundColor Red
            Write-Host "Error: $($parsed.Error)" -ForegroundColor Red
            $overallSuccess = $false
        }
        elseif ($parsed.Success) {
            Write-Host "Status: PASSED" -ForegroundColor Green
            Write-Host "Tests: $($parsed.Passed)/$($parsed.Total) passed" -ForegroundColor Green

            if ($parsed.Skipped -gt 0) {
                Write-Host "Skipped: $($parsed.Skipped)" -ForegroundColor Yellow
            }

            $totalTests += $parsed.Total
            $totalPassed += $parsed.Passed
            $totalSkipped += $parsed.Skipped
        }
        else {
            Write-Host "Status: FAILED" -ForegroundColor Red
            Write-Host "Tests: $($parsed.Passed)/$($parsed.Total) passed, $($parsed.Failed) failed" -ForegroundColor Red

            $totalTests += $parsed.Total
            $totalPassed += $parsed.Passed
            $totalFailed += $parsed.Failed
            $totalSkipped += $parsed.Skipped

            $overallSuccess = $false
        }
    }

    Write-Host "Duration: $($result.Duration.ToString('mm\:ss'))" -ForegroundColor White
    Write-Host "Results: $($result.ResultsFile)" -ForegroundColor Gray
}

# ============================================================================
# Step 6: Final Summary
# ============================================================================

Write-Section "Overall Summary"

Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $totalPassed" -ForegroundColor Green
if ($totalFailed -gt 0) {
    Write-Host "Failed: $totalFailed" -ForegroundColor Red
}
if ($totalSkipped -gt 0) {
    Write-Host "Skipped: $totalSkipped" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Results Location: $OutputDir" -ForegroundColor Cyan

if ($overallSuccess) {
    Write-Host ""
    Write-Host "All tests PASSED!" -ForegroundColor Green
    Write-Host ""
    Pop-Location
    exit 0
}
else {
    Write-Host ""
    Write-Host "Some tests FAILED!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  - Check log files in: $logsDir" -ForegroundColor Gray
    Write-Host "  - Check test results in: $OutputDir" -ForegroundColor Gray
    Write-Host "  - Run with -Verbose flag for detailed output" -ForegroundColor Gray
    Write-Host ""
    Pop-Location
    exit 1
}
