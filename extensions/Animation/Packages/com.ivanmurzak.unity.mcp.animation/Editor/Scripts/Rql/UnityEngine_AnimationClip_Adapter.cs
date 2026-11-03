/*
┌─────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                    │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Animation)  │
│  Copyright (c) 2026 Ivan Murzak                                         │
│  Licensed under the Apache License, Version 2.0.                        │
│  See the LICENSE file in the project root for more information.         │
└─────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Rql.Execution;
using UnityEditor;
using UnityEngine;
using static com.IvanMurzak.Unity.MCP.Editor.Rql.Animation.AnimationEdit;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    /// <summary>
    /// AnimationClip: the curves (AnimationUtility bindings, not reachable by reflection), object-reference curves, events and
    /// clip settings become views with writable members; the clip-level operations are methods. frameRate / wrapMode / legacy …
    /// stay ordinary members. Calls have the clip as owner, so the engine records it for Undo and marks it dirty.
    /// </summary>
    public sealed class UnityEngine_AnimationClip_Adapter : RqlTypeAdapter<AnimationClip>
    {
        public override string Description =>
            "AnimationClip: frameRate wrapMode legacy length isLooping; curves (path type property keys), objectCurves, events, settings " +
            "(loopTime …) are writable views; call SetCurve(path, type, property, keys), SetObjectCurve, RemoveCurve, ClearCurves, " +
            "AddEvent(time, functionName, …), RemoveEvent(index), ClearEvents";

        protected override void Members(MemberTable<AnimationClip> m)
        {
            m.Add("curves", c => AnimationCurveView.All(c), description: "float curves (views: path type property keyCount keys preWrapMode postWrapMode)")
             .Add("objectCurves", c => ObjectCurveView.All(c), description: "object-reference curves, e.g. sprite swaps (views: path type property keys)")
             .Add("events", c => AnimationEventView.All(c), description: "animation events in time order (views with writable time functionName … parameters)")
             .Add("settings", c => new AnimationClipSettingsView(c), description: "clip settings (loopTime loopBlend cycleOffset mirror … writable)");

            var t = m.Untyped;
            Add(t, "SetCurve", typeof(int), "add or replace the float curve of (path, type, property) with keys [[time, value], …] " +
                                            "(also [time, value, inTangent, outTangent] or {time, value, …}); returns the key count",
                (c, a) => SetCurve(c, (string?)a[0], (string)a[1]!, (string)a[2]!, (List<Keyframe>)a[3]!),
                Req("path", typeof(string)), Req("type", typeof(string)), Req("property", typeof(string)), Req("keys", typeof(List<Keyframe>)));
            Add(t, "SetObjectCurve", typeof(int), "add or replace the object-reference curve of (path, type, property) with keys " +
                                                  "[{time: 0, value: unity.asset(\"….png\")}, …] (sprite swaps: type SpriteRenderer, property m_Sprite); returns the key count",
                (c, a) => SetObjectCurve(c, (string?)a[0], (string)a[1]!, (string)a[2]!, (List<ObjectReferenceKeyframe>)a[3]!),
                Req("path", typeof(string)), Req("type", typeof(string)), Req("property", typeof(string)), Req("keys", typeof(List<ObjectReferenceKeyframe>)));
            Add(t, "RemoveCurve", typeof(bool), "remove the float or object-reference curve of (path, type, property); returns whether one existed",
                (c, a) => RemoveCurve(c, (string?)a[0], (string)a[1]!, (string)a[2]!),
                Req("path", typeof(string)), Req("type", typeof(string)), Req("property", typeof(string)));
            Add(t, "ClearCurves", typeof(int), "remove every curve (float and object-reference); returns how many were removed",
                (c, a) => ClearCurves(c));
            Add(t, "AddEvent", typeof(int), "add an animation event calling functionName at time (seconds), with optional arguments; returns its index",
                (c, a) => AddEvent(c, (float)a[0]!, (string)a[1]!, (string?)a[2], (float)a[3]!, (int)a[4]!),
                Req("time", typeof(float)), Req("functionName", typeof(string)), Opt("stringParameter", typeof(string), ""),
                Opt("floatParameter", typeof(float), 0f), Opt("intParameter", typeof(int), 0));
            Add(t, "RemoveEvent", typeof(int), "remove the event at index; returns the remaining event count",
                (c, a) => RemoveEvent(c, (int)a[0]!), Req("index", typeof(int)));
            Add(t, "ClearEvents", typeof(int), "remove every event; returns how many were removed",
                (c, a) => ClearEvents(c));
        }

        // ------------------------------------------------------------ operations (the call stage has recorded the clip for Undo)

        internal static EditorCurveBinding Binding(string? path, string type, string property, bool objectCurve)
        {
            if (string.IsNullOrWhiteSpace(property)) throw new ArgumentException("property is empty, e.g. \"localPosition.x\"");
            var animated = ResolveType(type);
            return objectCurve
                ? EditorCurveBinding.PPtrCurve(path ?? "", animated, property)
                : EditorCurveBinding.FloatCurve(path ?? "", animated, property);
        }

        internal static int SetCurve(AnimationClip clip, string? path, string type, string property, List<Keyframe> keys)
        {
            if (keys.Count == 0) throw new ArgumentException("keys is empty: give at least one [time, value]");
            var binding = Binding(path, type, property, false);
            AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(keys.ToArray()));
            EditorUtility.SetDirty(clip);
            return keys.Count;
        }

        internal static int SetObjectCurve(AnimationClip clip, string? path, string type, string property, List<ObjectReferenceKeyframe> keys)
        {
            if (keys.Count == 0) throw new ArgumentException("keys is empty: give at least one {time, value}");
            var binding = Binding(path, type, property, true);
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.OrderBy(k => k.time).ToArray());
            EditorUtility.SetDirty(clip);
            return keys.Count;
        }

        internal static bool RemoveCurve(AnimationClip clip, string? path, string type, string property)
        {
            var animated = ResolveType(type);
            path ??= "";
            bool Match(EditorCurveBinding b) => b.path == path && b.type == animated && b.propertyName == property;
            var removed = false;
            foreach (var b in AnimationUtility.GetCurveBindings(clip).Where(Match))
            {
                AnimationUtility.SetEditorCurve(clip, b, null);
                removed = true;
            }
            foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(clip).Where(Match))
            {
                AnimationUtility.SetObjectReferenceCurve(clip, b, null);
                removed = true;
            }
            if (removed) EditorUtility.SetDirty(clip);
            return removed;
        }

        internal static int ClearCurves(AnimationClip clip)
        {
            var count = AnimationUtility.GetCurveBindings(clip).Length + AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            clip.ClearCurves();
            EditorUtility.SetDirty(clip);
            return count;
        }

        internal static int AddEvent(AnimationClip clip, float time, string functionName, string? stringParameter, float floatParameter, int intParameter)
        {
            if (string.IsNullOrWhiteSpace(functionName)) throw new ArgumentException("functionName is empty");
            if (time < 0f) throw new ArgumentException($"time {time} must not be negative");
            var added = new AnimationEvent
            {
                time = time,
                functionName = functionName,
                stringParameter = stringParameter ?? "",
                floatParameter = floatParameter,
                intParameter = intParameter,
            };
            // Stable time order (an event added at the time of existing ones goes after them), the order Unity keeps.
            var events = AnimationUtility.GetAnimationEvents(clip).ToList();
            var index = events.FindIndex(e => e.time > time);
            if (index < 0) index = events.Count;
            events.Insert(index, added);
            AnimationEdit.Edit(clip, "add event", () => AnimationUtility.SetAnimationEvents(clip, events.ToArray()));
            return index;
        }

        internal static int RemoveEvent(AnimationClip clip, int index)
        {
            var events = AnimationUtility.GetAnimationEvents(clip).ToList();
            CheckIndex(index, events.Count, "event", "the clip");
            events.RemoveAt(index);
            AnimationEdit.Edit(clip, "remove event", () => AnimationUtility.SetAnimationEvents(clip, events.ToArray()));
            return events.Count;
        }

        internal static int ClearEvents(AnimationClip clip)
        {
            var count = AnimationUtility.GetAnimationEvents(clip).Length;
            AnimationEdit.Edit(clip, "clear events", () => AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>()));
            return count;
        }

        // ------------------------------------------------------------ registration helper

        private static void Add(MemberTable table, string name, Type returns, string description,
            Func<AnimationClip, object?[], object?> body, params CallableParameter[] parameters) =>
            table.AddMethod(name, parameters, returns, (o, a) => body((AnimationClip)o, a), description);
    }
}
