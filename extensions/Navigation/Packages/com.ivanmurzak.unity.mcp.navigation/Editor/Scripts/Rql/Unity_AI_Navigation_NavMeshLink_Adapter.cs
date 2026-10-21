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
    using NavMeshLink = global::Unity.AI.Navigation.NavMeshLink;

    /// <summary>
    /// NavMeshLink: agentType / areaName as names. Backing fields are hidden: the property setters rebuild the link, and
    /// costModifier is stored as m_CostModifier signed by m_IsOverridingCost.
    /// </summary>
    public sealed class Unity_AI_Navigation_NavMeshLink_Adapter : RqlTypeAdapter<NavMeshLink>
    {
        public override string Description =>
            "NavMeshLink: startPoint / endPoint (local), width, bidirectional, costModifier (negative = area cost), area / areaName, agentType";

        protected override void Members(MemberTable<NavMeshLink> m)
        {
            NavigationMembers.AddAgentType(m, l => l.agentTypeID, (l, id) => l.agentTypeID = id);
            NavigationMembers.AddAreaName(m, "areaName", l => l.area, (l, i) => l.area = i, "area");
            NavigationMembers.HideBackingFields(m.Untyped, "m_IsOverridingCost", "m_AutoUpdatePosition", "m_AutoUpdatePositions");
        }
    }
}
