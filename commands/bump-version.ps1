#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automated version bumping script for Unity-MCP project

.DESCRIPTION
    Updates version numbers across all project files automatically to prevent human errors.
    Supports preview mode for safe testing.

.PARAMETER NewVersion
    The new version number in semver format (e.g., "0.18.0")

.PARAMETER WhatIf
    Preview changes without applying them

.EXAMPLE
    .\bump-version.ps1 -NewVersion "0.18.0"

.EXAMPLE
    .\bump-version.ps1 -NewVersion "0.18.0" -WhatIf
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$NewVersion,

    [switch]$WhatIf
)

# Set location to repository root (parent of commands folder)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
Push-Location $repoRoot

# Script configuration
$ErrorActionPreference = "Stop"

# Version file locations (relative to script root)
$VersionFiles = @(
    @{
        Path        = "Unity-MCP-Server/server.json"
        Pattern     = '"version":\s*"[\d\.]+"'
        Replace     = '"version": "{VERSION}"'
        Description = "Server JSON version (2 occurrences)"
    },
    @{
        Path        = "Unity-MCP-Server/com.IvanMurzak.Unity.MCP.Server.csproj"
        Pattern     = '<Version>[\d\.]+</Version>'
        Replace     = '<Version>{VERSION}</Version>'
        Description = "Server csproj XML version"
    },
    @{
        Path        = "Installer/Assets/com.IvanMurzak/AI Game Dev Installer/Installer.cs"
        Pattern     = 'public const string Version = "[\d\.]+";'
        Replace     = 'public const string Version = "{VERSION}";'
        Description = "Installer C# version constant"
    },
    @{
        Path        = "Unity-MCP-Plugin/Packages/AI-Game-Developer/package.json"
        Pattern     = '"version":\s*"[\d\.]+"'
        Replace     = '"version": "{VERSION}"'
        Description = "Unity package version"
    },
    @{
        Path        = "Unity-MCP-Plugin/Packages/AI-Game-Developer/Runtime/UnityMcpPlugin.cs"
        Pattern     = 'public const string Version = "[\d\.]+";'
        Replace     = 'public const string Version = "{VERSION}";'
        Description = "Plugin C# version constant"
    },
    @{
        Path        = "cli/package.json"
        Pattern     = '"version":\s*"[\d\.]+(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?"'
        Replace     = '"version": "{VERSION}"'
        Description = "CLI npm package version"
    },
    @{
        Path        = "cli/src/utils/manifest.ts"
        Pattern     = "const FALLBACK_VERSION = '[\d\.]+(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?'"
        Replace     = "const FALLBACK_VERSION = '{VERSION}'"
        Description = "CLI OpenUPM fallback version"
    }
)

function Write-ColorText {
    param([string]$Text, [string]$Color = "White")
    Write-Host $Text -ForegroundColor $Color
}

function Test-SemanticVersion {
    param([string]$Version)

    if ([string]::IsNullOrWhiteSpace($Version)) {
        return $false
    }

    # Basic semver pattern: major.minor.patch (with optional prerelease/build)
    $pattern = '^\d+\.\d+\.\d+(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?$'
    return $Version -match $pattern
}

function Get-CurrentVersion {
    # Extract current version from package.json
    $packageJsonPath = "Unity-MCP-Plugin/Packages/AI-Game-Developer/package.json"
    if (-not (Test-Path $packageJsonPath)) {
        throw "Could not find package.json at: $packageJsonPath"
    }

    $content = Get-Content $packageJsonPath -Raw
    if ($content -match '"version":\s*"([\d\.]+)"') {
        return $Matches[1]
    }

    throw "Could not extract current version from package.json"
}

