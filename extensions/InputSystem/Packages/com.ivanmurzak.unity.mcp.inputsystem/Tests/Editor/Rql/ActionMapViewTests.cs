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
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    using InputActionType = UnityEngine.InputSystem.InputActionType;

    public class ActionMapViewTests : InputSystemTestBase
    {
        [Test]
        public void RenameKeepsActionsAndPersists()
        {
            var id = Asset.FindActionMap("Player")!.id;
            Ok(Map("Player") + " | set name = \"Gameplay\"");
            using var saved = FromFile();
            var map = saved.Asset.FindActionMap("Gameplay");
            Assert.IsNotNull(map);
            Assert.AreEqual(id, map!.id);
            Assert.AreEqual(2, map.actions.Count);
            Assert.IsNull(saved.Asset.FindActionMap("Player"));
        }

        [Test]
        public void RenameRejectsDuplicates()
        {
            Fails(Map("Player") + " | set name = \"ui\"", "already exists");
            Fails(Map("Player") + " | set name = \"\"", "name is empty");
        }

        [Test]
        public void AddActionPersistsTypeExpectedControlTypeAndBinding()
        {
            var r = Ok(Map("Player") + " | call AddAction(\"Look\", \"Value\", \"Vector2\", \"<Mouse>/delta\")");
            StringAssert.Contains("Look", Table(r));
            using var saved = FromFile();
            var look = saved.Asset.FindAction("Player/Look", true);
            Assert.AreEqual(InputActionType.Value, look.type);
            Assert.AreEqual("Vector2", look.expectedControlType);
            Assert.AreEqual(1, look.bindings.Count);
            Assert.AreEqual("<Mouse>/delta", look.bindings[0].path);
        }

        [Test]
        public void AddActionDefaultsToAButtonWithoutBindings()
        {
            Ok(Map("UI") + " | call AddAction(\"Submit\")");
            var submit = Asset.FindAction("UI/Submit", true);
            Assert.AreEqual(InputActionType.Button, submit.type);
            Assert.AreEqual(0, submit.bindings.Count);
            // The returned view is usable straight away.
            var r = Read(Action("Submit") + " | select map name type");
            Assert.AreEqual("UI", Cell(r));
        }

        [Test]
        public void AddActionValidates()
        {
            Fails(Map("Player") + " | call AddAction(\"jump\")", "already exists");
            Fails(Map("Player") + " | call AddAction(\"Look\", \"Value\", \"Vector9\")", "unknown control type");
            Fails(Map("Player") + " | call AddAction(\"Look\", \"Value\", null, \"<Wheel>/x\")", "unknown device layout");
            Fails(Map("Player") + " | call AddAction(\"Look\", \"Sometimes\")", "expected one of");
        }

        [Test]
        public void RemoveDeletesTheMap()
        {
            var r = Ok(Map("UI") + " | call Remove()");
            Assert.AreEqual("1", Result(r));
            using var saved = FromFile();
            Assert.IsNull(saved.Asset.FindActionMap("UI"));
            Assert.AreEqual(1, saved.Asset.actionMaps.Count);
        }
    }
}
