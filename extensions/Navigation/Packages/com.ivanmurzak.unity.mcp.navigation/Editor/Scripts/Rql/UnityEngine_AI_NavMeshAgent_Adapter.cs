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
using System.Collections.Generic;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Navigation
{
    using NavMeshAgent = UnityEngine.AI.NavMeshAgent;

    /// <summary>
    /// NavMeshAgent: SetDestination / Warp / ResetPath, agentType and areaNames (area mask) as names. Path-following state
    /// (isStopped, remainingDistance …) only exists while the agent is on a NavMesh — Unity logs an error when it is read
    /// or written otherwise — so those members are guarded.
    /// </summary>
    public sealed class UnityEngine_AI_NavMeshAgent_Adapter : RqlTypeAdapter<NavMeshAgent>
    {
        private const string OffNavMesh = "the agent is not on a baked NavMesh (enter Play Mode with a baked surface under it)";

        public override string Description =>
            "NavMeshAgent: speed, acceleration, angularSpeed, stoppingDistance, agentType, areaNames; call SetDestination(\"(x, y, z)\") / Warp / ResetPath (Play Mode, on a baked NavMesh)";

        protected override void Members(MemberTable<NavMeshAgent> m)
        {
            NavigationMembers.AddAgentType(m, a => a.agentTypeID, (a, id) => a.agentTypeID = id);
            m.Add<List<string>>("areaNames", a => NavMeshNames.AreaMaskNames(a.areaMask), (a, v) => a.areaMask = NavMeshNames.AreaMask(v),
                    (a, v) => NavMeshNames.ValidateAreaMask(v), "traversable areas: [\"All\"] or names such as [\"Walkable\", \"Jump\"]; assigning sets areaMask")
             .Add<bool>("isStopped", a => OnNavMesh(a) && a.isStopped, (a, v) => a.isStopped = v, (a, v) => OnNavMesh(a) ? null : OffNavMesh,
                    "path following paused (only on a NavMesh)")
             .Add<float?>("remainingDistance", a => OnNavMesh(a) && !float.IsInfinity(a.remainingDistance) ? a.remainingDistance : (float?)null,
                    description: "distance left on the current path (null when unknown or off the NavMesh)")
             .Function<Vector3, bool>("SetDestination", (a, point) => OnNavMesh(a) && a.SetDestination(point), "point",
                    "path-find to a world point; false when the agent is not on a baked NavMesh (edit mode: nothing happens)")
             .Function<Vector3, bool>("Warp", (a, point) => a.isActiveAndEnabled && a.Warp(point), "point",
                    "teleport the agent to a world point on the NavMesh (Play Mode); false when no NavMesh is near it or in edit mode")
             .Method("ResetPath", a => { if (OnNavMesh(a)) a.ResetPath(); }, "drop the current path (no-op off the NavMesh)");
        }

        private static bool OnNavMesh(NavMeshAgent a) => a.isActiveAndEnabled && a.isOnNavMesh;
    }
}
