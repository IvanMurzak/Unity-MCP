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
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    public class UnityEngine_InputSystem_InputActionAsset_AdapterTests : InputSystemTestBase
    {
        [Test]
        public void OnlyTheViewsAreMembers()
        {
            var r = Read(A + " | describe");
            var t = Table(r);
            foreach (var member in new[] { "maps", "actions", "controlSchemes", "path" }) StringAssert.Contains(member, t);
            foreach (var hidden in new[] { "bindingMask", "devices", "m_ActionMaps", "m_ControlSchemes" }) StringAssert.DoesNotContain(hidden, t);
            Assert.IsTrue(Q(A).Ok, Table(Q(A)));
        }

        [Test]
        public void MapsActionsAndSchemesAreReadable()
        {
            var maps = Read(A + " | get maps | select name actionCount");
            Assert.AreEqual(2, maps.Rows!.Count, Table(maps));
            Assert.AreEqual("Player", Cell(maps));
            Assert.AreEqual("2", Cell(maps, 1));
            var actions = Read(A + " | get actions | select map name type expectedControlType bindingCount");
            Assert.AreEqual(3, actions.Rows!.Count, Table(actions));
            Assert.AreEqual("Move", Cell(actions, 1, 1));
            Assert.AreEqual("Value", Cell(actions, 2, 1));
            Assert.AreEqual("Vector2", Cell(actions, 3, 1));
            Assert.AreEqual("5", Cell(actions, 4, 1));
            Assert.AreEqual("UI", Cell(actions, 0, 2));
            var path = Read(A + " | select path");
            Assert.AreEqual(P, Cell(path));
        }

        [Test]
        public void NameIsReadOnly()
        {
            var r = M(A + " | set name = \"Other\"");
            Assert.IsFalse(r.Ok, Table(r));
        }

        [Test]
        public void AddMapPersists()
        {
            var r = Ok(A + " | call AddMap(\"Vehicle\")");
            StringAssert.Contains("Vehicle", Table(r));
            Assert.IsNotNull(Asset.FindActionMap("Vehicle"));
            using var saved = FromFile();
            Assert.IsNotNull(saved.Asset.FindActionMap("Vehicle"));
        }

        [Test]
        public void AddMapRejectsDuplicatesAndBadNames()
        {
            Fails(A + " | call AddMap(\"player\")", "already exists");
            Fails(A + " | call AddMap(\"\")", "name is empty");
            Fails(A + " | call AddMap(\"A/B\")", "must not contain '/'");
        }

        [Test]
        public void AddControlSchemePersistsDevicesAndGroup()
        {
            Ok(A + " | call AddControlScheme(\"Touch\", [\"<Touchscreen>\"], [\"<Pen>\"])");
            using var saved = FromFile();
            var scheme = saved.Asset.controlSchemes.Single(s => s.name == "Touch");
            Assert.AreEqual("Touch", scheme.bindingGroup);
            Assert.AreEqual(2, scheme.deviceRequirements.Count);
            Assert.AreEqual("<Touchscreen>", scheme.deviceRequirements[0].controlPath);
            Assert.IsFalse(scheme.deviceRequirements[0].isOptional);
            Assert.AreEqual("<Pen>", scheme.deviceRequirements[1].controlPath);
            Assert.IsTrue(scheme.deviceRequirements[1].isOptional);

            Ok(A + " | call AddControlScheme(\"VR\", [\"<XRController>\"], null, \"XR\")");
            Assert.AreEqual("XR", Asset.controlSchemes.Single(s => s.name == "VR").bindingGroup);
        }

        [Test]
        public void AddControlSchemeValidates()
        {
            Fails(A + " | call AddControlScheme(\"gamepad\", [\"<Gamepad>\"])", "already exists");
            Fails(A + " | call AddControlScheme(\"Wheel\", [\"<SteeringWheel>\"])", "unknown device layout");
            Fails(A + " | call AddControlScheme(\"Wheel\", [\"/foo\"])", "must be a layout path");
            Fails(A + " | call AddControlScheme(\"A;B\", [\"<Gamepad>\"])", "must not contain ';'");
        }

        [Test]
        public void UndoRestoresTheFile()
        {
            var before = File.ReadAllText(P);
            Ok(A + " | call AddMap(\"Vehicle\")");
            Assert.AreNotEqual(before, File.ReadAllText(P));
            Undo.PerformUndo();
            Assert.IsNull(Asset.FindActionMap("Vehicle"));
            using (var saved = FromFile()) Assert.IsNull(saved.Asset.FindActionMap("Vehicle"));
            Undo.PerformRedo();
            using (var saved = FromFile()) Assert.IsNotNull(saved.Asset.FindActionMap("Vehicle"));
        }

        [Test]
        public void AnAssetReferencedByAComponentIsTheSameAsset()
        {
            var go = new UnityEngine.GameObject("Player");
            var input = go.AddComponent<UnityEngine.InputSystem.PlayerInput>();
            input.actions = Asset;
            var r = Read("objects | get components | where $ is PlayerInput | get actions | get maps | select name");
            Assert.AreEqual(2, r.Rows!.Count, Table(r));
            Ok("objects | get components | where $ is PlayerInput | get actions | call AddMap(\"Menu\")");
            using var saved = FromFile();
            Assert.IsNotNull(saved.Asset.FindActionMap("Menu"));
        }
    }
}
