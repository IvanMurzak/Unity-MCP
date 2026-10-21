/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Cinemachine)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using System.Reflection;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Cinemachine
{
    /// <summary>Member configuration shared by the Cinemachine adapters.</summary>
    public static class CinemachineMembers
    {
        /// <summary>
        /// Hide Cinemachine's private [SerializeField, HideInInspector] bookkeeping (m_StreamingVersion, m_LegacyPriority,
        /// m_NoiseOffsets …: upgrade markers and random seeds the Inspector never shows) and the given members where the type has them.
        /// </summary>
        public static MemberTable HideInternals(MemberTable table, params string[] names)
        {
            table.HideWhere(m => m.Member is FieldInfo f && f.IsPrivate && f.IsDefined(typeof(HideInInspector), true));
            return table.HideExisting(names);
        }

        /// <summary>Make the given members explicit-only (computed per read, large, or runtime-only) where the type has them.</summary>
        public static MemberTable ExplicitOnly(MemberTable table, params string[] names) =>
            table.ExplicitOnly(names.Where(table.Contains).ToArray());
    }
}
