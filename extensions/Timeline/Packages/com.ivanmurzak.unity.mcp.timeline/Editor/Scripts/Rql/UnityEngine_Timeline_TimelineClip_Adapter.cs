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
using System.Globalization;
using System.Linq;
using Rql.Execution;
using UnityEditor;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Timelines
{
    using TimelineClip = UnityEngine.Timeline.TimelineClip;
    using TrackAsset = UnityEngine.Timeline.TrackAsset;

    /// <summary>
    /// TimelineClip is a plain class serialized inside its track, so a write on it would reach no UnityEngine.Object: the engine
    /// could neither record it for Undo nor mark the asset dirty. Every writable member is therefore rewrapped to record the
    /// parent track first and mark track and timeline dirty after. Timing values are validated instead of Timeline's silent
    /// clamping (negative start, blends longer than the clip, a time scale on clips that cannot change speed …).
    /// </summary>
    public sealed class UnityEngine_Timeline_TimelineClip_Adapter : RqlTypeAdapter<TimelineClip>
    {
        public override string Description =>
            "TimelineClip: start, duration, end, clipIn, timeScale, easeIn/OutDuration, displayName, blendIn/OutDuration (read-only: overlap clips), asset (its PlayableAsset), " +
            "track; move with set start = start + 1; call Delete()";

        protected override void Members(MemberTable<TimelineClip> m)
        {
            m.HideWhere(a => a.Member != null && a.Member.IsDefined(typeof(ObsoleteAttribute), true));
            if (m.Untyped.Contains("asset")) m.ReadOnly("asset");
            // Blends are computed from overlapping clips (Timeline recalculates them on every change): move clips to overlap instead.
            foreach (var blend in new[] { "blendInDuration", "blendOutDuration" })
                if (m.Untyped.Contains(blend)) m.Untyped.ReadOnly(blend).Describe(blend, "crossfade length, computed from the overlap with the neighbouring clip (move clips to change it); -1 = none");

            foreach (var member in m.Untyped.Members.Where(a => a.CanWrite).ToList())
            {
                var original = member;
                var name = member.Name;
                m.Untyped.Add(name, original.Type, original.Getter,
                    (o, v) =>
                    {
                        var clip = (TimelineClip)o;
                        var track = clip.GetParentTrack();
                        if (track != null) Undo.RecordObject(track, "RQL timeline clip " + name);
                        original.SetValue(o, v);
                        TimelineOps.Dirty(track);
                    },
                    (o, v) => Validate((TimelineClip)o, name, v), original.Description, original.Owning);
            }

            m.Add("track", c => c.GetParentTrack(), description: "the TrackAsset holding the clip")
             .Method("Delete", c => Delete(c), "delete the clip and its PlayableAsset (undoable)");
        }

        private static string? Validate(TimelineClip clip, string member, object? value)
        {
            if (!(value is double v)) return null;
            string Num(double d) => d.ToString(CultureInfo.InvariantCulture);
            string? Caps(ClipCaps caps, string what) =>
                (clip.clipCaps & caps) != 0 || v == 0.0 ? null : $"{clip.asset?.GetType().Name ?? "this clip"} does not support {what} (clipCaps: {clip.clipCaps})";
            switch (member)
            {
                case "start":
                    return v >= 0.0 ? null : $"start {Num(v)} must be >= 0";
                case "duration":
                    return v > 0.0 ? null : $"duration {Num(v)} must be > 0";
                case "clipIn":
                    return v < 0.0 ? $"clipIn {Num(v)} must be >= 0" : Caps(ClipCaps.ClipIn, "clipIn");
                case "timeScale":
                    if (v < TimelineClip.kTimeScaleMin || v > TimelineClip.kTimeScaleMax)
                        return $"timeScale {Num(v)} must be within [{Num(TimelineClip.kTimeScaleMin)}, {Num(TimelineClip.kTimeScaleMax)}]";
                    return v == 1.0 ? null : Caps(ClipCaps.SpeedMultiplier, "a time scale");
                case "easeInDuration":
                case "easeOutDuration":
                    if (v < 0.0) return $"{member} {Num(v)} must be >= 0";
                    // Timeline clamps an ease to the part of the clip outside the opposite blend (crossfade): reject instead.
                    var blend = member == "easeInDuration" ? clip.blendOutDuration : clip.blendInDuration;
                    var available = clip.duration - Math.Max(blend, 0.0);
                    if (v > available)
                        return $"{member} {Num(v)} is longer than the clip ({Num(available)} s" + (blend > 0.0 ? $" outside its {Num(blend)} s blend)" : ")");
                    return Caps(ClipCaps.Blending, "blending / easing");
                default:
                    return null;
            }
        }

        private static void Delete(TimelineClip clip)
        {
            var track = TimelineOps.ParentTrack(clip);
            var timeline = track.timelineAsset;
            if (timeline == null || !timeline.DeleteClip(clip)) throw new InvalidOperationException($"clip '{clip.displayName}' could not be deleted");
            TimelineOps.Dirty(track);
        }
    }
}
