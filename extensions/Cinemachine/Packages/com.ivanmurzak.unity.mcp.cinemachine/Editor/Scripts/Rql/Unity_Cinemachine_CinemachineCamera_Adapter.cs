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
using System;
using System.Collections.Generic;
using System.Linq;
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Cinemachine
{
    using CinemachineBasicMultiChannelPerlin = global::Unity.Cinemachine.CinemachineBasicMultiChannelPerlin;
    using CinemachineCamera = global::Unity.Cinemachine.CinemachineCamera;
    using CinemachineComponentBase = global::Unity.Cinemachine.CinemachineComponentBase;
    using CinemachineExtension = global::Unity.Cinemachine.CinemachineExtension;
    using Stage = global::Unity.Cinemachine.CinemachineCore.Stage;

    /// <summary>
    /// CinemachineCamera: the pipeline as read-only members (body, aim, noise, extensions) and the pipeline operations as
    /// methods (SetBody / SetAim by component name, SetNoise by profile name, SetTargets by GameObject name or ref). Lens,
    /// Priority, Follow / LookAt and the rest stay plain members (<c>set Lens.FieldOfView = 50</c>). The camera is the call
    /// owner, so the engine records it for Undo; added / removed components are registered with Undo here.
    /// </summary>
    public sealed class Unity_Cinemachine_CinemachineCamera_Adapter : RqlTypeAdapter<CinemachineCamera>
    {
        public override string Description =>
            "CinemachineCamera: set Lens.FieldOfView / Priority.Value / Follow / LookAt; body, aim, noise, extensions (the pipeline components); " +
            "call SetBody(type) / SetAim(type) / SetNoise(profile, amplitudeGain?, frequencyGain?) / SetTargets(follow, lookAt?)";

        protected override void Members(MemberTable<CinemachineCamera> m)
        {
            m.Add<CinemachineComponentBase?>("body", c => CinemachinePipeline.Component(c, Stage.Body),
                    description: "the position-control (Body) component, e.g. CinemachineFollow; null without one (SetBody)")
             .Add<CinemachineComponentBase?>("aim", c => CinemachinePipeline.Component(c, Stage.Aim),
                    description: "the rotation-control (Aim) component, e.g. CinemachineRotationComposer; null without one (SetAim)")
             .Add<CinemachineComponentBase?>("noise", c => CinemachinePipeline.Component(c, Stage.Noise),
                    description: "the noise component (CinemachineBasicMultiChannelPerlin); null without one (SetNoise)")
             .Add<List<CinemachineExtension>>("extensions", c => c.GetComponents<CinemachineExtension>().Where(e => e != null).ToList(),
                    description: "CinemachineExtension components on the camera (Deoccluder, Confiner3D …; add with call AddComponent)");

            var t = m.Untyped;
            t.AddMethod("SetBody", new[] { Req("type", typeof(string)) }, typeof(CinemachineComponentBase),
                (c, a) => CinemachinePipeline.Replace((CinemachineCamera)c, Stage.Body, (string?)a[0]),
                "make type (Follow, OrbitalFollow, ThirdPersonFollow, PositionComposer, HardLockToTarget, SplineDolly; \"None\" removes it) " +
                "the Body component, replacing the previous one (kept with its settings when it is already that type); returns it");
            t.AddMethod("SetAim", new[] { Req("type", typeof(string)) }, typeof(CinemachineComponentBase),
                (c, a) => CinemachinePipeline.Replace((CinemachineCamera)c, Stage.Aim, (string?)a[0]),
                "make type (RotationComposer, HardLookAt, PanTilt, RotateWithFollowTarget, SplineDollyLookAtTargets; \"None\" removes it) " +
                "the Aim component, replacing the previous one; returns it");
            t.AddMethod("SetNoise", new[] { Req("profile", typeof(string)), Opt("amplitudeGain", typeof(float), 1f), Opt("frequencyGain", typeof(float), 1f) },
                typeof(CinemachineBasicMultiChannelPerlin),
                (c, a) => CinemachinePipeline.SetNoise((CinemachineCamera)c, (string?)a[0], (float)a[1]!, (float)a[2]!),
                "procedural shake: a CinemachineBasicMultiChannelPerlin with the NoiseSettings profile (name such as \"6D Shake\" or an " +
                "asset path; cinemachine | get noiseProfiles lists them) and gains; \"None\" removes the noise; returns the component");
            t.AddMethod("SetTargets", new[] { Req("follow", typeof(object)), Opt("lookAt", typeof(object), null) }, typeof(void),
                (c, a) => { SetTargets((CinemachineCamera)c, a[0], a[1]); return null; },
                "set Follow and LookAt from GameObject names or refs (null clears); without lookAt the camera looks at the Follow target");
        }

        private static void SetTargets(CinemachineCamera camera, object? follow, object? lookAt)
        {
            var followTarget = CinemachinePipeline.ResolveTarget(follow, nameof(follow));
            var lookAtTarget = CinemachinePipeline.ResolveTarget(lookAt, nameof(lookAt));
            camera.Follow = followTarget;
            if (lookAtTarget != null) camera.LookAt = lookAtTarget;
            else
            {
                // LookAt follows the Follow target (the CinemachineCamera default)
                camera.Target.CustomLookAtTarget = false;
                camera.Target.LookAtTarget = null;
            }
        }

        private static CallableParameter Req(string name, Type type) => new CallableParameter(name, type);

        private static CallableParameter Opt(string name, Type type, object? value) => new CallableParameter(name, type, true, value);
    }
}
