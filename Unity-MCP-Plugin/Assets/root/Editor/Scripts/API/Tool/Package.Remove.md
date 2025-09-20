# Package Remove Tool

## Overview

The Package Remove tool allows AI assistants to remove Unity packages from the project by modifying the `manifest.json` file. It supports removing multiple packages at once and provides detailed feedback for each package operation.

## Method Signature

```csharp
public static async Task<ResponseCallTool> Remove(
    string[] packageIds,
    string? requestId = null
)
```

## Parameters

- **packageIds**: Array of package IDs to remove (e.g., `["com.unity.postprocessing", "org.nuget.system.text.json"]`)
- **requestId**: Request identifier with `[RequestID]` attribute for tracking async operations

## Behavior

### Processing Flow

1. **Validation**: Validates RequestID and package array
2. **Manifest Reading**: Reads and parses `/Packages/manifest.json`
3. **Package Processing**: For each package ID:
   - If found: Removes from dependencies and logs success
   - If not found: Logs warning message
4. **Asset Refresh**: If any packages were removed:
   - Saves modified manifest.json
   - Calls `AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport)`
   - Returns `Processing` status
5. **Completion Handling**: After domain reload (if triggered):
   - Checks compilation success/failure
   - Sends final results to MCP server

### Response Examples

**Immediate Success (no packages removed)**:
```
[Warning] Package com.nonexistent.package not found
```

**Processing Response (packages removed)**:
```
Processing... (Final results sent after AssetDatabase refresh completes)
```

**Final Completion Response**:
```
[Success] Package com.unity.postprocessing removed
[Success] Package org.nuget.system.text.json removed
[Warning] Package com.nonexistent.package not found
```

**Compilation Error Response**:
```
[Error] Package removal completed but compilation failed. Check the console for compilation errors.

Package removal results:
[Success] Package com.unity.postprocessing removed
```

### Error Handling

- **Invalid RequestID**: Returns immediate error
- **Missing manifest.json**: Returns file not found error
- **JSON Parse Error**: Returns parsing error with file path
- **Compilation Failure**: Returns error with package results included

## Domain Reload Handling

The tool properly handles Unity's domain reload scenarios:

- **No Domain Reload**: Results returned immediately after AssetDatabase refresh
- **Domain Reload Triggered**: Results stored and sent after compilation completes
- **Compilation Errors**: Detected and reported with original package results

## Testing

Unit tests are available in `TestToolPackageRemove.cs` covering:

- RequestID validation
- Empty package array handling
- Package removal tracker functionality
- JSON manipulation validation
- Error scenarios

## Integration

The tool integrates with the Unity MCP system through:

- **McpPluginTool** attribute for discovery
- **ResponseCallTool** for standardized responses
- **McpPluginUnity.NotifyToolRequestCompleted** for async completion
- **PackageRemovalTracker** for cross-domain-reload state management

## Usage by AI Assistants

AI assistants can use this tool to:

1. Remove unused or problematic packages
2. Clean up project dependencies
3. Prepare projects for different deployment targets
4. Resolve package conflicts by removing specific versions

The tool provides detailed feedback making it suitable for automated package management workflows.