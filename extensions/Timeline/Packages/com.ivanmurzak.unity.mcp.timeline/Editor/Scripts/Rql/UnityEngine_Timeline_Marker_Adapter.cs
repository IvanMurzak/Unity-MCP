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
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Timelines
{
    using Marker = UnityEngine.Timeline.Marker;

    /// <summary>
    /// Timeline markers (SignalEmitter and custom Marker types): <c>time</c> is validated (no negative times), <c>parent</c> (the
    /// track) is a reference, the serialized backing fields are hidden, and Delete removes the marker from its track.
    /// </summary>
    public sealed class UnityEngine_Timeline_Marker_Adapter : RqlTypeAdapter<Marker>
    {
        public override string Description =>
            "Timeline marker (SignalEmitter: asset = the SignalAsset, retroactive, emitOnce): time in seconds, parent track; call Delete()";

        protected override void Members(MemberTable<Marker> m)
        {
            TimelineOps.HideSerializedFields(m.Untyped);
            m.Reference("parent")
             .Add("time", k => k.time, (k, v) => k.time = v,
                  (k, v) => v >= 0.0 ? null : $"time {v.ToString(CultureInfo.InvariantCulture)} must be >= 0", "marker time in seconds")
             .Method("Delete", k => Delete(k), "remove the marker from its track (undoable)");
        }

        private static void Delete(Marker marker)
        {
            var track = marker.parent;
            if (track == null) throw new InvalidOperationException($"marker '{marker.name}' is not on a track");
            if (!track.DeleteMarker(marker)) throw new InvalidOperationException($"marker '{marker.name}' could not be deleted");
            TimelineOps.Dirty(track);
        }
    }
}
