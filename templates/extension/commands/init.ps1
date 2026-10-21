#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Initializes the Unity Package project by replacing placeholders.

.DESCRIPTION
    Replaces placeholders in file content, filenames, and directory names.
    Placeholders:
    - YOUR_PACKAGE_ID
    - YOUR_PACKAGE_ID_LOWERCASE
    - YOUR_PACKAGE_NAME
    - YOUR_PACKAGE_NAME_INSTALLER
    - YOUR_PACKAGE_NAME_INSTALLER_FILE
    - YOUR_GITHUB_USERNAME_REPOSITORY
    - YOUR_FEATURE_NAME            (C# identifier: RQL module namespace and class names)
    - YOUR_FEATURE_NAME_LOWERCASE  (the RQL source name agents type, e.g. "coolpackage | call ...")

.PARAMETER PackageId
    The package ID (e.g., "com.company.package").

.PARAMETER PackageName
    The package name (e.g., "My Package").

.PARAMETER InstallerExtraPath
    Optional. An extra path segment between Assets/ and the Installer folder.
    For example, "Editor" places the installer at Assets/Editor/{Package} Installer.
    If not provided, you will be prompted (press Enter to skip).

.PARAMETER GitHubRepository
    The GitHub username and repository name in the format "Username/Repository-Name".
    For example, "MyGitHubUsername/My-Repository-Name".

.PARAMETER FeatureName
    Optional. A C# identifier for the extension's RQL module (namespace com.IvanMurzak.Unity.MCP.Editor.Rql.<FeatureName>,
    class <FeatureName>Module). Defaults to PackageName without spaces and symbols ("Cool Package" -> "CoolPackage").
    Must not equal a Unity type name the module uses (e.g. "Terrain", "Light").

.EXAMPLE
    .\init.ps1 -PackageId "com.mycompany.coolpackage" -PackageName "Cool Package"

.EXAMPLE
    .\init.ps1 -PackageId "com.mycompany.coolpackage" -PackageName "Cool Package" -InstallerExtraPath "Editor"

.EXAMPLE
    .\init.ps1 -PackageId "com.mycompany.coolpackage" -PackageName "Cool Package" -GitHubRepository "myusername/cool-package"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$PackageId,

    [Parameter(Mandatory = $true)]
    [string]$PackageName,

    [Parameter(Mandatory = $false)]
    [string]$InstallerExtraPath,

    [Parameter(Mandatory = $true)]
    [string]$GitHubRepository,

    [Parameter(Mandatory = $false)]
    [string]$FeatureName
)

$ErrorActionPreference = "Stop"

# Prompt for InstallerExtraPath if not provided
if (-not $PSBoundParameters.ContainsKey('InstallerExtraPath')) {
    Write-Host "Optional: Enter an extra path for the Installer (e.g., 'Editor')." -ForegroundColor Cyan
    Write-Host "Press Enter to skip." -ForegroundColor Gray
    $InstallerExtraPath = Read-Host "Installer Extra Path"
}

# Normalize InstallerExtraPath
if (-not [string]::IsNullOrWhiteSpace($InstallerExtraPath)) {
    $InstallerExtraPath = ($InstallerExtraPath.Trim() -replace '\\', '/').TrimStart('/').TrimEnd('/')
}

# Derived variables
$PackageIdLowercase = $PackageId.ToLower()

# RQL feature name: a C# identifier (namespace segment / class name prefix)
if ([string]::IsNullOrWhiteSpace($FeatureName)) {
    $FeatureName = (($PackageName -split '[^A-Za-z0-9]+' | Where-Object { $_ } | ForEach-Object {
        $_.Substring(0, 1).ToUpper() + $_.Substring(1)
    }) -join '')
}
if ($FeatureName -notmatch '^[A-Za-z][A-Za-z0-9]*$') {
    throw "FeatureName '$FeatureName' is not a valid identifier (letters and digits, starting with a letter). Pass -FeatureName."
}
$FeatureNameLowercase = $FeatureName.ToLower()

# Compute base installer name and file name first
$PackageNameInstallerBase = "$PackageName Installer"
$PackageNameInstallerFile = $PackageNameInstallerBase -replace ' ', '-'  # Stays unchanged (no path prefix)

# Prepend extra path to installer name if provided
if ([string]::IsNullOrWhiteSpace($InstallerExtraPath)) {
    $PackageNameInstaller = $PackageNameInstallerBase
}
else {
    $PackageNameInstaller = "$InstallerExtraPath/$PackageNameInstallerBase"
}

