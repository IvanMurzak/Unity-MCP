/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Navigation)     │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Navigation
{
    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): NavMeshSurface bake / clear,
    /// NavMeshAgent destinations, agent-type and area names, the <c>navigation</c> source (agent-type bake settings, areas,
    /// BakeAll / ClearAll), and the agent guide rendered into the unity-rql skill.
    /// </summary>
    public sealed class NavigationModule : IRqlModule
    {
        private const string Guide =
@"AI Navigation components are components: `objects | get components | where $ is NavMeshSurface` (also NavMeshModifier,
NavMeshModifierVolume, NavMeshLink, NavMeshAgent). Add them with AddComponent, then set members (all undoable):

```
objects | where name == ""Ground"" | call AddComponent(""NavMeshSurface"")
objects | get components | where $ is NavMeshSurface | set collectObjects = ""Volume"", size = ""(20, 5, 20)"", center = ""(0, 1, 0)""
objects | get components | where $ is NavMeshSurface | set agentType = ""Humanoid"", defaultAreaName = ""Walkable"", layerMask = [""Default"", ""Water""]
objects | get components | where $ is NavMeshSurface | set overrideVoxelSize = true, voxelSize = 0.1
objects | get components | where $ is NavMeshSurface | call Bake()          build now (saved scene: NavMesh-<name>.asset next to it)
objects | get components | where $ is NavMeshSurface | call Clear()         remove the baked data
objects | get components | where $ is NavMeshSurface | select gameObject.name agentType navMeshData buildSettings.agentRadius
navigation | call BakeAll()                                                   every active surface (also ClearAll())
```

Modifiers, volumes, links:

```
objects | where name == ""Water"" | call AddComponent(""NavMeshModifier"")
objects | get components | where $ is NavMeshModifier | set overrideArea = true, areaName = ""Not Walkable""
objects | get components | where $ is NavMeshModifier | set ignoreFromBuild = true, applyToChildren = false, affectedAgents = [""All""]
objects | where name == ""Mud"" | call AddComponent(""NavMeshModifierVolume"")
objects | get components | where $ is NavMeshModifierVolume | set size = ""(4, 2, 4)"", center = ""(0, 1, 0)"", areaName = ""Jump""
objects | where name == ""Gap"" | call AddComponent(""NavMeshLink"")
objects | get components | where $ is NavMeshLink | set startPoint = ""(0, 0, -2)"", endPoint = ""(0, 1, 3)"", width = 1, bidirectional = false, costModifier = 2, areaName = ""Jump""
```

Agents (`SetDestination` / `Warp` / `ResetPath` act only on an agent on a baked NavMesh, i.e. in Play Mode; they return false otherwise):

```
objects | where name == ""Enemy"" | call AddComponent(""NavMeshAgent"")
objects | get components | where $ is NavMeshAgent | set speed = 5, acceleration = 12, angularSpeed = 240, stoppingDistance = 0.5
objects | get components | where $ is NavMeshAgent | set agentType = ""Humanoid"", areaNames = [""Walkable"", ""Jump""]   area mask; [""All""] = every area
objects | get components | where $ is NavMeshAgent | call SetDestination(""(10, 0, 5)"")
objects | get components | where $ is NavMeshAgent | select gameObject.name isOnNavMesh hasPath remainingDistance
```

Bake settings live on the agent type (shared by every surface baking for it; rebake afterwards); areas name the area indices:

```
navigation | get agentTypes | select agentTypeID name agentRadius agentHeight agentSlope agentClimb
navigation | get agentTypes | where name == ""Humanoid"" | set agentRadius = 0.4, agentHeight = 1.8, agentSlope = 40, agentClimb = 0.5
navigation | get areas | where name != """" | select index name cost
navigation | get areas | where index == 3 | set name = ""Water"", cost = 4
```

`agentType`, `areaName` / `defaultAreaName`, `areaNames` and `affectedAgents` are the name views of agentTypeID, area /
defaultArea, areaMask and the agent-type list; the numeric members stay writable. A LayerMask reads as its int mask and takes an
int (-1 = Everything), a layer name or a list of layer names.";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx) =>
            ctx.AddAdapter(new Unity_AI_Navigation_NavMeshSurface_Adapter())
               .AddAdapter(new Unity_AI_Navigation_NavMeshModifier_Adapter())
               .AddAdapter(new Unity_AI_Navigation_NavMeshModifierVolume_Adapter())
               .AddAdapter(new Unity_AI_Navigation_NavMeshLink_Adapter())
               .AddAdapter(new UnityEngine_AI_NavMeshAgent_Adapter())
               .AddSource("navigation", () => NavigationApi.Instance,
                   "AI Navigation project settings and operations: agentTypes (bake settings, writable), areas (name, cost), BakeAll(), ClearAll()")
               .AddGuide("Navigation", Guide);
    }
}
