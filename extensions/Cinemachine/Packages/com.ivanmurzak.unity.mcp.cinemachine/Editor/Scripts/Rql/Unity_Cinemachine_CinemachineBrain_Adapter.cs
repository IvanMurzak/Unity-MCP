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
    using CinemachineBrain = global::Unity.Cinemachine.CinemachineBrain;
    using CinemachineVirtualCameraBase = global::Unity.Cinemachine.CinemachineVirtualCameraBase;

    /// <summary>
    /// CinemachineBrain: DefaultBlend / CustomBlends / UpdateMethod stay plain members (`set DefaultBlend.Time = 1.5`);
    /// `activeCamera` is the live Cinemachine camera as a component (ActiveVirtualCamera is an interface); duplicates and the
    /// per-frame CameraState / ActiveBlend are hidden or explicit-only.
    /// </summary>
    public sealed class Unity_Cinemachine_CinemachineBrain_Adapter : RqlTypeAdapter<CinemachineBrain>
    {
        protected override void Members(MemberTable<CinemachineBrain> m)
        {
            m.Add<CinemachineVirtualCameraBase?>("activeCamera", b => b.ActiveVirtualCamera as CinemachineVirtualCameraBase,
                description: "the live Cinemachine camera (updated while the brain runs: Play Mode or after an Editor update); null when none");
            CinemachineMembers.HideInternals(m.Untyped, "Name", "Description", "IsValid", "ParentCamera", "ActiveVirtualCamera");
            CinemachineMembers.ExplicitOnly(m.Untyped, "State", "ActiveBlend");
        }
    }
}
