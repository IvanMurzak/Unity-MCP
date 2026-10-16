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
    using NavMeshModifier = global::Unity.AI.Navigation.NavMeshModifier;

    /// <summary>NavMeshModifier: areaName and affectedAgents as names; backing fields hidden behind their properties.</summary>
    public sealed class Unity_AI_Navigation_NavMeshModifier_Adapter : RqlTypeAdapter<NavMeshModifier>
    {
        public override string Description =>
            "NavMeshModifier: overrideArea + area / areaName, ignoreFromBuild, applyToChildren, affectedAgents (agent type names or [\"All\"])";

        protected override void Members(MemberTable<NavMeshModifier> m)
        {
            NavigationMembers.AddAreaName(m, "areaName", c => c.area, (c, i) => c.area = i, "area (set overrideArea = true for it to apply)");
            NavigationMembers.AddAffectedAgents(m);
            NavigationMembers.HideBackingFields(m.Untyped);
        }
    }
}
