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
using System.Linq;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    public class ControlSchemeViewTests : InputSystemTestBase
    {
        [Test]
        public void SchemesAreReadable()
        {
            var r = Read(A + " | get controlSchemes | select name bindingGroup requiredDevices optionalDevices");
            Assert.AreEqual(2, r.Rows!.Count, Table(r));
            Assert.AreEqual("Keyboard&Mouse", Cell(r));
            Assert.AreEqual("Keyboard&Mouse", Cell(r, 1));
            StringAssert.Contains("<Keyboard>", Cell(r, 2));
            StringAssert.Contains("<Mouse>", Cell(r, 3));
            Assert.AreEqual("Gamepad", Cell(r, 0, 1));
        }

        [Test]
        public void DevicesAreReplacedAndPersist()
        {
            Ok(Scheme("Gamepad") + " | set requiredDevices = [\"<Gamepad>\", \"<Joystick>\"], optionalDevices = [\"<Keyboard>\"]");
            using var saved = FromFile();
            var scheme = saved.Asset.controlSchemes.Single(s => s.name == "Gamepad");
            CollectionAssert.AreEqual(new[] { "<Gamepad>", "<Joystick>" },
                scheme.deviceRequirements.Where(d => !d.isOptional).Select(d => d.controlPath).ToArray());
            CollectionAssert.AreEqual(new[] { "<Keyboard>" },
                scheme.deviceRequirements.Where(d => d.isOptional).Select(d => d.controlPath).ToArray());
        }

        [Test]
        public void DeviceListsCannotBeEditedInPlace()
        {
            var r = M(Scheme("Gamepad") + " | set requiredDevices[0] = \"<Joystick>\"");
            Assert.IsFalse(r.Ok, Table(r));
            Assert.AreEqual("<Gamepad>", Asset.controlSchemes.Single(s => s.name == "Gamepad").deviceRequirements[0].controlPath);
        }

        [Test]
        public void RenameRenamesTheGroupInTheBindings()
        {
            Ok(Scheme("Gamepad") + " | set name = \"Controller\"");
            using var saved = FromFile();
            Assert.IsFalse(saved.Asset.controlSchemes.Any(s => s.name == "Gamepad"));
            Assert.AreEqual("Controller", saved.Asset.controlSchemes.Single(s => s.name == "Controller").bindingGroup);
            Assert.AreEqual("Controller", saved.Asset.FindAction("Player/Jump", true).bindings[1].groups);
            Assert.AreEqual("Keyboard&Mouse", saved.Asset.FindAction("Player/Jump", true).bindings[0].groups);
        }

        [Test]
        public void BindingGroupChangesFollowIntoTheBindings()
        {
            Ok(Scheme("Keyboard&Mouse") + " | set bindingGroup = \"KBM\"");
            using var saved = FromFile();
            Assert.AreEqual("KBM", saved.Asset.controlSchemes.Single(s => s.name == "Keyboard&Mouse").bindingGroup);
            Assert.IsTrue(saved.Asset.FindAction("Player/Move", true).bindings.Skip(1).All(b => b.groups == "KBM"));
            Assert.AreEqual("KBM", saved.Asset.FindAction("UI/Click", true).bindings[0].groups);
        }

        [Test]
        public void WritesAreValidated()
        {
            Fails(Scheme("Gamepad") + " | set name = \"keyboard&mouse\"", "already exists");
            Fails(Scheme("Gamepad") + " | set requiredDevices = [\"<Wheel>\"]", "unknown device layout");
            Fails(Scheme("Gamepad") + " | set bindingGroup = \"A;B\"", "must not contain ';'");
        }

        [Test]
        public void RemoveDeletesTheScheme()
        {
            var r = Ok(Scheme("Gamepad") + " | call Remove()");
            Assert.AreEqual("1", Result(r));
            using var saved = FromFile();
            Assert.AreEqual("Keyboard&Mouse", saved.Asset.controlSchemes.Single().name);
        }
    }
}