# Replacements map for file content (includes extra path in installer name)
$Replacements = @{
    "YOUR_PACKAGE_ID"                  = $PackageId
    "YOUR_PACKAGE_ID_LOWERCASE"        = $PackageIdLowercase
    "YOUR_PACKAGE_NAME"                = $PackageName
    "YOUR_PACKAGE_NAME_INSTALLER_FILE" = $PackageNameInstallerFile
    "YOUR_PACKAGE_NAME_INSTALLER"      = $PackageNameInstaller
    "YOUR_GITHUB_USERNAME_REPOSITORY"  = $GitHubRepository
    "YOUR_FEATURE_NAME"                = $FeatureName
    "YOUR_FEATURE_NAME_LOWERCASE"      = $FeatureNameLowercase
}

# Replacements map for file/folder renaming (uses base names without extra path)
$ReplacementsForRenaming = @{
    "YOUR_PACKAGE_ID"                  = $PackageId
    "YOUR_PACKAGE_ID_LOWERCASE"        = $PackageIdLowercase
    "YOUR_PACKAGE_NAME"                = $PackageName
    "YOUR_PACKAGE_NAME_INSTALLER_FILE" = $PackageNameInstallerFile
    "YOUR_PACKAGE_NAME_INSTALLER"      = $PackageNameInstallerBase
    "YOUR_GITHUB_USERNAME_REPOSITORY"  = $GitHubRepository
    "YOUR_FEATURE_NAME"                = $FeatureName
    "YOUR_FEATURE_NAME_LOWERCASE"      = $FeatureNameLowercase
}

# Sort keys by length descending to avoid partial replacements
$SortedKeys = $Replacements.Keys | Sort-Object { $_.Length } -Descending

# Root directory (parent of commands)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

Write-Host "Initializing package with:" -ForegroundColor Cyan
Write-Host "  ID: $PackageId"
Write-Host "  Name: $PackageName"
Write-Host "  Installer: $PackageNameInstaller"
Write-Host "  Installer File: $PackageNameInstallerFile"
Write-Host "  GitHub Repository: $GitHubRepository"
Write-Host "  RQL Feature Name: $FeatureName (source: $FeatureNameLowercase)"
if (-not [string]::IsNullOrWhiteSpace($InstallerExtraPath)) {
    Write-Host "  Installer Extra Path: $InstallerExtraPath"
}
Write-Host ""

# Define target paths with optional ignore patterns
# Each entry can be:
#   - A simple string: "path/to/target"
#   - A hashtable with Path and optional Ignore array: @{ Path = "folder"; Ignore = @("*/pattern", "specific/path") }
# Ignore patterns are relative to the target path and support wildcards (*) for single directory level
$TargetPaths = @(
    @{ Path = "commands/bump-version.ps1" },
    @{ Path = "commands/get-version.ps1" },
    @{ Path = "commands/update-ai-game-developer.ps1" },
    @{
        Path   = "Installer"
        Ignore = @(
            "/Library",
            "/Temp",
            "/Logs",
            "/obj"
        )
    },
    @{
        Path   = "Unity-Package"
        Ignore = @(
            "/Library",
            "/Temp",
            "/Logs",
            "/obj"
        )
    },
    @{
        Path   = "Unity-Tests"
        Ignore = @(
            "*/Library",
            "*/Temp",
            "*/Logs",
            "*/obj"
        )
    },
    @{ Path = "README.md" },
    @{ Path = ".github" }
)

