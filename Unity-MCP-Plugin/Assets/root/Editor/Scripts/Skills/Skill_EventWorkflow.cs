/*
┌──────────────────────────────────────────────────────────────────┐
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginSkillType]
    public static class Skill_EventWorkflow
    {
        public const string SkillId = "unity-event-workflow";

        [McpPluginSkill(SkillId,
@"Best practices for waiting on Unity events, monitoring errors, and playtesting
with the MCP event system. Read this BEFORE using sleep, polling, or screenshots
to wait for state changes.")]
        public static string Markdown => @"
# Unity Event Workflow — Stop Polling, Start Subscribing

## The Problem
When you need to wait for something in Unity (play mode entered, scene loaded,
server callback done, compilation finished), do NOT use:
- sleep/delay loops
- Repeated screenshot captures to check state
- Polling console-get-logs or editor-application-get-state in a loop

These waste tokens and are unreliable.

## The Solution: Event Tools

### Three tools, three use cases:

| Tool | Blocking? | Use when |
|------|-----------|----------|
| `event-subscribe` | YES — blocks until event | You need to wait for a specific event before continuing |
| `event-watch` | NO — returns immediately, notifies later | You want background monitoring while doing other work |
| `event-list` | NO — instant | You want to see what events are available |

---

## Quick Start

### 1. Wait for play mode (instead of polling editor-application-get-state)
```
editor-application-set-state(playMode: true)
event-subscribe(type='play_mode_changed', timeoutMs=30000)
```

### 2. Monitor errors in background (instead of polling console-get-logs)
```
event-watch(type='error_logged', timeoutMs=120000)
// ... continue other work ... notification arrives if error occurs
```

### 3. Wait for compilation (instead of sleep + assets-refresh loop)
```
[parallel]
  event-subscribe(type='compilation_finished', timeoutMs=60000)
  script-update-or-create(...)  // triggers compilation
```

### 4. Wait for scene load
```
[parallel]
  event-subscribe(type='scene_loaded', timeoutMs=30000)
  scene-open(scenePath='...')
```

---

## Custom Events (Game Logic)

Built-in events cover Unity Editor events only. For game-specific events
(server response, popup close, data load), use **dynamic hooks**:

### Step 1: Hook game event to McpEventBus (via script-execute, no code changes needed)
```
script-execute:
  class Script {
    public static object Main() {
      SomeManager.Instance.OnDataLoaded += () =>
        McpEventBus.Push(""data_loaded"", source: ""SomeManager"");
      return ""hook registered"";
    }
  }
```

### Step 2: Subscribe + trigger in parallel
```
[parallel]
  event-subscribe(type='data_loaded', timeoutMs=15000)
  script-execute(trigger the game action)
```

### Step 3: No cleanup needed
Hooks die automatically when play mode stops.

---

## Playtesting Checklist

When asked to playtest or run the game:

1. **First**: `event-watch(type='error_logged', timeoutMs=120000)` — background error monitoring
2. **Enter play mode**: `editor-application-set-state` + `event-subscribe(type='play_mode_changed')`
3. **Wait for scene**: `event-subscribe(type='scene_loaded')` — NOT screenshot polling
4. **Game events**: Hook with `script-execute` then `event-subscribe` for precise timing
5. **Check results**: Errors arrive via event-watch notification automatically

---

## Built-in Event Types

| Event | Fires when |
|-------|-----------|
| `play_mode_changed` | Play/Pause/Stop transitions |
| `scene_loaded` | Runtime scene load complete |
| `scene_opened` | Editor scene open complete |
| `compilation_started` | Script compilation begins |
| `compilation_finished` | Script compilation ends (has `hasErrors` flag) |
| `error_logged` | Error or exception in console |
| `warning_logged` | Warning in console |
| `pause_state_changed` | Editor pause toggled |
| `hierarchy_changed` | Any GO created/destroyed/reparented |
| `selection_changed` | Editor selection changed |

Run `event-list` for the full list including any custom events.
";
    }
}
