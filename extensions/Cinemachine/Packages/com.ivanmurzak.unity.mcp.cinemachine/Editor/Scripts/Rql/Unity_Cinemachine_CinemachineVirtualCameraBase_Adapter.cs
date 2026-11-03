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
    using CinemachineVirtualCameraBase = global::Unity.Cinemachine.CinemachineVirtualCameraBase;

    /// <summary>
    /// Every Cinemachine camera (CinemachineCamera, ClearShot, StateDrivenCamera, Sequencer, MixingCamera, the deprecated
    /// CM2 cameras): runtime caches and duplicates hidden (Name = name, the *TargetAsGroup / *TargetAsVcam / *TargetChanged
    /// caches, PreviousStateIsValid, upgrade fields); the computed CameraState is explicit-only (`select State.RawPosition`).
    /// </summary>
    public sealed class Unity_Cinemachine_CinemachineVirtualCameraBase_Adapter : RqlTypeAdapter<CinemachineVirtualCameraBase>
    {
        protected override void Members(MemberTable<CinemachineVirtualCameraBase> m)
        {
            CinemachineMembers.HideInternals(m.Untyped, "Name", "IsValid", "ParentCamera", "PreviousStateIsValid",
                "FollowTargetChanged", "LookAtTargetChanged", "FollowTargetAsGroup", "FollowTargetAsVcam", "LookAtTargetAsGroup", "LookAtTargetAsVcam");
            CinemachineMembers.ExplicitOnly(m.Untyped, "State");
        }
    }
}
