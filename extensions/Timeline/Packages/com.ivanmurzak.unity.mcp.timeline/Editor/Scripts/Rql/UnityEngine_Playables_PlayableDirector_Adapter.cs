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
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Timelines
{
    using PlayableDirector = UnityEngine.Playables.PlayableDirector;
    using TimelineAsset = UnityEngine.Timeline.TimelineAsset;
    using TrackAsset = UnityEngine.Timeline.TrackAsset;

    /// <summary>
    /// PlayableDirector: the per-track scene bindings of its timeline as a map keyed by track name,
    /// <c>set bindings["Hero"] = ref("GameObject#…")</c> — a GameObject is narrowed to the component the track drives (Animator,
    /// AudioSource …) and a component widened to its GameObject for GameObject tracks, mismatches fail. <c>playableGraph</c>
    /// (a native handle, invalid outside playback) is hidden.
    /// </summary>
    public sealed class UnityEngine_Playables_PlayableDirector_Adapter : RqlTypeAdapter<PlayableDirector>
    {
        public override string Description =>
            "PlayableDirector: playableAsset (the TimelineAsset), playOnAwake, time, extrapolationMode; " +
            "bindings[\"Track name\"] = the scene object bound to each output track of the timeline";

        protected override void Members(MemberTable<PlayableDirector> m)
        {
            if (m.Untyped.Contains("playableGraph")) m.Hide("playableGraph");
            m.Map<Object?>("bindings", Keys, (d, k) => Find(d, k) is { Count: 1 } found ? d.GetGenericBinding(found[0]) : null,
                (d, k, v) =>
                {
                    var track = Track(d, k);
                    d.SetGenericBinding(track, Narrow(track, v, out _));
                },
                (d, k, v) => Validate(d, k, v),
                "scene object bound to each output track of the timeline, by track name (null = unbound)",
                // Without a timeline every key "exists", so the write reaches validation and explains what is missing.
                (d, k) => !(d.playableAsset is TimelineAsset) || Find(d, k).Count > 0);
        }

        private static IEnumerable<string> Keys(PlayableDirector d) =>
            d.playableAsset is TimelineAsset t ? t.GetOutputTracks().Where(x => x != null).Select(x => x.name) : Enumerable.Empty<string>();

        private static List<TrackAsset> Find(PlayableDirector d, string name) =>
            d.playableAsset is TimelineAsset t ? t.GetOutputTracks().Where(x => x != null && x.name == name).ToList() : new List<TrackAsset>();

        private static TrackAsset Track(PlayableDirector d, string name)
        {
            var found = Find(d, name);
            return found.Count == 1 ? found[0] : throw new ArgumentException(Problem(d, name, found.Count)!);
        }

        private static string? Problem(PlayableDirector d, string name, int count)
        {
            if (!(d.playableAsset is TimelineAsset t)) return $"director '{d.name}' plays no TimelineAsset: set playableAsset = unity.asset(\"….playable\") first";
            if (count == 1) return null;
            if (count > 1) return $"timeline '{t.name}' has {count} output tracks named \"{name}\": rename one";
            return $"timeline '{t.name}' has no output track \"{name}\" (tracks: {string.Join(", ", Keys(d).Select(k => $"\"{k}\""))})";
        }

        private static string? Validate(PlayableDirector d, string key, Object? value)
        {
            var found = Find(d, key);
            var problem = Problem(d, key, found.Count);
            if (problem != null) return problem;
            Narrow(found[0], value, out var reason);
            return reason;
        }

        /// <summary>The object to bind: the value itself, the matching component of a GameObject, or a component's GameObject.</summary>
        private static Object? Narrow(TrackAsset track, Object? value, out string? reason)
        {
            reason = null;
            if (value == null) return null;
            var type = UnityEngine_Timeline_TrackAsset_Adapter.BindingType(track);
            if (type == null)
            {
                reason = $"{track.GetType().Name} '{track.name}' takes no binding";
                return null;
            }
            if (type.IsInstanceOfType(value)) return value;
            var go = value is GameObject g ? g : value is Component c ? c.gameObject : null;
            if (go != null)
            {
                if (type == typeof(GameObject)) return go;
                if (typeof(Component).IsAssignableFrom(type))
                {
                    var component = go.GetComponent(type);
                    if (component != null) return component;
                    reason = $"'{go.name}' has no {type.Name} for {track.GetType().Name} '{track.name}' (add one: call AddComponent(\"{type.Name}\"))";
                    return null;
                }
            }
            reason = $"{track.GetType().Name} '{track.name}' binds a {type.Name}, not {value.GetType().Name} '{value.name}'";
            return null;
        }
    }
}
