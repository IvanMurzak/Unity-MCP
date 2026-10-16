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
    using NavMeshModifierVolume = global::Unity.AI.Navigation.NavMeshModifierVolume;

    /// <summary>NavMeshModifierVolume: areaName and affectedAgents as names; backing fields hidden behind their properties.</summary>
    public sealed class Unity_AI_Navigation_NavMeshModifierVolume_Adapter : RqlTypeAdapter<NavMeshModifierVolume>
    {
        public override string Description =>
            "NavMeshModifierVolume: a local box (size, center) whose area / areaName applies to the geometry inside; affectedAgents as names";

        protected override void Members(MemberTable<NavMeshModifierVolume> m)
        {
            NavigationMembers.AddAreaName(m, "areaName", c => c.area, (c, i) => c.area = i, "area");
            NavigationMembers.AddAffectedAgents(m);
            NavigationMembers.HideBackingFields(m.Untyped);
        }
    }
}
