---
name: unity-version-split
description: "Split a C# file into Unity 6.5+ and pre-Unity 6.5 variants. Use when a file needs different implementations for different Unity versions due to API changes (e.g., EntityId vs int, GetEntityId vs GetInstanceID)."
argument-hint: "[path to the .cs file that needs splitting]"
---

# Unity Version Split

Split a C# source file into two variants: one for Unity 6.5+ (`UNITY_6000_5_OR_NEWER`) and one for older versions (pre-Unity 6.5).

## When to Use

Unity 6.5 introduced breaking changes including:
- `EntityId` replaces `int` for instance IDs (`GetEntityId()` replaces `GetInstanceID()`)
- `EditorUtility.EntityIdToObject(EntityId)` replaces `EditorUtility.InstanceIDToObject(int)`
- Implicit `EntityId <-> int` conversions are marked `[Obsolete(..., true)]` (compile error)

When a file needs different code for these Unity versions, split it into two files following the project convention.

## File Naming Convention

| File | Purpose | Preprocessor Guard |
|---|---|---|
| `FileName.cs` | Unity 6.5+ (newer version) | `#if UNITY_6000_5_OR_NEWER` |
| `FileName.pre-Unity.6.5.cs` | Pre-Unity 6.5 (older version) | `#if !UNITY_6000_5_OR_NEWER` |

For files that also have Editor/Runtime variants, combine the guards:
| File | Guard |
|---|---|
| `FileName.Editor.cs` | `#if UNITY_EDITOR && UNITY_6000_5_OR_NEWER` |
| `FileName.Editor.pre-Unity.6.5.cs` | `#if UNITY_EDITOR && !UNITY_6000_5_OR_NEWER` |
| `FileName.Runtime.cs` | `#if !UNITY_EDITOR && UNITY_6000_5_OR_NEWER` |
| `FileName.Runtime.pre-Unity.6.5.cs` | `#if !UNITY_EDITOR && !UNITY_6000_5_OR_NEWER` |

## Step-by-Step Process

### Step 1 — Read the Source File

Read `$ARGUMENTS` (the target file path). Identify which parts need version-specific code.

### Step 2 — Identify the Split Points

Common differences between Unity 6.5+ and pre-6.5:

| Unity 6.5+ | Pre-Unity 6.5 |
|---|---|
| `EntityId` | `int` |
| `EntityId.None` | `0` |
| `go.GetEntityId()` | `go.GetInstanceID()` |
| `EditorUtility.EntityIdToObject(entityId)` | `EditorUtility.InstanceIDToObject(instanceID)` |
| `ObjectRef.InstanceID` is `EntityId` | `ObjectRef.InstanceID` is `int?` |

### Step 3 — Create the Two Files

1. **Update the original `.cs` file** to contain only the Unity 6.5+ version:
   - Set the preprocessor guard to include `UNITY_6000_5_OR_NEWER`
   - Use `EntityId`, `GetEntityId()`, etc.

2. **Create the `.pre-Unity.6.5.cs` file** with the pre-6.5 version:
   - Set the preprocessor guard to include `!UNITY_6000_5_OR_NEWER`
   - Use `int`, `GetInstanceID()`, etc.

Both files must:
- Start with `#nullable enable`
- Have the same copyright header as the original
- Use the same namespace and class name (partial classes)
- End with `#endif` matching the opening `#if`

### Step 4 — Refresh Assets

Run `assets-refresh` tool to let Unity generate `.meta` files and recompile.

**IMPORTANT: Do NOT manually create `.meta` files.** Unity auto-generates them after `assets-refresh` or any AssetDatabase.Refresh() call.

### Step 5 — Verify

Check for compilation errors using `console-get-logs` tool.

## Example

Given `GameObjectUtils.Runtime.cs` with `#if !UNITY_EDITOR` that uses both `EntityId` and `int` with `#if UNITY_6000_5_OR_NEWER` inside:

**Before (single file with nested #if):**
```csharp
#if !UNITY_EDITOR
// ...
#if UNITY_6000_5_OR_NEWER
        public static GameObject? FindByInstanceID(EntityId instanceID) { ... }
#else
        public static GameObject? FindByInstanceID(int instanceID) { ... }
#endif
#endif
```

**After (two clean files):**

`GameObjectUtils.Runtime.cs`:
```csharp
#if !UNITY_EDITOR && UNITY_6000_5_OR_NEWER
// ...
        public static GameObject? FindByInstanceID(EntityId instanceID) { ... }
#endif
```

`GameObjectUtils.Runtime.pre-Unity.6.5.cs`:
```csharp
#if !UNITY_EDITOR && !UNITY_6000_5_OR_NEWER
// ...
        public static GameObject? FindByInstanceID(int instanceID) { ... }
#endif
```

## Reference Files

Existing examples of this pattern in the codebase:
- `Runtime/Data/ObjectRef.cs` / `ObjectRef.pre-Unity.6.5.cs`
- `Runtime/Data/GameObjectRef.cs` / `GameObjectRef.pre-Unity.6.5.cs`
- `Runtime/Utils/GameObjectUtils.Editor.cs` / `GameObjectUtils.Editor.pre-Unity.6.5.cs`
- `Tests/Editor/Tool/Assets/AssetsPrefabCreateTests.cs` / `AssetsPrefabCreateTests.pre-Unity.6.5.cs`