function Update-VersionFiles {
    param([string]$OldVersion, [string]$NewVersion, [bool]$PreviewOnly = $false)

    $changes = @()

    foreach ($file in $VersionFiles) {
        $fullPath = $file.Path

        if (-not (Test-Path $fullPath)) {
            Write-ColorText "⚠️  File not found: $($file.Path)" "Yellow"
            continue
        }

        $content = Get-Content $fullPath -Raw
        $originalContent = $content

        # Create the replacement string
        $replacement = $file.Replace -replace '\{VERSION\}', $NewVersion

        # Apply the replacement
        $newContent = $content -replace $file.Pattern, $replacement

        # Check if any changes were made
        if ($originalContent -ne $newContent) {
            # Count matches for reporting
            $regexMatches = [regex]::Matches($originalContent, $file.Pattern)

            $changes += @{
                Path            = $file.Path
                Description     = $file.Description
                Matches         = $regexMatches.Count
                Content         = $newContent
                OriginalContent = $originalContent
            }

            Write-ColorText "📝 $($file.Description): $($regexMatches.Count) occurrence(s)" "Green"

            # Show the actual changes
            foreach ($match in $regexMatches) {
                $newValue = $match.Value -replace $file.Pattern, $replacement
                Write-ColorText "   $($match.Value) → $newValue" "Gray"
            }
        }
        else {
            Write-ColorText "⚠️  No matches found in: $($file.Path)" "Yellow"
            Write-ColorText "   Pattern: $($file.Pattern)" "Gray"
        }
    }

    if ($changes.Count -eq 0) {
        Write-ColorText "❌ No version references found to update!" "Red"
        Pop-Location
        exit 1
    }

    if ($PreviewOnly) {
        Write-ColorText "`n📋 Preview Summary:" "Cyan"
        Write-ColorText "Files to be modified: $($changes.Count)" "White"
        Write-ColorText "Total replacements: $(($changes | Measure-Object -Property Matches -Sum).Sum)" "White"
        return $null
    }

    # Apply changes
    foreach ($change in $changes) {
        $fullPath = $change.Path
        Set-Content -Path $fullPath -Value $change.Content -NoNewline
    }

    return $changes
}

# Main execution
try {
    Write-ColorText "🚀 Unity-MCP Version Bump Script" "Cyan"
    Write-ColorText "=================================" "Cyan"

    # Validate semantic version format
    if (-not (Test-SemanticVersion $NewVersion)) {
        Write-ColorText "❌ Invalid semantic version format: $NewVersion" "Red"
        Write-ColorText "Expected format: major.minor.patch (e.g., '1.2.3')" "Yellow"
        Pop-Location
        exit 1
    }

    # Get current version
    $currentVersion = Get-CurrentVersion
    Write-ColorText "📋 Current version: $currentVersion" "White"
    Write-ColorText "📋 New version: $NewVersion" "White"

    if ($currentVersion -eq $NewVersion) {
        Write-ColorText "⚠️  New version is the same as current version" "Yellow"
        Pop-Location
        exit 0
    }

    Write-ColorText "`n🔍 Scanning for version references..." "Cyan"

    # Update version files
    $changes = Update-VersionFiles -OldVersion $currentVersion -NewVersion $NewVersion -PreviewOnly $WhatIf

    if ($WhatIf) {
        Write-ColorText "`n✅ Preview completed. Use without -WhatIf to apply changes." "Green"
        Pop-Location
        exit 0
    }

    if ($changes -and $changes.Count -gt 0) {
        # Update CLI package-lock.json — only the two root-level version fields
        # that follow "name": "unity-mcp-cli". Dependency versions are not touched.
        $lockFilePath = "cli/package-lock.json"
        if (Test-Path $lockFilePath) {
            Write-ColorText "`nUpdating CLI package-lock.json..." "Cyan"
            $lockContent = Get-Content $lockFilePath -Raw
            $pattern = '("name":\s*"unity-mcp-cli",\s+"version":\s*")[\d\.]+(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?'
            $replacement = "`${1}$NewVersion"
            $newLockContent = [regex]::Replace($lockContent, $pattern, $replacement)
            if ($newLockContent -ne $lockContent) {
                $matches = [regex]::Matches($lockContent, $pattern)
                Set-Content -Path $lockFilePath -Value $newLockContent -NoNewline
                Write-ColorText "   CLI package-lock.json updated ($($matches.Count) occurrences)" "Green"
            }
        }

        Write-ColorText "`n🎉 Version bump completed successfully!" "Green"
        Write-ColorText "   Updated $($changes.Count) files" "White"
        Write-ColorText "   Total replacements: $(($changes | Measure-Object -Property Matches -Sum).Sum)" "White"
        Write-ColorText "   Version: $currentVersion → $NewVersion" "White"
        Write-ColorText "`n💡 Remember to commit these changes to git" "Cyan"
    }

    Pop-Location
}
catch {
    Write-ColorText "`n❌ Script failed: $($_.Exception.Message)" "Red"
    Pop-Location
    exit 1
}