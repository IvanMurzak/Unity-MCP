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
```

## Prerequisites

- **PowerShell Core** (works on Windows, macOS, Linux)

## Script Features

### ✅ Automated Updates
The script automatically updates version numbers in:

| File | Location | Description |
|------|----------|-------------|
| `README.md` | Download URL section | Download installer URL |
| `Unity-MCP-Server/server.json` | Version field (2 occurrences) | Server version (2 occurrences) |
| `AssetStore-Installer/.../Installer.cs` | Version constant | C# version constant |
| `Unity-MCP-Plugin/.../package.json` | Version field | Unity package version |
| `Unity-MCP-Plugin/.../README.md` | Download URL section | Plugin download URL |
| `Unity-MCP-Plugin/.../McpPluginUnity.Startup.cs` | Version constant | Plugin C# version constant |

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

### 📝 Manual Git Integration
After running the script successfully, you'll need to manually commit the changes:

```powershell
# Stage the modified files
git add .

# Create a commit
git commit -m "chore: Bump version from 0.17.1 to 0.18.0"
```

## Detailed Usage

### Command Line Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-NewVersion` | String | New semantic version (e.g., "0.18.0") - Required |
| `-WhatIf` | Switch | Preview changes without applying |

### Usage Syntax

```powershell
.\bump-version.ps1 -NewVersion "0.18.0" [-WhatIf]
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

🎉 Version bump completed successfully!
   Updated 6 files
   Total replacements: 7
   Version: 0.17.1 → 0.18.0

💡 Remember to commit these changes to git
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

### Error Handling
- **Invalid Version Format**: Validates semantic versioning
- **Missing Files**: Warns if expected files aren't found
- **No Matches**: Alerts if version patterns aren't found
- **Preview Mode**: Always test with `-WhatIf` before applying changes

## Troubleshooting

### Common Issues

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

#### If Version Bump Applies Wrong Changes
1. **Check git status to see what changed:**
   ```powershell
   git status
   git diff
   ```

2. **Revert changes manually:**
   ```powershell
   git checkout -- .  # Revert all changes
   ```

3. **Or revert specific files:**
   ```powershell
   git checkout -- "path/to/specific/file"
   ```

## Integration with Development Workflow

### Recommended Workflow
1. **Complete your changes** and commit them
2. **Preview the version bump**: `.\bump-version.ps1 -NewVersion "X.Y.Z" -WhatIf`
3. **Apply the version bump**: `.\bump-version.ps1 -NewVersion "X.Y.Z"`
4. **Commit the version changes**: `git add . && git commit -m "chore: Bump version to X.Y.Z"`
5. **Create release** using the new version tag
6. **Push changes**: `git push && git push --tags`

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
4. **Manual git backups** - create a branch or tag before major version changes
5. **Test after bump** - verify the project still builds and works correctly
6. **Document changes** - update CHANGELOG.md manually after version bump
7. **Commit immediately** - commit version changes right after running the script

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