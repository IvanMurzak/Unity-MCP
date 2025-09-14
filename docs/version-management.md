# Version Management Guide

This guide explains how to use the automated version bumping system for the Unity-MCP project.

## Overview

The `bump-version.ps1` script automates version updates across all project files, eliminating manual errors and ensuring consistency. It handles **7 version references across 6 files** in the project.

## Quick Start

```powershell
# Preview what changes will be made
.\bump-version.ps1 -NewVersion "0.18.0" -WhatIf

# Apply the version bump
.\bump-version.ps1 -NewVersion "0.18.0"

# Rollback if needed
.\bump-version.ps1 -Rollback
```

## Prerequisites

- **PowerShell Core** (works on Windows, macOS, Linux)
- **Git repository** (for automatic commits and rollback)
- **Clean working directory** (no uncommitted changes recommended)

## Script Features

### ✅ Automated Updates
The script automatically updates version numbers in:

| File | Location | Description |
|------|----------|-------------|
| `README.md` | Line 104 | Download installer URL |
| `Unity-MCP-Server/server.json` | Lines 11, 17 | Server version (2 occurrences) |
| `AssetStore-Installer/.../Installer.cs` | Line 19 | C# version constant |
| `Unity-MCP-Plugin/.../package.json` | Line 14 | Unity package version |
| `Unity-MCP-Plugin/.../README.md` | Line 104 | Plugin download URL |
| `Unity-MCP-Plugin/.../McpPluginUnity.Startup.cs` | Line 31 | Plugin C# version constant |

### 🔍 Preview Mode
Use `-WhatIf` to see exactly what changes will be made without applying them:

```powershell
.\bump-version.ps1 -NewVersion "0.18.0" -WhatIf
```

**Example Output:**
```
🔍 Scanning for version references...
📝 Root README download URL: 1 occurrence(s)
   https://github.com/IvanMurzak/Unity-MCP/releases/download/0.17.1/... → 0.18.0/...
📝 Server JSON version (2 occurrences): 2 occurrence(s)
   "version": "0.17.1" → "version": "0.18.0"

📋 Preview Summary:
Files to be modified: 6
Total replacements: 7
```

### 🛡️ Version Validation
The script validates semantic version format (major.minor.patch):

```powershell
# ✅ Valid formats
.\bump-version.ps1 -NewVersion "1.0.0"
.\bump-version.ps1 -NewVersion "0.18.0"
.\bump-version.ps1 -NewVersion "2.1.3"

# ❌ Invalid formats
.\bump-version.ps1 -NewVersion "1.0"        # Missing patch
.\bump-version.ps1 -NewVersion "v1.0.0"     # Prefix not allowed
.\bump-version.ps1 -NewVersion "1.0.0.1"    # Too many components
```

### 🔄 Git Integration
The script automatically:
- Creates a backup tag before changes
- Stages modified files
- Creates a commit with format: `chore: Bump version from X.X.X to Y.Y.Y`
- Removes backup tag on success

### ↩️ Rollback Functionality
If something goes wrong, you can rollback:

```powershell
.\bump-version.ps1 -Rollback
```

This will:
- Reset to the state before the last version bump
- Remove the backup tag
- Restore all files to their previous state

## Detailed Usage

### Command Line Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-NewVersion` | String | New semantic version (e.g., "0.18.0") |
| `-WhatIf` | Switch | Preview changes without applying |
| `-Rollback` | Switch | Undo the last version bump |

### Parameter Sets

The script uses PowerShell parameter sets to prevent invalid combinations:

**Bump Parameter Set:**
```powershell
.\bump-version.ps1 -NewVersion "0.18.0" [-WhatIf]
```

**Rollback Parameter Set:**
```powershell
.\bump-version.ps1 -Rollback
```

## Examples

### Basic Version Bump
```powershell
# Update from 0.17.1 to 0.18.0
.\bump-version.ps1 -NewVersion "0.18.0"
```

**Output:**
```
🚀 Unity-MCP Version Bump Script
=================================
📋 Current version: 0.17.1
📋 New version: 0.18.0

🔍 Scanning for version references...
📝 Root README download URL: 1 occurrence(s)
📝 Server JSON version (2 occurrences): 2 occurrence(s)
📝 Installer C# version constant: 1 occurrence(s)
📝 Unity package version: 1 occurrence(s)
📝 Plugin README download URL: 1 occurrence(s)
📝 Plugin C# version constant: 1 occurrence(s)

📝 Creating git commit...
✅ Created git commit: chore: Bump version from 0.17.1 to 0.18.0

🎉 Version bump completed successfully!
   Updated 6 files
   Total replacements: 7
   Version: 0.17.1 → 0.18.0

💡 Use '-Rollback' if you need to undo these changes
```

### Preview Changes
```powershell
# See what would change without applying
.\bump-version.ps1 -NewVersion "1.0.0" -WhatIf
```

