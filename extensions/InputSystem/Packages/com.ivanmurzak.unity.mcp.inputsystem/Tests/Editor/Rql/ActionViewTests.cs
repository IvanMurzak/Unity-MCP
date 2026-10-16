/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-InputSystem)    │
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
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.InputSystem;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    using InputActionType = UnityEngine.InputSystem.InputActionType;
    using Setup = UnityEngine.InputSystem.InputActionSetupExtensions;

    public class ActionViewTests : InputSystemTestBase
    {
        [Test]
        public void SettingsPersist()
        {
            Ok(Action("Jump") + " | set type = \"PassThrough\", expectedControlType = \"Button\", interactions = \"hold(duration=0.4)\", " +
               "processors = \"invert\", initialStateCheck = true");
            using var saved = FromFile();
            var jump = saved.Asset.FindAction("Player/Jump", true);
            Assert.AreEqual(InputActionType.PassThrough, jump.type);
            Assert.AreEqual("Button", jump.expectedControlType);
            Assert.AreEqual("hold(duration=0.4)", jump.interactions);
            Assert.AreEqual("invert", jump.processors);
            Assert.IsTrue(jump.wantsInitialStateCheck);
        }

        [Test]
        public void LeavingValueTurnsTheInitialStateCheckOff()
        {
            Ok(Action("Move") + " | set type = \"Button\"");
            using (var saved = FromFile()) Assert.IsFalse(saved.Asset.FindAction("Player/Move", true).wantsInitialStateCheck);
            Ok(Action("Move") + " | set initialStateCheck = true");
            using (var saved = FromFile()) Assert.IsTrue(saved.Asset.FindAction("Player/Move", true).wantsInitialStateCheck);
        }

        [Test]
        public void ClearingASettingPersists()
        {
            Ok(Action("Move") + " | set expectedControlType = \"\"");
            using var saved = FromFile();
            Assert.IsTrue(string.IsNullOrEmpty(saved.Asset.FindAction("Player/Move", true).expectedControlType));
        }

        [Test]
        public void RenameKeepsTheBindings()
        {
            Ok(Action("Jump") + " | set name = \"Leap\"");
            using var saved = FromFile();
            var leap = saved.Asset.FindAction("Player/Leap", true);
            CollectionAssert.AreEqual(new[] { "<Keyboard>/space", "<Gamepad>/buttonSouth" }, leap.bindings.Select(b => b.path).ToArray());
            Assert.IsNull(saved.Asset.FindAction("Player/Jump"));
        }

        [Test]
        public void InvalidSettingsAreRejectedWithTheKnownNames()
        {
            Fails(Action("Jump") + " | set interactions = \"hodl\"", "unknown interaction 'hodl'");
            Fails(Action("Jump") + " | set interactions = \"hodl\"", "Hold");
            Fails(Action("Jump") + " | set processors = \"inverse\"", "unknown processor 'inverse'");
            Fails(Action("Jump") + " | set expectedControlType = \"Vector9\"", "unknown control type");
            Fails(Action("Jump") + " | set name = \"Move\"", "already exists");
            Fails(Action("Jump") + " | set type = \"Sometimes\"", "expected one of");
        }

        [Test]
        public void AFailingWriteRollsBackTheEarlierOnes()
        {
            Fails(Action("Jump") + " | set type = \"Value\", interactions = \"hodl\"", "rolled back");
            Assert.AreEqual(InputActionType.Button, Asset.FindAction("Player/Jump", true).type);
        }

        [Test]
        public void AMutationThatDoesNotSaveDropsItsDeferredEdits()
        {
            // RqlUnity.Discarded: nothing stays queued for the next mutation's save, and the asset matches its file.
            Fails(Action("Jump") + " | set type = \"Value\", interactions = \"hodl\"", "rolled back");
            var pending = (HashSet<string>)typeof(InputAssetStore)
                .GetField("Pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            Assert.IsEmpty(pending);
            using var saved = FromFile();
            Assert.AreEqual(saved.Asset.FindAction("Player/Jump", true).type, Asset.FindAction("Player/Jump", true).type);
        }

        [Test]
        public void AFailedDeferredEditKeepsTheFileUntouched()
        {
            // Inside a mutation a failed edit restores the earlier deferred edits in memory only: writing them to the file
            // would persist a set the engine then rolls back (Discard re-imports the file).
            var before = File.ReadAllText(P);
            var mutating = typeof(RqlUnity).GetField("_mutating", BindingFlags.NonPublic | BindingFlags.Static)!;
            mutating.SetValue(null, 1);
            try
            {
                InputAssetStore.Edit(Asset, "first", () => Setup.AddActionMap(Asset, "Deferred"));
                Assert.Throws<InvalidOperationException>(() =>
                    InputAssetStore.Edit(Asset, "second", () => throw new InvalidOperationException("boom")));
                Assert.IsNotNull(Asset.FindActionMap("Deferred"), "the snapshot keeps the earlier deferred edit in memory");
                Assert.AreEqual(before, File.ReadAllText(P), "a failed deferred edit wrote the file");
            }
            finally
            {
                mutating.SetValue(null, 0);
                typeof(InputAssetStore).GetMethod("Discard", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);
            }
            Assert.IsNull(Asset.FindActionMap("Deferred"));
            Assert.AreEqual(before, File.ReadAllText(P));
        }

        [Test]
        public void AddBindingPersistsWithGroupsInteractionsAndProcessors()
        {
            var r = Ok(Action("Jump") + " | call AddBinding(\"<Gamepad>/buttonNorth\", \"Gamepad\", \"tap\", \"invert\")");
            StringAssert.Contains("buttonNorth", Table(r));
            using var saved = FromFile();
            var b = saved.Asset.FindAction("Player/Jump", true).bindings.Last();
            Assert.AreEqual("<Gamepad>/buttonNorth", b.path);
            Assert.AreEqual("Gamepad", b.groups);
            Assert.AreEqual("tap", b.interactions);
            Assert.AreEqual("invert", b.processors);
        }

        [Test]
        public void AddBindingValidates()
        {
            Fails(Action("Jump") + " | call AddBinding(\"<Wheel>/x\")", "unknown device layout '<Wheel>'");
            Fails(Action("Jump") + " | call AddBinding(\"<Gamepad>/start\", \"Touch\")", "binding group 'Touch' is not a control scheme");
            Fails(Action("Jump") + " | call AddBinding(\"<Gamepad>/start\", null, \"tapp\")", "unknown interaction");
            Fails(Action("Jump") + " | call AddBinding(\"\")", "control path is empty");
        }

        [Test]
        public void AddCompositePersistsItsParts()
        {
            var r = Ok(Action("Move") + " | call AddComposite(\"2DVector\", {up: \"<Keyboard>/upArrow\", down: \"<Keyboard>/downArrow\", " +
                       "left: \"<Keyboard>/leftArrow\", right: \"<Keyboard>/rightArrow\"}, \"Keyboard&Mouse\")");
            StringAssert.Contains("2DVector", Table(r));
            using var saved = FromFile();
            var bindings = saved.Asset.FindAction("Player/Move", true).bindings;
            Assert.AreEqual(10, bindings.Count);
            Assert.IsTrue(bindings[5].isComposite);
            Assert.AreEqual("2DVector", bindings[5].path);
            CollectionAssert.AreEqual(new[] { "up", "down", "left", "right" }, bindings.Skip(6).Select(b => b.name).ToArray());
            CollectionAssert.AreEqual(new[] { "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow" },
                bindings.Skip(6).Select(b => b.path).ToArray());
            Assert.IsTrue(bindings.Skip(6).All(b => b.isPartOfComposite && b.groups == "Keyboard&Mouse"));
        }

        [Test]
        public void AddCompositeAcceptsOneDimensionalAxes()
        {
            Ok(A + " | call AddMap(\"Car\")");
            Ok(Map("Car") + " | call AddAction(\"Throttle\", \"Value\", \"Axis\")");
            Ok(Action("Throttle") + " | call AddComposite(\"1DAxis\", {negative: \"<Keyboard>/s\", positive: \"<Keyboard>/w\"})");
            using var saved = FromFile();
            var b = saved.Asset.FindAction("Car/Throttle", true).bindings;
            Assert.AreEqual(3, b.Count);
            Assert.AreEqual("1DAxis", b[0].path);
        }

        [Test]
        public void AddCompositeValidates()
        {
            Fails(Action("Move") + " | call AddComposite(\"4DVector\", {up: \"<Keyboard>/w\"})", "unknown composite '4DVector'");
            Fails(Action("Move") + " | call AddComposite(\"2DVector\", {top: \"<Keyboard>/w\"})", "has no part 'top'");
            Fails(Action("Move") + " | call AddComposite(\"2DVector\", {})", "parts are empty");
            Fails(Action("Move") + " | call AddComposite(\"2DVector\", {up: \"<Wheel>/x\"})", "unknown device layout");
        }

        [Test]
        public void RemoveDeletesTheActionAndItsBindings()
        {
            var r = Ok(Action("Jump") + " | call Remove()");
            Assert.AreEqual("1", Result(r));
            using var saved = FromFile();
            Assert.IsNull(saved.Asset.FindAction("Player/Jump"));
            Assert.IsFalse(saved.Asset.FindActionMap("Player")!.bindings.Any(b => b.action == "Jump"));
        }
    }
}
