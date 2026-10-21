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
using System.IO;
using Rql;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Timelines
{
    using TimelineAsset = UnityEngine.Timeline.TimelineAsset;

    /// <summary>
    /// The <c>timeline</c> source: Timeline operations without an owner object (creating TimelineAsset / SignalAsset files), e.g.
    /// <c>timeline | call Create("Assets/Cutscenes/Intro.playable", 30)</c>. Operations on an existing timeline are methods of
    /// TimelineAsset, TrackAsset, TimelineClip and markers (see the adapters).
    /// </summary>
    public sealed class TimelineEditorApi
    {
        public static readonly TimelineEditorApi Instance = new TimelineEditorApi();

        [RqlCallable("create an empty TimelineAsset at path (\"Assets/…/X.playable\", missing folders are created) with an editor " +
                     "frame rate (1 … 1000, default 60); fixedDuration > 0 switches durationMode to FixedLength; returns the asset")]
        public TimelineAsset Create(string path, double frameRate = 60.0, double fixedDuration = 0.0)
        {
            if (frameRate <= 0.0 || frameRate > 1000.0) throw new ArgumentException($"frameRate {frameRate} must be within (0, 1000]");
            if (fixedDuration < 0.0) throw new ArgumentException($"fixedDuration {fixedDuration} must be >= 0 (0 = based on clips)");
            path = TimelineOps.NewAssetPath(path, ".playable");

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = Path.GetFileNameWithoutExtension(path);
            timeline.editorSettings.frameRate = frameRate;
            if (fixedDuration > 0.0)
            {
                timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
                timeline.fixedDuration = fixedDuration;
            }
            AssetDatabase.CreateAsset(timeline, path);
            return timeline;
        }

        [RqlCallable("create a SignalAsset at path (\"Assets/…/X.signal\") for SignalEmitter markers (set asset = …) and SignalReceivers; returns it")]
        public SignalAsset CreateSignal(string path)
        {
            path = TimelineOps.NewAssetPath(path, ".signal");
            var signal = ScriptableObject.CreateInstance<SignalAsset>();
            signal.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(signal, path);
            return signal;
        }
    }
}