### Major Version Bump
```powershell
# Update to next major version
.\bump-version.ps1 -NewVersion "1.0.0"
```

### Same Version Detection
```powershell
# If new version equals current version
.\bump-version.ps1 -NewVersion "0.17.1"
```

**Output:**
```
⚠️ New version is the same as current version
```

## Safety Features

### Git Repository Checks
The script performs several safety checks:

1. **Repository Detection**: Ensures you're in a git repository
2. **Uncommitted Changes**: Warns about uncommitted changes and asks for confirmation
3. **Backup Creation**: Creates a backup tag before making changes

### Error Handling
- **Invalid Version Format**: Validates semantic versioning
- **Missing Files**: Warns if expected files aren't found
- **No Matches**: Alerts if version patterns aren't found
- **Git Failures**: Provides rollback instructions on git errors

### Rollback Protection
- Creates backup tag before any changes
- Allows complete rollback to previous state
- Cleans up backup tags on successful completion

## Troubleshooting

### Common Issues

#### "Not in a git repository"
**Problem:** Script requires git repository for safety features.
**Solution:** Ensure you're running the script from within the Unity-MCP git repository.

#### "You have uncommitted changes"
**Problem:** Git working directory has uncommitted changes.
**Solutions:**
- Commit or stash changes before running
- Answer 'y' to continue anyway (not recommended)

#### "No version references found to update"
**Problem:** Script couldn't find version patterns in expected files.
**Solutions:**
- Check if files have been moved or renamed
- Verify current version format matches expected patterns
- Run with `-WhatIf` to see what the script is looking for

#### "Invalid semantic version format"
**Problem:** Provided version doesn't match semver format.
**Solution:** Use format: `major.minor.patch` (e.g., "1.0.0", "0.18.0")

#### PowerShell Execution Policy
**Problem:** PowerShell prevents script execution.
**Solution:**
```powershell
# Windows - run as Administrator
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or run with bypass for single execution
pwsh -ExecutionPolicy Bypass -File "bump-version.ps1" -NewVersion "0.18.0"
```

### Emergency Recovery

#### If Version Bump Fails
1. **Use Rollback:**
   ```powershell
   .\bump-version.ps1 -Rollback
   ```

2. **Manual Git Reset (if rollback fails):**
   ```powershell
   git reset --hard HEAD~1  # Reset to previous commit
   ```

3. **Restore from Backup Tag:**
   ```powershell
   git reset --hard version-bump-backup
   git tag -d version-bump-backup
   ```

#### If Script Gets Interrupted
If the script is interrupted mid-execution:

1. Check git status:
   ```powershell
   git status
   ```

2. If backup tag exists, rollback:
   ```powershell
   .\bump-version.ps1 -Rollback
   ```

3. If no backup tag, manually revert changes:
   ```powershell
   git checkout -- .  # Revert all changes
   ```

## Integration with Development Workflow

### Recommended Workflow
1. **Complete your changes** and commit them
2. **Preview the version bump**: `.\bump-version.ps1 -NewVersion "X.Y.Z" -WhatIf`
3. **Apply the version bump**: `.\bump-version.ps1 -NewVersion "X.Y.Z"`
4. **Create release** using the new version tag
5. **Push changes**: `git push && git push --tags`

### Continuous Integration
For CI/CD pipelines, you can use the script programmatically:

```powershell
# In CI script
$newVersion = "0.18.0"
$result = & .\bump-version.ps1 -NewVersion $newVersion
if ($LASTEXITCODE -ne 0) {
    Write-Error "Version bump failed"
    exit 1
}
```

## Best Practices

1. **Always preview first** with `-WhatIf` before applying changes
2. **Clean working directory** - commit or stash changes before version bump
3. **Follow semantic versioning** - increment major/minor/patch appropriately
4. **Keep backups** - the script creates them automatically, but manual backups never hurt
5. **Test after bump** - verify the project still builds and works correctly
6. **Document changes** - update CHANGELOG.md manually after version bump

## Script Maintenance

### Adding New Version Locations
If new files need version updates, modify the `$VersionFiles` array in `bump-version.ps1`:

```powershell
$VersionFiles = @(
    @{
        Path = "path/to/new/file.json"
        Pattern = '"version":\s*"[\d\.]+"'
        Replace = '"version": "{VERSION}"'
        Description = "Description of this version location"
    }
    # ... existing entries
)
```

### Updating Version Patterns
If version format changes in existing files, update the corresponding `Pattern` and `Replace` values in the `$VersionFiles` array.

## Related Documentation

- [CHANGELOG.md](../Unity-MCP-Plugin/Assets/root/CHANGELOG.md) - Version history
- [Unity-MCP Plugin Documentation](../Unity-MCP-Plugin/Assets/root/README.md)
- [Unity-MCP Server Documentation](../Unity-MCP-Server/README.md)