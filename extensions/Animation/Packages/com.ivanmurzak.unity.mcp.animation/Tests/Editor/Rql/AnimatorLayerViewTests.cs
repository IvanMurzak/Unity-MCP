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
using NUnit.Framework;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    using AnimatorLayerBlendingMode = UnityEditor.Animations.AnimatorLayerBlendingMode;

    public class AnimatorLayerViewTests : AnimationTestBase
    {
        [Test]
        public void LayersAreReadable()
        {
            var r = Table(Ok(Q(P + " | get layers | select index name defaultWeight defaultState.name states.name")));
            StringAssert.Contains("Base Layer", r);
            StringAssert.Contains("Idle", r);
            StringAssert.Contains("Walk", r);
        }

        [Test]
        public void LayerMembersAreWritable()
        {
            Controller.AddLayer("Arms");
            Ok(P + " | set layers[1].defaultWeight = 0.5, layers[1].blendingMode = \"Additive\", layers[1].iKPass = true, layers[1].name = \"Upper\"");
            var layer = Controller.layers[1];
            Assert.AreEqual(0.5f, layer.defaultWeight, 1e-5f);
            Assert.AreEqual(AnimatorLayerBlendingMode.Additive, layer.blendingMode);
            Assert.IsTrue(layer.iKPass);
            Assert.AreEqual("Upper", layer.name);
        }

        [Test]
        public void LayerWritesAreValidated()
        {
            Controller.AddLayer("Arms");
            Fails(P + " | set layers[1].defaultWeight = 2", "must be 0 … 1");
            Fails(P + " | set layers[1].name = \"Base Layer\"", "already has a layer named");
            Fails(P + " | set layers[1].syncedLayerIndex = 1", "another layer's index");
            Assert.AreEqual("Arms", Controller.layers[1].name);
        }

        [Test]
        public void SyncedLayer()
        {
            Controller.AddLayer("Arms");
            Ok(P + " | set layers[1].syncedLayerIndex = 0");
            Assert.AreEqual(0, Controller.layers[1].syncedLayerIndex);
        }

        [Test]
        public void DefaultStateIsWritable()
        {
            var walk = Cell(Ok(Q(P + " | get layers | get states | where name == \"Walk\" | select $ref")));
            Ok(P + " | set layers[0].defaultState = ref(\"" + walk + "\")");
            Assert.AreEqual("Walk", Base.defaultState.name);
        }

        [Test]
        public void StatesOfALayerAreWritable()
        {
            Ok(P + " | get layers | get states | where name == \"Walk\" | set speed = 2, tag = \"Move\"");
            Assert.AreEqual(2f, StateNamed("Walk").speed, 1e-5f);
            Assert.AreEqual("Move", StateNamed("Walk").tag);
        }

        [Test]
        public void StatesIncludeSubStateMachines()
        {
            Base.AddStateMachine("Combat").AddState("Attack");
            Assert.AreEqual(3L, Count(P + " | get layers | get states | count"));
            Ok(P + " | call SetDefaultState(\"Idle\")");
        }

        [Test]
        public void LayerWritesAreUndoable()
        {
            Controller.AddLayer("Arms");
            Undo.ClearAll();
            Ok(P + " | set layers[1].defaultWeight = 0.25");
            Undo.PerformUndo();
            Assert.AreEqual(0f, Controller.layers[1].defaultWeight, 1e-5f);
        }
    }
}
