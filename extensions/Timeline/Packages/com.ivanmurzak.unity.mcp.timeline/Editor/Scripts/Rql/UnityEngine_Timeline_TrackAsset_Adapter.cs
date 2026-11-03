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
using System;
using System.Collections.Generic;
using System.Linq;
using Rql.Execution;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Timelines
{
    using TimelineAsset = UnityEngine.Timeline.TimelineAsset;
    using TrackAsset = UnityEngine.Timeline.TrackAsset;

    /// <summary>
    /// Every TrackAsset (Animation, Activation, Audio, Signal, Control, Playable, Group, custom): <c>clips</c>, <c>markers</c>,
    /// <c>children</c> (sub-tracks of a group) and <c>bindingType</c> as members, AddClip / AddMarker / Delete as methods. The
    /// serialized lists (m_Clips, m_Children …) are hidden: they are only consistent when changed through Timeline's API.
    /// </summary>
    public sealed class UnityEngine_Timeline_TrackAsset_Adapter : RqlTypeAdapter<TrackAsset>
    {
        public override string Description =>
            "TrackAsset: name, muted, locked, clips, markers, children (group tracks), bindingType (what a PlayableDirector binds to it); " +
            "call AddClip(asset?, start?, duration?, name?), AddMarker(time, type?), Delete()";

        private static CallableParameter P<TP>(string name) => new CallableParameter(name, typeof(TP));
        private static CallableParameter P<TP>(string name, TP fallback) => new CallableParameter(name, typeof(TP), true, fallback);

        public static List<Object> Markers(TrackAsset track) => track.GetMarkers().OfType<Object>().ToList();

        /// <summary>The scene type a PlayableDirector binds to the track (Animator, GameObject, AudioSource …), or null.</summary>
        public static Type? BindingType(TrackAsset track)
        {
            foreach (var output in track.outputs)
                if (output.outputTargetType != null) return output.outputTargetType;
            return null;
        }

        protected override void Members(MemberTable<TrackAsset> m)
        {
            TimelineOps.HideSerializedFields(m.Untyped);
            if (m.Untyped.Contains("outputs")) m.ExplicitOnly("outputs");
            m.Reference("timelineAsset", "parent")
             .Add("clips", t => t.GetClips().ToList(), description: "clips in time order; members start, duration, clipIn, blends, eases, timeScale, displayName, asset")
             .Add("markers", t => Markers(t), description: "markers on this track (SignalEmitter …): time, asset")
             .Add("children", t => t.GetChildTracks().ToList(), description: "sub-tracks (GroupTrack children, override tracks)")
             .Add("bindingType", t => BindingType(t)?.Name, description: "type of the scene object a PlayableDirector binds to this track (null = none)");

            m.Untyped.AddMethod("AddClip", new[] { P<Object?>("asset", null), P("start", 0.0), P("duration", 0.0), P<string?>("name", null) }, typeof(TimelineClip),
                (o, a) => AddClip((TrackAsset)o, (Object?)a[0], (double)a[1]!, (double)a[2]!, (string?)a[3]),
                "add a clip at start seconds: AnimationTrack takes an AnimationClip, AudioTrack an AudioClip (unity.asset(\"…\")), other tracks " +
                "a default clip (asset null); duration 0 keeps the default / source length; returns the clip");
            m.Untyped.AddMethod("AddMarker", new[] { P<double>("time"), P("type", "Signal") }, typeof(Object),
                (o, a) => AddMarker((TrackAsset)o, (double)a[0]!, (string)a[1]!),
                "add a marker (Signal = SignalEmitter, or a Marker type name) at time seconds on this track; returns it");
            m.Untyped.AddMethod("Delete", Array.Empty<CallableParameter>(), typeof(void),
                (o, a) => { Delete((TrackAsset)o); return null; },
                "delete the track with its clips, markers and sub-tracks (undoable)");
        }

        private static TimelineClip AddClip(TrackAsset track, Object? asset, double start, double duration, string? name)
        {
            if (start < 0.0) throw new ArgumentException($"start {start} must be >= 0");
            if (duration < 0.0) throw new ArgumentException($"duration {duration} must be >= 0 (0 = default length)");
            TimelineClip? clip;
            switch (asset)
            {
                case null:
                    if (!HasClips(track))
                        throw new ArgumentException($"{track.GetType().Name} '{track.name}' has no clips" +
                                                    (track.supportsNotifications ? ": use AddMarker" : ""));
                    clip = track.CreateDefaultClip();
                    break;
                case AnimationClip animation when track is AnimationTrack animationTrack:
                    clip = animationTrack.CreateClip(animation);
                    break;
                case AudioClip audio when track is AudioTrack audioTrack:
                    clip = audioTrack.CreateClip(audio);
                    break;
                default:
                    throw new ArgumentException($"{TimelineOps.Describe(asset)} does not fit {track.GetType().Name} '{track.name}': an AnimationTrack " +
                                                "takes an AnimationClip, an AudioTrack an AudioClip, other tracks a default clip (asset null)");
            }
            if (clip == null) throw new InvalidOperationException($"{track.GetType().Name} '{track.name}' could not create a clip");
            clip.start = start;
            if (duration > 0.0) clip.duration = duration;
            if (!string.IsNullOrEmpty(name)) clip.displayName = name!;
            TimelineOps.Dirty(track);
            return clip;
        }

        /// <summary>Whether the track hosts clips (GroupTrack declares TrackAsset as its "clip" type for its sub-tracks).</summary>
        private static bool HasClips(TrackAsset track) =>
            track.GetType().GetCustomAttributes(typeof(TrackClipTypeAttribute), true).OfType<TrackClipTypeAttribute>()
                .Any(a => a.inspectedType != null && !a.inspectedType.IsAbstract && !typeof(TrackAsset).IsAssignableFrom(a.inspectedType)
                          && typeof(ScriptableObject).IsAssignableFrom(a.inspectedType)
                          && typeof(UnityEngine.Playables.IPlayableAsset).IsAssignableFrom(a.inspectedType));

        private static Object AddMarker(TrackAsset track, double time, string type)
        {
            if (time < 0.0) throw new ArgumentException($"time {time} must be >= 0");
            var markerType = TimelineOps.ResolveMarkerType(type);
            if (typeof(UnityEngine.Playables.INotification).IsAssignableFrom(markerType) && !track.supportsNotifications)
                throw new ArgumentException($"{track.GetType().Name} '{track.name}' does not accept {markerType.Name} (notifications): use a SignalTrack or the timeline's AddMarker");
            var marker = track.CreateMarker(markerType, time);
            TimelineOps.Dirty(track);
            return (Object)marker;
        }

        private static void Delete(TrackAsset track)
        {
            var timeline = track.timelineAsset;
            if (timeline == null) throw new InvalidOperationException($"track '{track.name}' is not part of a timeline");
            if (track == timeline.markerTrack) throw new ArgumentException("the timeline's marker track cannot be deleted: delete its markers (… | get markers | call Delete())");
            if (!timeline.DeleteTrack(track)) throw new InvalidOperationException($"track '{track.name}' could not be deleted");
            TimelineOps.Dirty(timeline);
        }
    }
}
