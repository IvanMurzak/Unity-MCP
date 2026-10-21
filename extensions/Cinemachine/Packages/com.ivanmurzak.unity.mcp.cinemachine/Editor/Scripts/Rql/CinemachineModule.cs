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
    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): the <c>cinemachine</c> source
    /// (cameras, brains, CreateCamera, EnsureBrain), the CinemachineCamera pipeline methods, and the agent guide rendered into
    /// the unity-rql skill.
    /// </summary>
    public sealed class CinemachineModule : IRqlModule
    {
        public const string GuideTopic = "Cinemachine";

        private const string C = "cinemachine | get cameras";

        internal const string Guide =
@"Cinemachine 3: a `CinemachineCamera` (virtual camera) on its own GameObject; the `CinemachineBrain` on the Unity Camera
shows the live one (highest `Priority.Value`) and blends between them. A camera's pipeline is a Body component
(position: Follow, OrbitalFollow, ThirdPersonFollow, PositionComposer …), an Aim component (rotation: RotationComposer,
HardLookAt, PanTilt …), an optional noise component and extensions, all on the camera's GameObject. Follow / LookAt
targets take a GameObject name or ref. Writes and calls are undoable.

```
editor | call CreateGameObject(""Player"", null, ""Capsule"")   (a target for the examples)
cinemachine | call CreateCamera(""FollowCam"", ""Player"", null, ""(0, 3, -6)"", ""(20, 0, 0)"", 10)   name, follow?, lookAt?, position?, eulerAngles?, priority? (adds a CinemachineBrain to the main Camera)
cinemachine | call CreateCamera(""Overview"")
cinemachine | get cameras | select name Priority.Value IsLive Follow.name LookAt.name body.$type aim.$type noise.$type
" + C + @" | where name == ""FollowCam"" | call SetBody(""Follow"")   Body by name; cinemachine | get bodyTypes lists them (""None"" removes)
" + C + @" | where name == ""FollowCam"" | call SetAim(""RotationComposer"")   Aim by name; cinemachine | get aimTypes
" + C + @" | where name == ""FollowCam"" | set body.FollowOffset = ""(0, 2, -8)"", body.TrackerSettings.PositionDamping = ""(1, 1, 1)""
" + C + @" | where name == ""FollowCam"" | set aim.Composition.ScreenPosition = ""(0.1, 0)"", aim.Damping = ""(0.5, 0.5)""
" + C + @" | where name == ""FollowCam"" | set Lens.FieldOfView = 50, Lens.NearClipPlane = 0.3, Lens.Dutch = 5
" + C + @" | where name == ""Overview"" | set Lens.ModeOverride = ""Orthographic"", Lens.OrthographicSize = 12
" + C + @" | where name == ""FollowCam"" | set Priority.Value = 20
" + C + @" | where name == ""Overview"" | call SetTargets(""Player"")   follow (and look at) Player; SetTargets(follow, lookAt) for both, null clears
" + C + @" | where name == ""Overview"" | set LookAt = null
" + C + @" | where name == ""FollowCam"" | call SetNoise(""Handheld_normal_mild"", 1.5, 1)   profile, amplitudeGain?, frequencyGain?; cinemachine | get noiseProfiles | select name
" + C + @" | where name == ""FollowCam"" | set noise.AmplitudeGain = 0.5
" + C + @" | where name == ""FollowCam"" | call SetBody(""OrbitalFollow"")   replace the Body
" + C + @" | where name == ""FollowCam"" | set body.Radius = 6, body.OrbitStyle = ""Sphere""
objects | where name == ""FollowCam"" | call AddComponent(""CinemachineDeoccluder"")   extensions are components (cinemachine | get extensionTypes)
" + C + @" | select name extensions.$type
cinemachine | call EnsureBrain()   brain on the main Camera (EnsureBrain(""Camera name"") for another)
cinemachine | get brains | set DefaultBlend.Style = ""EaseInOut"", DefaultBlend.Time = 1.5
cinemachine | get brains | select gameObject.name DefaultBlend.Style DefaultBlend.Time UpdateMethod activeCamera
" + C + @" | where name == ""Overview"" | call SetNoise(""None"")
" + C + @" | where name == ""Overview"" | call Destroy()   remove the CinemachineCamera component (objects | … | call Destroy() removes the GameObject)
```

`body` / `aim` / `noise` are the pipeline components themselves, so their members are written directly
(`rql-schema ""CinemachineFollow""` lists them). LensSettings is a struct written in place (`set Lens.FieldOfView = …`);
`Lens.ModeOverride` (None, Orthographic, Perspective, Physical) overrides the Unity Camera's projection. Blend styles:
Cut, EaseInOut, EaseIn, EaseOut, HardIn, HardOut, Linear, Custom. Brains update the live camera in Play Mode; `IsLive`
reports it.";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx) =>
            ctx.AddSource("cinemachine", () => CinemachineApi.Instance,
                   "Cinemachine: cameras, brains, noiseProfiles, bodyTypes / aimTypes / extensionTypes; CreateCamera(name, follow, lookAt, position, eulerAngles, priority), EnsureBrain(camera)")
               .AddAdapter(new Unity_Cinemachine_CinemachineVirtualCameraBase_Adapter())
               .AddAdapter(new Unity_Cinemachine_CinemachineCamera_Adapter())
               .AddAdapter(new Unity_Cinemachine_CinemachineComponentBase_Adapter())
               .AddAdapter(new Unity_Cinemachine_CinemachineBrain_Adapter())
               .AddGuide(GuideTopic, Guide);
    }
}
