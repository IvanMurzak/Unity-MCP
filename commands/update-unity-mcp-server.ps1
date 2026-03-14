#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Updates Unity-MCP-Server dependencies to the latest versions from NuGet

.DESCRIPTION
    Updates com.IvanMurzak.ReflectorNet and com.IvanMurzak.McpPlugin.Server packages
    in the Unity-MCP-Server project to the latest available versions on NuGet.

.PARAMETER WhatIf
    Preview which packages would be updated without applying changes

.EXAMPLE
    .\update-unity-mcp-server.ps1

.EXAMPLE
    .\update-unity-mcp-server.ps1 -WhatIf
#>

param(
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# Packages to update
$Packages = @(
    "com.IvanMurzak.ReflectorNet",
    "com.IvanMurzak.McpPlugin.Server"
)

# Project file to update (relative to script root)
$ProjectFile = "/../Unity-MCP-Server/com.IvanMurzak.Unity.MCP.Server.csproj"

function Write-ColorText {
    param([string]$Text, [string]$Color = "White")
    Write-Host $Text -ForegroundColor $Color
}

function Get-CurrentPackageVersion {
    param(
        [string]$ProjectPath,
        [string]$PackageName
    )

    if (-not (Test-Path $ProjectPath)) {
        return $null
    }

    $content = Get-Content $ProjectPath -Raw
    if ($content -match "PackageReference Include=`"$PackageName`" Version=`"([^`"]+)`"") {
        return $Matches[1]
    }
    return $null
}

# Main execution
try {
    Write-ColorText "🔄 Unity-MCP-Server Dependencies Update Script" "Cyan"
    Write-ColorText "===============================================" "Cyan"

    $fullPath = Join-Path $PSScriptRoot $ProjectFile
    $projectName = Split-Path $ProjectFile -Leaf

    if (-not (Test-Path $fullPath)) {
        Write-ColorText "❌ Project not found: $fullPath" "Red"
        exit 1
    }

    Write-ColorText "Project: $projectName`n" "White"

    # Get current versions
    Write-ColorText "📋 Current versions:" "Cyan"
    foreach ($package in $Packages) {
        $version = Get-CurrentPackageVersion -ProjectPath $fullPath -PackageName $package
        if ($version) {
            Write-ColorText "   $package : $version" "Gray"
        } else {
            Write-ColorText "   $package : not found" "Yellow"
        }
    }

    if ($WhatIf) {
        Write-ColorText "`n📋 Preview mode - would update the following packages:" "Cyan"
        foreach ($package in $Packages) {
            Write-ColorText "   $package" "Gray"
        }
        Write-ColorText "`n✅ Preview completed. Run without -WhatIf to apply changes." "Green"
        exit 0
    }

    # Clear local NuGet cache
    Write-ColorText "`n🧹 Clearing local NuGet cache..." "Cyan"
    $cacheResult = dotnet nuget locals all --clear 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-ColorText "   ✅ Cache cleared successfully" "Green"
    } else {
        Write-ColorText "   ⚠️  Cache clear warning: $cacheResult" "Yellow"
    }

    Write-ColorText "`n🚀 Updating packages to latest versions..." "Cyan"

    foreach ($package in $Packages) {
        Write-ColorText "📦 Updating $package..." "White"

        $result = dotnet add $fullPath package $package 2>&1

        if ($LASTEXITCODE -eq 0) {
            $newVersion = Get-CurrentPackageVersion -ProjectPath $fullPath -PackageName $package
            Write-ColorText "   ✅ Updated to $newVersion" "Green"
        } else {
            Write-ColorText "   ❌ Failed to update: $result" "Red"
        }
    }

    Write-ColorText "`n🎉 Unity-MCP-Server dependencies update completed!" "Green"
    Write-ColorText "💡 Remember to commit these changes to git" "Cyan"
}
catch {
    Write-ColorText "`n❌ Script failed: $($_.Exception.Message)" "Red"
    exit 1
}
