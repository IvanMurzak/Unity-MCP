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
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Cinemachine
{
    using CinemachineComponentBase = global::Unity.Cinemachine.CinemachineComponentBase;

    /// <summary>
    /// Pipeline components (Body / Aim / Noise: CinemachineFollow, CinemachineRotationComposer, CinemachineBasicMultiChannelPerlin …):
    /// VirtualCamera is a back-reference (not traversed), the target-group caches and private bookkeeping are hidden, the
    /// computed VcamState is explicit-only. Their settings stay plain members (`set FollowOffset = "(0, 2, -8)"`).
    /// </summary>
    public sealed class Unity_Cinemachine_CinemachineComponentBase_Adapter : RqlTypeAdapter<CinemachineComponentBase>
    {
        protected override void Members(MemberTable<CinemachineComponentBase> m)
        {
            CinemachineMembers.HideInternals(m.Untyped, "FollowTargetAsGroup", "LookAtTargetAsGroup");
            CinemachineMembers.ExplicitOnly(m.Untyped, "VcamState");
            m.Untyped.Reference("VirtualCamera");
        }
    }
}
