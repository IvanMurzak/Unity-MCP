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
using System.IO;
using System.Linq;
using System.Reflection;
using Rql.Execution;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Timelines
{
    /// <summary>
    /// Shared Timeline operations behind the RQL surface: track / marker type resolution, track lookup, asset paths and the
    /// dirty bookkeeping. Timeline's own create / delete calls register their Undo steps; the engine records the call target.
    /// </summary>
    internal static class TimelineOps
    {
        public const string TrackKinds = "Animation, Activation, Audio, Signal, Control, Playable, Group";

        /// <summary>Private serialized fields (m_Tracks, m_Clips …) are Timeline's internal bookkeeping: writing them corrupts the asset.</summary>
        public static MemberTable HideSerializedFields(MemberTable m) =>
            m.HideWhere(a => a.Member is FieldInfo f && !f.IsPublic && f.Name.StartsWith("m_", StringComparison.Ordinal));

        /// <summary>Root tracks without the timeline's marker track (it is <c>markerTrack</c>), so indices stay stable.</summary>
        public static List<TrackAsset> RootTracks(TimelineAsset timeline) =>
            timeline.GetRootTracks().Where(t => t != null && t != timeline.markerTrack).ToList();

        /// <summary>Every track of the timeline (groups and nested tracks included), depth-first.</summary>
        public static List<TrackAsset> AllTracks(TimelineAsset timeline)
        {
            var all = new List<TrackAsset>();
            void Walk(IEnumerable<TrackAsset> tracks)
            {
                foreach (var t in tracks)
                {
                    if (t == null) continue;
                    all.Add(t);
                    Walk(t.GetChildTracks());
                }
            }
            Walk(RootTracks(timeline));
            return all;
        }

        /// <summary>The single track named <paramref name="name"/> anywhere in the timeline.</summary>
        public static TrackAsset FindTrack(TimelineAsset timeline, string name, Func<TrackAsset, bool>? filter = null)
        {
            var matches = AllTracks(timeline).Where(t => t.name == name && (filter == null || filter(t))).ToList();
            if (matches.Count == 1) return matches[0];
            var names = string.Join(", ", AllTracks(timeline).Select(t => $"\"{t.name}\""));
            if (matches.Count == 0) throw new ArgumentException($"timeline '{timeline.name}' has no track \"{name}\" (tracks: {(names.Length == 0 ? "none" : names)})");
            throw new ArgumentException($"timeline '{timeline.name}' has {matches.Count} tracks named \"{name}\": rename one first (set name = …)");
        }

        public static Type ResolveTrackType(string kind)
        {
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException($"track type is empty: use {TrackKinds} or a TrackAsset type name");
            switch (kind.Trim().ToLowerInvariant())
            {
                case "animation": return typeof(AnimationTrack);
                case "activation": return typeof(ActivationTrack);
                case "audio": return typeof(AudioTrack);
                case "signal": return typeof(SignalTrack);
                case "control": return typeof(ControlTrack);
                case "playable": return typeof(PlayableTrack);
                case "group": return typeof(GroupTrack);
            }
            return ResolveType<TrackAsset>(kind, "track type", TrackKinds);
        }

        public static Type ResolveMarkerType(string kind)
        {
            if (string.IsNullOrWhiteSpace(kind) || kind.Trim().Equals("Signal", StringComparison.OrdinalIgnoreCase))
                return typeof(SignalEmitter);
            var type = ResolveType<IMarker>(kind, "marker type", "Signal");
            if (!typeof(ScriptableObject).IsAssignableFrom(type)) throw new ArgumentException($"marker type {type.Name} is not a ScriptableObject");
            return type;
        }

        /// <summary>A concrete subtype of <typeparamref name="TBase"/> by full name or short name (TypeCache, no assembly scan).</summary>
        private static Type ResolveType<TBase>(string name, string what, string kinds)
        {
            name = name.Trim();
            var candidates = TypeCache.GetTypesDerivedFrom<TBase>().Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition).ToList();
            var match = candidates.FirstOrDefault(t => t.FullName == name)
                        ?? candidates.FirstOrDefault(t => t.Name == name)
                        ?? candidates.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match == null) throw new ArgumentException($"unknown {what} \"{name}\": use {kinds} or a type name such as {candidates.Select(t => t.Name).FirstOrDefault() ?? typeof(TBase).Name}");
            return match;
        }

        /// <summary>Mark the track and its timeline dirty (clips and markers are serialized into the track, sub-assets into the .playable).</summary>
        public static void Dirty(TrackAsset? track)
        {
            if (track == null) return;
            EditorUtility.SetDirty(track);
            if (track.timelineAsset != null) EditorUtility.SetDirty(track.timelineAsset);
        }

        public static void Dirty(TimelineAsset timeline) => EditorUtility.SetDirty(timeline);

        public static TrackAsset ParentTrack(TimelineClip clip) =>
            clip.GetParentTrack() ?? throw new InvalidOperationException($"clip '{clip.displayName}' is not on a track");

        /// <summary>Validate a new asset path: under Assets/, the expected extension, not overwriting an existing asset; creates folders.</summary>
        public static string NewAssetPath(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is empty");
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException($"'{path}' must start with Assets/");
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"'{path}' must end with {extension}");
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new ArgumentException($"an asset already exists at '{path}' (choose another path)");
            RqlEditorApi.Instance.CreateFolder(Path.GetDirectoryName(path)!);
            return path;
        }

        public static string Describe(Object? o) => o == null ? "null" : $"{o.GetType().Name} '{o.name}'";
    }
}
