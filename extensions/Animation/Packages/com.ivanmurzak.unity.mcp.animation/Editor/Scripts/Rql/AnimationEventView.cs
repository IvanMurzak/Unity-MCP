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
using Rql;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    /// <summary>
    /// One animation event of a clip (clip.events[i]). AnimationEvent objects are copies (and their runtime-only members throw
    /// outside an event callback), so this view reads the clip's events and writes the whole array back (undoable).
    /// </summary>
    [RqlDescription("Animation event: time (seconds) functionName stringParameter floatParameter intParameter objectReferenceParameter, " +
                    "all writable (undoable); call Remove()")]
    public sealed class AnimationEventView
    {
        internal AnimationEventView(AnimationClip clip, int index)
        {
            this.clip = clip;
            this.index = index;
        }

        internal static List<AnimationEventView> All(AnimationClip clip) =>
            Enumerable.Range(0, AnimationUtility.GetAnimationEvents(clip).Length).Select(i => new AnimationEventView(clip, i)).ToList();

        [RqlReference]
        [RqlDescription("the AnimationClip that owns the event")]
        public AnimationClip clip { get; }

        [RqlDescription("index of the event (events are in time order)")]
        public int index { get; }

        private AnimationEvent Event => Events()[index];

        [RqlDescription("time in seconds")]
        public float time { get => Event.time; set => Write("set event time", e => e.time = value); }

        [RqlDescription("method called on the animated GameObject's scripts")]
        public string functionName { get => Event.functionName; set => Write("set event function", e => e.functionName = value ?? ""); }

        [RqlDescription("string argument")]
        public string stringParameter { get => Event.stringParameter; set => Write("set event string", e => e.stringParameter = value ?? ""); }

        [RqlDescription("float argument")]
        public float floatParameter { get => Event.floatParameter; set => Write("set event float", e => e.floatParameter = value); }

        [RqlDescription("int argument")]
        public int intParameter { get => Event.intParameter; set => Write("set event int", e => e.intParameter = value); }

        [RqlReference]
        [RqlDescription("object argument (an asset reference)")]
        public Object? objectReferenceParameter
        {
            get => Event.objectReferenceParameter;
            set => Write("set event object", e => e.objectReferenceParameter = value);
        }

        [RqlCallable("remove this event from the clip")]
        public void Remove() => UnityEngine_AnimationClip_Adapter.RemoveEvent(clip, index);

        private AnimationEvent[] Events()
        {
            var events = AnimationUtility.GetAnimationEvents(clip);
            AnimationEdit.CheckIndex(index, events.Length, "event", "the clip");
            return events;
        }

        private void Write(string what, Action<AnimationEvent> change)
        {
            var events = Events();
            change(events[index]);
            AnimationEdit.Edit(clip, what, () => AnimationUtility.SetAnimationEvents(clip, events));
        }

        public override string ToString() => $"event {index} of {clip.name}";
    }
}