# Helper function to check if a path should be ignored
function Test-ShouldIgnore {
    param(
        [string]$ItemPath,
        [string]$BasePath,
        [string[]]$IgnorePatterns
    )

    if (-not $IgnorePatterns -or $IgnorePatterns.Count -eq 0) {
        return $false
    }

    # Get relative path from base
    $RelativePath = $ItemPath.Substring($BasePath.Length).TrimStart('\', '/')
    $RelativePath = $RelativePath -replace '\\', '/'

    foreach ($Pattern in $IgnorePatterns) {
        # Convert glob pattern to regex
        # * matches any single directory/file name (not path separator)
        # ** would match multiple levels (not implemented here for simplicity)
        $RegexPattern = "^" + ($Pattern -replace '\*', '[^/]+') + "(/.*)?$"

        if ($RelativePath -match $RegexPattern) {
            return $true
        }
    }

    return $false
}

# Helper function to normalize target path entry
function Get-TargetPathInfo {
    param($Entry)

    if ($Entry -is [string]) {
        return @{ Path = $Entry; Ignore = @() }
    }
    elseif ($Entry -is [hashtable]) {
        return @{
            Path   = $Entry.Path
            Ignore = if ($Entry.Ignore) { $Entry.Ignore } else { @() }
        }
    }
    else {
        throw "Invalid target path entry: $Entry"
    }
}

# 1. Replace content in files
Write-Host "Replacing content in files..." -ForegroundColor Yellow
$Files = @()
foreach ($Entry in $TargetPaths) {
    $TargetInfo = Get-TargetPathInfo $Entry
    $FullPath = Join-Path $RepoRoot $TargetInfo.Path
    if (Test-Path $FullPath) {
        if ((Get-Item $FullPath).PSIsContainer) {
            $AllFiles = Get-ChildItem -Path $FullPath -Recurse -File
            foreach ($File in $AllFiles) {
                if (-not (Test-ShouldIgnore -ItemPath $File.FullName -BasePath $FullPath -IgnorePatterns $TargetInfo.Ignore)) {
                    $Files += $File
                }
            }
        }
        else {
            $Files += Get-Item $FullPath
        }
    }
    else {
        Write-Warning "Path not found: $FullPath"
    }
}

foreach ($File in $Files) {
    $Content = Get-Content -Path $File.FullName -Raw
    $NewContent = $Content
    $Modified = $false

    foreach ($Key in $SortedKeys) {
        if ($NewContent -match $Key) {
            $NewContent = $NewContent -replace $Key, $Replacements[$Key]
            $Modified = $true
        }
    }

    if ($Modified) {
        Set-Content -Path $File.FullName -Value $NewContent -NoNewline
        Write-Host "  Updated: $($File.FullName)" -ForegroundColor Gray
    }
}

# 1.5. Create extra path folders and move installer (if extra path provided)
if (-not [string]::IsNullOrWhiteSpace($InstallerExtraPath)) {
    Write-Host "Creating extra path and moving installer folder..." -ForegroundColor Yellow
    $InstallerAssetsDir = Join-Path $RepoRoot "Installer/Assets"
    $SourceFolder = Join-Path $InstallerAssetsDir "YOUR_PACKAGE_NAME_INSTALLER"
    $ExtraPathDir = Join-Path $InstallerAssetsDir $InstallerExtraPath

    # Create extra path directories
    if (-not (Test-Path $ExtraPathDir)) {
        New-Item -Path $ExtraPathDir -ItemType Directory -Force | Out-Null
        Write-Host "  Created: $ExtraPathDir" -ForegroundColor Gray
    }

    # Move installer folder
    if (Test-Path $SourceFolder) {
        $DestFolder = Join-Path $ExtraPathDir "YOUR_PACKAGE_NAME_INSTALLER"
        Move-Item -Path $SourceFolder -Destination $DestFolder -Force
        Write-Host "  Moved installer to: $InstallerExtraPath/" -ForegroundColor Gray
    }
}

# 2. Rename files and directories
# We need to do this depth-first (bottom-up) so we don't rename a parent directory before its children
Write-Host "Renaming files and directories..." -ForegroundColor Yellow
$Items = @()
foreach ($Entry in $TargetPaths) {
    $TargetInfo = Get-TargetPathInfo $Entry
    $FullPath = Join-Path $RepoRoot $TargetInfo.Path
    if (Test-Path $FullPath) {
        if ((Get-Item $FullPath).PSIsContainer) {
            $AllItems = Get-ChildItem -Path $FullPath -Recurse
            foreach ($Item in $AllItems) {
                if (-not (Test-ShouldIgnore -ItemPath $Item.FullName -BasePath $FullPath -IgnorePatterns $TargetInfo.Ignore)) {
                    $Items += $Item
                }
            }
        }
        else {
            $Items += Get-Item $FullPath
        }
    }
}

# Sort depth-first (longest path first) to handle nested renames correctly
$Items = $Items | Sort-Object -Property FullName -Descending | Select-Object -Unique

foreach ($Item in $Items) {
    $NewName = $Item.Name
    foreach ($Key in $SortedKeys) {
        if ($NewName -match $Key) {
            $NewName = $NewName -replace $Key, $ReplacementsForRenaming[$Key]
        }
    }

    if ($NewName -ne $Item.Name) {
        Rename-Item -Path $Item.FullName -NewName $NewName
        Write-Host "  Renamed: $($Item.Name) -> $NewName" -ForegroundColor Gray
    }
}

Write-Host "Done!" -ForegroundColor Green
