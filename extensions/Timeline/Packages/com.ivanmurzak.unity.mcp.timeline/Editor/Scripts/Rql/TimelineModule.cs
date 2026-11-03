/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Timelines
{
    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): the <c>timeline</c> source
    /// (create TimelineAsset / SignalAsset files), TimelineAsset / TrackAsset / TimelineClip / Marker members and methods,
    /// PlayableDirector bindings, and the agent guide rendered into the unity-rql skill.
    /// </summary>
    public sealed class TimelineModule : IRqlModule
    {
        public const string GuideTopic = "Timeline";

        private const string A = "unity.asset(\"Assets/Cutscenes/Intro.playable\")";
        private const string D = "objects | get components | where $ is PlayableDirector";

        private const string Guide =
@"A TimelineAsset (`.playable`) holds tracks; tracks hold clips and markers; a scene `PlayableDirector` plays it and binds
each track to a scene object. Times are seconds. Find timelines with `unity.assets(""t:TimelineAsset"")`.

```
unity.assets(""t:TimelineAsset"") | select $ref name duration frameRate tracks.name
" + A + @" | get tracks | select $ref name $type bindingType muted clips.count() markers.count()
" + A + @" | get tracks | recurse children | select $depth name $type              groups and their sub-tracks
" + A + @" | get tracks | get clips | select track.name displayName start duration end asset.name
timeline | call Create(""Assets/Cutscenes/Intro.playable"", 30)                 path, frameRate?, fixedDuration? (> 0 = FixedLength)
" + A + @" | set frameRate = 24, durationMode = ""FixedLength"", fixedDuration = 12
```

Tracks (type: Animation, Activation, Audio, Signal, Control, Playable, Group or a TrackAsset type name):

```
" + A + @" | call AddTrack(""Animation"", ""Hero"")                 type, name?, group? — returns the track
" + A + @" | call AddTrack(""Group"", ""Crowd"")
" + A + @" | call AddTrack(""Activation"", ""Door"", ""Crowd"")       nested in the GroupTrack named Crowd
" + A + @" | get tracks | where name == ""Hero"" | set muted = true, locked = false, name = ""Hero Anim""
" + A + @" | get tracks | where name == ""Door"" | call Delete()        with its clips, markers and sub-tracks
```

Clips (AnimationTrack: an AnimationClip, AudioTrack: an AudioClip, other tracks: a default clip with asset null):

```
" + A + @" | get tracks | where name == ""Hero"" | call AddClip(unity.asset(""Assets/Anim/Walk.anim""), 2, 3, ""Walk"")   asset?, start?, duration?, name?
" + A + @" | get tracks | where name == ""Door"" | call AddClip(null, 0, 1.5)
" + A + @" | get tracks | get clips | where displayName == ""Walk"" | set start = start + 0.5          move (duration kept)
" + A + @" | get tracks | get clips | where displayName == ""Walk"" | set duration = 4, clipIn = 0.25, timeScale = 1.5, easeInDuration = 0.5, easeOutDuration = 0.3
" + A + @" | get tracks | get clips | where displayName == ""Walk"" | set asset.removeStartOffset = false   the clip's PlayableAsset
" + A + @" | get tracks | get clips | where end > 10 | call Delete()
```

Markers and signals:

```
timeline | call CreateSignal(""Assets/Cutscenes/Boom.signal"")
" + A + @" | call AddMarker(3.5)                                    SignalEmitter on the timeline marker track
" + A + @" | get tracks | where $type == ""SignalTrack"" | call AddMarker(1, ""Signal"")
" + A + @" | get markers | set asset = unity.asset(""Assets/Cutscenes/Boom.signal""), time = 4
" + A + @" | get tracks | get markers | where time > 8 | call Delete()
```

Director and bindings (a GameObject is narrowed to the component the track drives, e.g. its Animator):

```
objects | where name == ""Cutscene"" | call AddComponent(""PlayableDirector"")
" + D + @" | where gameObject.name == ""Cutscene"" | set playableAsset = " + A + @", playOnAwake = false
" + D + @" | select gameObject.name playableAsset.name bindings
" + D + @" | set bindings[""Hero""] = ref(""GameObject#…""), bindings[""Door""] = null
```

Crossfades (blendInDuration / blendOutDuration, read-only) come from overlapping clips: move a clip's start into its neighbour.
Invalid values (negative times, eases longer than the clip, a time scale on clips that cannot change speed, a clip asset that does not fit the track, unknown track names)
fail without changing anything; Timeline does not clamp them silently here. Creating, deleting and editing tracks, clips,
markers and bindings is undoable; created asset files stay. rql-mutate saves the edited .playable.
Clips have no $ref: address them through their timeline (`… | get tracks | get clips | where …`).";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            ctx.AddSource("timeline", () => TimelineEditorApi.Instance,
                   "timeline operations without an owner: Create (TimelineAsset .playable), CreateSignal (SignalAsset .signal)")
               .AddAdapter(new UnityEngine_Timeline_TimelineAsset_Adapter())
               .AddAdapter(new UnityEngine_Timeline_TrackAsset_Adapter())
               .AddAdapter(new UnityEngine_Timeline_TimelineClip_Adapter())
               .AddAdapter(new UnityEngine_Timeline_Marker_Adapter())
               .AddAdapter(new UnityEngine_Playables_PlayableDirector_Adapter())
               .AddGuide(GuideTopic, Guide);
        }
    }
}
