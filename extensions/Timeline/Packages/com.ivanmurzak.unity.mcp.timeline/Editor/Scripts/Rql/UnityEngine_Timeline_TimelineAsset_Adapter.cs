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
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Timelines
{
    using TimelineAsset = UnityEngine.Timeline.TimelineAsset;
    using TrackAsset = UnityEngine.Timeline.TrackAsset;

    /// <summary>
    /// TimelineAsset: <c>tracks</c> (root tracks, the marker track excluded), <c>markers</c> (timeline-level markers) and a
    /// validated <c>frameRate</c> as members; AddTrack / AddMarker as methods. The internal serialized lists (m_Tracks …) and
    /// <c>editorSettings</c> (a plain class: writes into it would not mark the asset dirty) are hidden.
    /// </summary>
    public sealed class UnityEngine_Timeline_TimelineAsset_Adapter : RqlTypeAdapter<TimelineAsset>
    {
        public override string Description =>
            "TimelineAsset: tracks (root tracks; nested: recurse children), markers, frameRate, durationMode, fixedDuration, duration; " +
            "call AddTrack(type, name?, group?), AddMarker(time, type?)";

        private static CallableParameter P<TP>(string name) => new CallableParameter(name, typeof(TP));
        private static CallableParameter P<TP>(string name, TP fallback) => new CallableParameter(name, typeof(TP), true, fallback);

        protected override void Members(MemberTable<TimelineAsset> m)
        {
            TimelineOps.HideSerializedFields(m.Untyped);
            if (m.Untyped.Contains("outputs")) m.ExplicitOnly("outputs");
            m.Hide("editorSettings")
             .Reference("markerTrack")
             .Add("tracks", t => TimelineOps.RootTracks(t), description: "root tracks in order (groups included, nested tracks via children; the marker track is markerTrack)")
             .Add("markers", t => t.markerTrack == null ? new List<Object>() : UnityEngine_Timeline_TrackAsset_Adapter.Markers(t.markerTrack),
                  description: "markers on the timeline's own marker track (above all tracks)")
             .Add("frameRate", t => t.editorSettings.frameRate, (t, v) => t.editorSettings.frameRate = v,
                  (t, v) => v > 0.0 && v <= 1000.0 ? null : $"frameRate {v} must be within (0, 1000]",
                  "editor frame rate (frames per second) used for snapping and display");

            m.Untyped.AddMethod("AddTrack", new[] { P<string>("type"), P<string?>("name", null), P<string?>("group", null) }, typeof(TrackAsset),
                (o, a) => AddTrack((TimelineAsset)o, (string)a[0]!, (string?)a[1], (string?)a[2]),
                $"add a track: type is {TimelineOps.TrackKinds} or a TrackAsset type name; name defaults to the type (made unique); " +
                "group = name of a GroupTrack to nest it in; returns the track");
            m.Untyped.AddMethod("AddMarker", new[] { P<double>("time"), P("type", "Signal") }, typeof(Object),
                (o, a) => AddMarker((TimelineAsset)o, (double)a[0]!, (string)a[1]!),
                "add a marker (Signal = SignalEmitter, or a Marker type name) at time seconds on the timeline's marker track; returns it");
        }

        private static TrackAsset AddTrack(TimelineAsset timeline, string type, string? name, string? group)
        {
            var trackType = TimelineOps.ResolveTrackType(type);
            if (trackType == typeof(MarkerTrack)) throw new ArgumentException("the marker track is created by AddMarker");
            TrackAsset? parent = null;
            if (!string.IsNullOrEmpty(group))
                parent = TimelineOps.FindTrack(timeline, group!, t => t is GroupTrack);
            var track = timeline.CreateTrack(trackType, parent, string.IsNullOrWhiteSpace(name) ? null : name);
            TimelineOps.Dirty(track);
            return track;
        }

        private static Object AddMarker(TimelineAsset timeline, double time, string type)
        {
            if (time < 0.0) throw new ArgumentException($"time {time} must be >= 0");
            var markerType = TimelineOps.ResolveMarkerType(type);
            if (timeline.markerTrack == null)
            {
                timeline.CreateMarkerTrack();
                Undo.RegisterCreatedObjectUndo(timeline.markerTrack, "RQL timeline marker track");
            }
            var marker = timeline.markerTrack!.CreateMarker(markerType, time);
            TimelineOps.Dirty(timeline.markerTrack);
            return (Object)marker;
        }
    }
}
